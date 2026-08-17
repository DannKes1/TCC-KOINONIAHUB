using System.Globalization;
using System.Net.Mail;
using System.Text;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    // Importa pessoas (membros/visitantes) a partir de um arquivo CSV,
    // por exemplo o rol de membros mantido pela secretaria da igreja.
    // Regras:
    //  - Coluna "Nome" é obrigatória; as demais são opcionais.
    //  - Separador ";" ou "," ou TAB (detectado automaticamente).
    //  - Linhas duplicadas (mesmo e-mail, ou mesmo nome sem e-mail) são
    //    ignoradas, permitindo reimportar o arquivo com segurança.
    public class PessoaImportacaoServico : IPessoaImportacaoServico
    {
        private const int LimiteLinhas = 1000;

        private readonly KoinoniaHubDbContext _db;

        public PessoaImportacaoServico(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<PessoaImportacaoRespostaDto> ImportarCsvAsync(int igrejaId, Stream conteudo)
        {
            string texto;
            using (var leitor = new StreamReader(conteudo, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                texto = await leitor.ReadToEndAsync();

            var linhas = LerCsv(texto);
            if (linhas.Count == 0)
                throw new InvalidOperationException("O arquivo está vazio.");

            var cabecalho = linhas[0].Select(NormalizarChave).ToList();

            int Coluna(params string[] nomes)
            {
                foreach (var n in nomes)
                {
                    var i = cabecalho.IndexOf(n);
                    if (i >= 0) return i;
                }
                return -1;
            }

            var idxNome = Coluna("nome", "nomecompleto");
            if (idxNome < 0)
                throw new InvalidOperationException("O arquivo precisa ter uma coluna 'Nome'. Baixe o modelo CSV para conferir o formato.");

            var idxEmail = Coluna("email");
            var idxCelular = Coluna("celular", "whatsapp");
            var idxTelefone = Coluna("telefone", "fone");
            var idxNascimento = Coluna("datanascimento", "datadenascimento", "nascimento");
            var idxSexo = Coluna("sexo");
            var idxCategoria = Coluna("categoria");
            var idxSituacao = Coluna("situacao");
            var idxCidade = Coluna("cidade");
            var idxEstado = Coluna("estado", "uf");
            var idxBairro = Coluna("bairro");
            var idxEndereco = Coluna("endereco");
            var idxCep = Coluna("cep");
            var idxCpf = Coluna("cpf");
            var idxObs = Coluna("observacoes", "observacao", "obs");

            if (linhas.Count - 1 > LimiteLinhas)
                throw new InvalidOperationException($"O arquivo tem {linhas.Count - 1} linhas de dados. O limite por importação é {LimiteLinhas}.");

            // Pessoas já cadastradas na igreja (verificação de duplicidade)
            var existentes = await _db.Pessoas
                .AsNoTracking()
                .Where(p => p.IgrejaId == igrejaId)
                .Select(p => new { p.Nome, p.Email })
                .ToListAsync();

            var emailsConhecidos = new HashSet<string>(
                existentes
                    .Where(p => !string.IsNullOrWhiteSpace(p.Email))
                    .Select(p => p.Email!.Trim().ToLowerInvariant()));

            var nomesConhecidos = new HashSet<string>(
                existentes.Select(p => p.Nome.Trim().ToLowerInvariant()));

            var resposta = new PessoaImportacaoRespostaDto();
            var novasPessoas = new List<Pessoa>();

            for (var i = 1; i < linhas.Count; i++)
            {
                var campos = linhas[i];
                var numeroLinha = i + 1; // número real no arquivo (linha 1 = cabeçalho)

                string Valor(int idx) =>
                    idx >= 0 && idx < campos.Count ? campos[idx].Trim() : string.Empty;

                if (campos.All(string.IsNullOrWhiteSpace)) continue;

                resposta.TotalLinhas++;

                var nome = Valor(idxNome);
                var email = Valor(idxEmail).ToLowerInvariant();
                var avisos = new List<string>();

                var item = new PessoaImportacaoItemDto
                {
                    Linha = numeroLinha,
                    Nome = nome,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email
                };
                resposta.Itens.Add(item);

                // ---- Validações que impedem a criação ----
                if (string.IsNullOrWhiteSpace(nome))
                {
                    item.Status = "Erro";
                    item.Mensagem = "A coluna Nome é obrigatória.";
                    continue;
                }

                if (nome.Length > 200)
                {
                    item.Status = "Erro";
                    item.Mensagem = "O nome excede o limite de 200 caracteres.";
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    if (email.Length > 100 || !EmailValido(email))
                    {
                        item.Status = "Erro";
                        item.Mensagem = "E-mail em formato inválido.";
                        continue;
                    }

                    if (emailsConhecidos.Contains(email))
                    {
                        item.Status = "Ignorado";
                        item.Mensagem = "Já existe uma pessoa com este e-mail (linha ignorada).";
                        continue;
                    }
                }
                else if (nomesConhecidos.Contains(nome.ToLowerInvariant()))
                {
                    // Sem e-mail para diferenciar: trata como possível duplicado.
                    item.Status = "Ignorado";
                    item.Mensagem = "Já existe uma pessoa com este nome (linha ignorada).";
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(email) && nomesConhecidos.Contains(nome.ToLowerInvariant()))
                    avisos.Add("atenção: já existia pessoa com o mesmo nome");

                // ---- Campos opcionais (problemas viram avisos, não erros) ----
                DateTime? dataNascimento = null;
                var textoNascimento = Valor(idxNascimento);
                if (!string.IsNullOrWhiteSpace(textoNascimento))
                {
                    dataNascimento = ConverterData(textoNascimento);
                    if (dataNascimento is null)
                        avisos.Add("data de nascimento ignorada (use dd/mm/aaaa)");
                }

                var categoria = NormalizarOpcao(Valor(idxCategoria), "Membro", "Visitante") ?? "Membro";
                var situacao = NormalizarOpcao(Valor(idxSituacao), "Ativo", "Inativo") ?? "Ativo";

                var pessoa = new Pessoa
                {
                    IgrejaId = igrejaId,
                    Nome = nome,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email,
                    Celular = Limitar(Valor(idxCelular), 20),
                    Telefone = Limitar(Valor(idxTelefone), 20),
                    DataNascimento = dataNascimento,
                    Sexo = Limitar(Valor(idxSexo), 20),
                    Categoria = categoria,
                    Situacao = situacao,
                    DataInativacao = situacao == "Inativo" ? DateTime.UtcNow : null,
                    Cidade = Limitar(Valor(idxCidade), 100),
                    Estado = Limitar(Valor(idxEstado), 2),
                    Bairro = Limitar(Valor(idxBairro), 100),
                    Endereco = Limitar(Valor(idxEndereco), 500),
                    CEP = Limitar(Valor(idxCep), 10),
                    CPF = Limitar(Valor(idxCpf), 14),
                    Observacoes = Limitar(Valor(idxObs), 1000)
                };

                novasPessoas.Add(pessoa);

                // Registra para as verificações de duplicidade dentro do próprio arquivo
                if (!string.IsNullOrWhiteSpace(email)) emailsConhecidos.Add(email);
                nomesConhecidos.Add(nome.ToLowerInvariant());

                item.Status = "Criado";
                item.Mensagem = avisos.Count > 0 ? string.Join("; ", avisos) : null;
            }

            if (novasPessoas.Count > 0)
            {
                _db.Pessoas.AddRange(novasPessoas);
                await _db.SaveChangesAsync();
            }

            resposta.Criados = resposta.Itens.Count(x => x.Status == "Criado");
            resposta.Ignorados = resposta.Itens.Count(x => x.Status == "Ignorado");
            resposta.Erros = resposta.Itens.Count(x => x.Status == "Erro");

            return resposta;
        }

        // ---------- Auxiliares ----------

        private static bool EmailValido(string email)
        {
            try
            {
                var endereco = new MailAddress(email);
                return endereco.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static string? Limitar(string valor, int maximo)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            valor = valor.Trim();
            return valor.Length <= maximo ? valor : valor[..maximo];
        }

        private static string? NormalizarOpcao(string valor, params string[] opcoes)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var normalizado = NormalizarChave(valor);
            return opcoes.FirstOrDefault(o => NormalizarChave(o) == normalizado);
        }

        private static DateTime? ConverterData(string valor)
        {
            var formatos = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };

            if (DateTime.TryParseExact(valor.Trim(), formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
                return DateTime.SpecifyKind(data.Date, DateTimeKind.Utc); // Npgsql exige Kind = Utc

            return null;
        }

        // Remove acentos, espaços e separadores para comparar nomes de coluna.
        // Ex.: "E-mail", "Data de Nascimento" -> "email", "datadenascimento".
        private static string NormalizarChave(string valor)
        {
            var semAcentos = new string(valor
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());

            return semAcentos
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");
        }

        // Leitor de CSV simples com suporte a campos entre aspas
        // (aspas duplas escapadas como "" e separador dentro de aspas).
        private static List<List<string>> LerCsv(string texto)
        {
            var separador = DetectarSeparador(texto);

            var linhas = new List<List<string>>();
            var campos = new List<string>();
            var atual = new StringBuilder();
            var entreAspas = false;

            void FecharCampo()
            {
                campos.Add(atual.ToString());
                atual.Clear();
            }

            void FecharLinha()
            {
                FecharCampo();
                if (campos.Any(c => !string.IsNullOrWhiteSpace(c)))
                    linhas.Add(campos);
                campos = new List<string>();
            }

            for (var i = 0; i < texto.Length; i++)
            {
                var c = texto[i];

                if (entreAspas)
                {
                    if (c == '"')
                    {
                        if (i + 1 < texto.Length && texto[i + 1] == '"')
                        {
                            atual.Append('"');
                            i++;
                        }
                        else
                        {
                            entreAspas = false;
                        }
                    }
                    else
                    {
                        atual.Append(c);
                    }
                    continue;
                }

                if (c == '"') { entreAspas = true; continue; }
                if (c == separador) { FecharCampo(); continue; }
                if (c == '\r') continue;
                if (c == '\n') { FecharLinha(); continue; }

                atual.Append(c);
            }

            FecharLinha();
            return linhas;
        }

        private static char DetectarSeparador(string texto)
        {
            var fimPrimeiraLinha = texto.IndexOf('\n');
            var primeiraLinha = fimPrimeiraLinha > 0 ? texto[..fimPrimeiraLinha] : texto;

            var pontoVirgula = primeiraLinha.Count(c => c == ';');
            var virgula = primeiraLinha.Count(c => c == ',');
            var tabulacao = primeiraLinha.Count(c => c == '\t');

            if (tabulacao > pontoVirgula && tabulacao > virgula) return '\t';
            return virgula > pontoVirgula ? ',' : ';';
        }
    }
}
