using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.DTOS.Requisicoes;
using KoinoniaHub.API.Aplicacao.Regras;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Dominio.Regras;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class PessoaServico : IPessoaServico
    {
        private readonly IPessoaRepositorio _repositorio;
        private readonly KoinoniaHubDbContext _db;

        public PessoaServico(IPessoaRepositorio repositorio, KoinoniaHubDbContext db)
        {
            _repositorio = repositorio;
            _db = db;
        }

        public async Task<PessoaRespostaDto> CriarAsync(int igrejaId, PessoaCriarRequisicaoDto dto)
        {
            // Corretiva G2: o e-mail não pode estar em outra pessoa nem na conta de outra pessoa
            // da igreja (EmailDaPessoa). Pessoa nova ainda não tem id nem conta (0).
            var email = EmailDaPessoa.Normalizar(dto.Email);
            if (email is not null)
                await EmailDaPessoa.GarantirDisponivelAsync(_db, igrejaId, pessoaId: 0, usuarioId: 0, email);

            var pessoa = new Pessoa
            {
                IgrejaId = igrejaId,
                Nome = dto.Nome.Trim(),
                DataNascimento = dto.DataNascimento,
                Sexo = dto.Sexo,
                EstadoCivil = dto.EstadoCivil,
                Celular = dto.Celular,
                Email = email,
                Endereco = dto.Endereco,
                Bairro = dto.Bairro,
                Cidade = dto.Cidade,
                Estado = dto.Estado,
                CEP = Cep.Normalizar(dto.CEP),
                Situacao = dto.Situacao,
                DataInativacao = string.Equals(dto.Situacao, "Inativo", StringComparison.OrdinalIgnoreCase)
                    ? DateTime.UtcNow
                    : null
            };

            var criada = await _repositorio.CriarAsync(pessoa);
            return Mapear(criada, emailDaConta: null);
        }

        public async Task<List<PessoaRespostaDto>> ListarAsync(int igrejaId)
        {
            var pessoas = await _repositorio.ListarAsync(igrejaId);

            // Corretiva G2: e-mail da conta de cada pessoa (PossuiConta e o e-mail exibido).
            var contas = await _db.Usuarios.AsNoTracking()
                .Where(u => u.IgrejaId == igrejaId && u.PessoaId != null)
                .Select(u => new { PessoaId = u.PessoaId!.Value, u.Email })
                .ToListAsync();
            var emailPorPessoa = new Dictionary<int, string>();
            foreach (var conta in contas) emailPorPessoa.TryAdd(conta.PessoaId, conta.Email);

            return pessoas.Select(p => Mapear(p, emailPorPessoa.GetValueOrDefault(p.Id))).ToList();
        }

        public async Task<PessoaRespostaDto?> ObterPorIdAsync(int igrejaId, int pessoaId)
        {
            var pessoa = await _repositorio.ObterPorIdAsync(igrejaId, pessoaId);
            return pessoa is null ? null : Mapear(pessoa, await EmailDaContaAsync(igrejaId, pessoa.Id));
        }

        // E-mail da conta de acesso vinculada à pessoa, ou nulo quando ela não tem conta.
        private Task<string?> EmailDaContaAsync(int igrejaId, int pessoaId) =>
            _db.Usuarios.AsNoTracking()
                .Where(u => u.IgrejaId == igrejaId && u.PessoaId == pessoaId)
                .Select(u => (string?)u.Email)
                .FirstOrDefaultAsync();

        // Corretiva G2: pessoa com conta tem o e-mail da conta (só o Administrador o altera,
        // pelo RF14); informar outro é recusado, e vazio mantém o da conta. Pessoa sem conta
        // pode mudar o e-mail, desde que ele não esteja em outra pessoa nem na conta de outra.
        private async Task<string?> ResolverEmailAsync(int igrejaId, int pessoaId, string? emailInformado)
        {
            var email = EmailDaPessoa.Normalizar(emailInformado);
            var emailDaConta = await EmailDaContaAsync(igrejaId, pessoaId);

            if (emailDaConta is not null)
            {
                if (email is not null && email != emailDaConta.ToLowerInvariant())
                    throw new InvalidOperationException(EmailDaPessoa.MensagemDaConta);
                return emailDaConta;
            }

            if (email is not null)
                await EmailDaPessoa.GarantirDisponivelAsync(_db, igrejaId, pessoaId, usuarioId: 0, email);

            return email;
        }

        public async Task<PessoaTurmaRespostaDto?> ObterParaTurmaAsync(int igrejaId, int pessoaId)
        {
            var pessoa = await _db.Pessoas.AsNoTracking()
                .Include(p => p.Parentescos)
                    .ThenInclude(x => x.Parente)
                .Include(p => p.ParentescosComoParente)
                    .ThenInclude(x => x.Pessoa)
                // Uma pessoa só: o produto Parentescos × ParentescosComoParente é pequeno; uma ida ao banco (S8733 avaliado).
                .AsSingleQuery()
                .FirstOrDefaultAsync(p => p.IgrejaId == igrejaId && p.Id == pessoaId);

            if (pessoa is null) return null;

            var registrados = pessoa.Parentescos
                .OrderBy(x => x.Parente.Nome)
                .Select(x => new PessoaTurmaParentescoRespostaDto
                {
                    ParenteNome = x.Parente.Nome,
                    TipoRelacionamento = x.TipoRelacionamento,
                    ParenteCelular = x.Parente.Celular,
                    Derivado = false
                })
                .ToList();

            // RNF 12.3 (mesma regra de ParentescoServico.ListarAsync): grau inverso dos vínculos
            // registrados no cadastro de outras pessoas, omitido quando há registro explícito inverso.
            var paresExplicitos = pessoa.Parentescos.Select(x => x.ParenteId).ToHashSet();

            registrados.AddRange(pessoa.ParentescosComoParente
                .Where(x => !paresExplicitos.Contains(x.PessoaId))
                .OrderBy(x => x.Pessoa.Nome)
                .Select(x => new PessoaTurmaParentescoRespostaDto
                {
                    ParenteNome = x.Pessoa.Nome,
                    TipoRelacionamento = ParentescoInverso.Derivar(x.TipoRelacionamento, x.Pessoa.Sexo, x.Pessoa.Nome),
                    ParenteCelular = x.Pessoa.Celular,
                    Derivado = true
                }));

            return new PessoaTurmaRespostaDto
            {
                Id = pessoa.Id,
                Nome = pessoa.Nome,
                Situacao = pessoa.Situacao,
                Celular = pessoa.Celular,
                Parentescos = registrados
            };
        }

        public async Task<bool> AtualizarAsync(int igrejaId, int pessoaId, PessoaAtualizarRequisicaoDto dto)
        {
            var pessoa = await _db.Pessoas.FirstOrDefaultAsync(p => p.IgrejaId == igrejaId && p.Id == pessoaId);
            if (pessoa is null) return false;


            var estavaInativa = string.Equals(pessoa.Situacao, "Inativo", StringComparison.OrdinalIgnoreCase);

            var email = await ResolverEmailAsync(igrejaId, pessoa.Id, dto.Email);

            pessoa.Nome = dto.Nome.Trim();
            pessoa.DataNascimento = dto.DataNascimento;
            pessoa.Sexo = dto.Sexo;
            pessoa.EstadoCivil = dto.EstadoCivil;
            pessoa.Celular = dto.Celular;
            pessoa.Email = email;
            pessoa.Endereco = dto.Endereco;
            pessoa.Bairro = dto.Bairro;
            pessoa.Cidade = dto.Cidade;
            pessoa.Estado = dto.Estado;
            pessoa.CEP = Cep.Normalizar(dto.CEP);
            pessoa.Situacao = dto.Situacao;

            var ficaInativa = string.Equals(pessoa.Situacao, "Inativo", StringComparison.OrdinalIgnoreCase);

            if (!estavaInativa && ficaInativa)
            {

                var agora = DateTime.UtcNow;
                pessoa.DataInativacao = agora;

                var matriculasAtivas = await _db.AlunosDepartamentos
                    .Where(m => m.PessoaId == pessoaId && m.Ativo)
                    .ToListAsync();

                foreach (var matricula in matriculasAtivas)
                {
                    matricula.Ativo = false;
                    matricula.DataSaida ??= agora;
                }

                var atribuicoesAtivas = await _db.Atribuicoes
                    .Where(a => a.PessoaId == pessoaId && a.Ativo)
                    .ToListAsync();

                foreach (var atribuicao in atribuicoesAtivas)
                {
                    atribuicao.Ativo = false;
                    atribuicao.DataFim ??= agora;
                }
            }
            else if (estavaInativa && !ficaInativa)
            {

                pessoa.DataInativacao = null;
            }

            await _repositorio.AtualizarAsync(pessoa);
            return true;
        }

        // Corretiva G2: pessoa com conta exibe o e-mail da conta (é o mesmo, por regra; nos
        // cadastros anteriores à regra a conta prevalece até a próxima gravação, que iguala).
        private static PessoaRespostaDto Mapear(Pessoa p, string? emailDaConta)
        {
            return new PessoaRespostaDto
            {
                Id = p.Id,
                Nome = p.Nome,
                DataNascimento = p.DataNascimento,
                Sexo = p.Sexo,
                EstadoCivil = p.EstadoCivil,
                Situacao = p.Situacao,
                DataInativacao = p.DataInativacao,
                Celular = p.Celular,
                Email = emailDaConta ?? p.Email,
                PossuiConta = emailDaConta is not null,
                Endereco = p.Endereco,
                Bairro = p.Bairro,
                Cidade = p.Cidade,
                Estado = p.Estado,
                CEP = p.CEP,
                CriadoEm = p.CriadoEm,
                AtualizadoEm = p.AtualizadoEm
            };
        }


        public async Task<PessoaRespostaDto?> ObterMeusDadosAsync(int igrejaId, int usuarioId)
        {
            var usuario = await _db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == usuarioId && u.IgrejaId == igrejaId);

            if (usuario?.PessoaId is null) return null;

            var pessoa = await _repositorio.ObterPorIdAsync(igrejaId, usuario.PessoaId.Value);
            return pessoa is null ? null : Mapear(pessoa, usuario.Email);
        }

        public async Task<bool> AtualizarMeusDadosAsync(int igrejaId, int usuarioId, MeusDadosAtualizarRequisicaoDto dto)
        {
            var usuario = await _db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == usuarioId && u.IgrejaId == igrejaId);

            if (usuario?.PessoaId is null)
                throw new InvalidOperationException("Seu usuário não está vinculado a uma pessoa.");

            var pessoa = await _db.Pessoas.FirstOrDefaultAsync(p =>
                p.IgrejaId == igrejaId && p.Id == usuario.PessoaId.Value);

            if (pessoa is null) return false;

            // Corretiva G2: quem tem conta (sempre, em Meus Dados) não altera o próprio e-mail.
            var email = await ResolverEmailAsync(igrejaId, pessoa.Id, dto.Email);

            pessoa.Celular = dto.Celular;
            pessoa.Email = email;
            pessoa.Endereco = dto.Endereco;
            pessoa.Bairro = dto.Bairro;
            pessoa.Cidade = dto.Cidade;
            pessoa.Estado = dto.Estado;
            pessoa.CEP = Cep.Normalizar(dto.CEP);

            await _repositorio.AtualizarAsync(pessoa);
            return true;
        }


        public async Task<List<MinhaTurmaRespostaDto>> ListarMinhasTurmasAsync(int igrejaId, int usuarioId)
        {
            var usuario = await _db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == usuarioId && u.IgrejaId == igrejaId);

            if (usuario?.PessoaId is null)
                return new List<MinhaTurmaRespostaDto>();

            var pessoaId = usuario.PessoaId.Value;
            var resultado = new Dictionary<int, MinhaTurmaRespostaDto>();


            var matriculas = await _db.AlunosDepartamentos.AsNoTracking()
                .Where(m => m.PessoaId == pessoaId && m.Ativo)
                .Include(m => m.Departamento)
                .ToListAsync();

            foreach (var m in matriculas)
            {
                if (m.Departamento is null || m.Departamento.IgrejaId != igrejaId) continue;

                resultado[m.DepartamentoId] = new MinhaTurmaRespostaDto
                {
                    DepartamentoId = m.DepartamentoId,
                    Nome = m.Departamento.Nome,
                    Tipo = m.Departamento.Tipo,
                    Ativo = m.Departamento.Ativo,
                    Vinculo = "Aluno"
                };
            }


            // RNF 21.4 (7.1): a atribuição só produz efeito para conta de perfil Professor
            // ou administrativo. Para o perfil Usuario, os vínculos de atribuição ficam fora
            // da lista (o de Aluno continua), para a tela não oferecer "Abrir turma" a quem
            // receberia 403 em todas as ações.
            var operaTurmas = Perfis.EhAdministrativo(usuario.Perfil) || Perfis.EhProfessor(usuario.Perfil);

            var atribuicoes = operaTurmas
                ? await _db.Atribuicoes.AsNoTracking()
                    .Where(a => a.PessoaId == pessoaId && a.Ativo)
                    .Include(a => a.Departamento)
                    .ToListAsync()
                : new List<Atribuicao>();

            foreach (var a in atribuicoes)
            {
                if (a.Departamento is null || a.Departamento.IgrejaId != igrejaId) continue;


                resultado[a.DepartamentoId] = new MinhaTurmaRespostaDto
                {
                    DepartamentoId = a.DepartamentoId,
                    Nome = a.Departamento.Nome,
                    Tipo = a.Departamento.Tipo,
                    Ativo = a.Departamento.Ativo,
                    Vinculo = a.Funcao
                };
            }


            var depIds = resultado.Keys.ToList();
            if (depIds.Count > 0)
            {
                var responsaveis = await (
                    from atr in _db.Atribuicoes.AsNoTracking()
                    join pes in _db.Pessoas.AsNoTracking() on atr.PessoaId equals pes.Id
                    where depIds.Contains(atr.DepartamentoId) && atr.Ativo && atr.Funcao == "Professor"
                    select new { atr.DepartamentoId, pes.Nome }
                ).ToListAsync();

                foreach (var grupo in responsaveis.GroupBy(r => r.DepartamentoId))
                    resultado[grupo.Key].Responsavel = string.Join(", ", grupo.Select(g => g.Nome).Distinct());
            }

            return resultado.Values
                .OrderBy(x => x.Nome, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), true))
                .ToList();
        }
    }
}
