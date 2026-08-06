using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.DTOS.Requisicoes;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
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
            
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var email = dto.Email.Trim().ToLowerInvariant();
                var existe = await _db.Pessoas.AnyAsync(p => p.IgrejaId == igrejaId && p.Email != null && p.Email.ToLower() == email);
                if (existe)
                    throw new InvalidOperationException("Já existe uma pessoa com este e-mail nesta igreja.");
            }

            var pessoa = new Pessoa
            {
                IgrejaId = igrejaId,
                Nome = dto.Nome.Trim(),
                CPF = dto.CPF,
                DataNascimento = dto.DataNascimento,
                Sexo = dto.Sexo,
                EstadoCivil = dto.EstadoCivil,
                Telefone = dto.Telefone,
                Celular = dto.Celular,
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant(),
                Endereco = dto.Endereco,
                Bairro = dto.Bairro,
                Cidade = dto.Cidade,
                Estado = dto.Estado,
                CEP = dto.CEP,
                Situacao = dto.Situacao,
                Categoria = dto.Categoria,
                DataInativacao = string.Equals(dto.Situacao, "Inativo", StringComparison.OrdinalIgnoreCase)
                    ? DateTime.UtcNow
                    : null,
                DataBatismo = dto.DataBatismo,
                DataMembresia = dto.DataMembresia,
                FotoUrl = dto.FotoUrl,
                Observacoes = dto.Observacoes
            };

            var criada = await _repositorio.CriarAsync(pessoa);
            return Mapear(criada);
        }

        public async Task<List<PessoaRespostaDto>> ListarAsync(int igrejaId)
        {
            var pessoas = await _repositorio.ListarAsync(igrejaId);
            return pessoas.Select(Mapear).ToList();
        }

        public async Task<PessoaRespostaDto?> ObterPorIdAsync(int igrejaId, int pessoaId)
        {
            var pessoa = await _repositorio.ObterPorIdAsync(igrejaId, pessoaId);
            return pessoa is null ? null : Mapear(pessoa);
        }

        public async Task<bool> AtualizarAsync(int igrejaId, int pessoaId, PessoaAtualizarRequisicaoDto dto)
        {
            var pessoa = await _db.Pessoas.FirstOrDefaultAsync(p => p.IgrejaId == igrejaId && p.Id == pessoaId);
            if (pessoa is null) return false;

           
            var estavaInativa = string.Equals(pessoa.Situacao, "Inativo", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var email = dto.Email.Trim().ToLowerInvariant();
                var existe = await _db.Pessoas.AnyAsync(p =>
                    p.IgrejaId == igrejaId &&
                    p.Id != pessoaId &&
                    p.Email != null &&
                    p.Email.ToLower() == email);

                if (existe)
                    throw new InvalidOperationException("Já existe uma pessoa com este e-mail nesta igreja.");
            }

            pessoa.Nome = dto.Nome.Trim();
            pessoa.CPF = dto.CPF;
            pessoa.DataNascimento = dto.DataNascimento;
            pessoa.Sexo = dto.Sexo;
            pessoa.EstadoCivil = dto.EstadoCivil;
            pessoa.Telefone = dto.Telefone;
            pessoa.Celular = dto.Celular;
            pessoa.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant();
            pessoa.Endereco = dto.Endereco;
            pessoa.Bairro = dto.Bairro;
            pessoa.Cidade = dto.Cidade;
            pessoa.Estado = dto.Estado;
            pessoa.CEP = dto.CEP;
            pessoa.Situacao = dto.Situacao;
            pessoa.Categoria = dto.Categoria;
            pessoa.DataBatismo = dto.DataBatismo;
            pessoa.DataMembresia = dto.DataMembresia;
            pessoa.FotoUrl = dto.FotoUrl;
            pessoa.Observacoes = dto.Observacoes;

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

        private static PessoaRespostaDto Mapear(Pessoa p)
        {
            return new PessoaRespostaDto
            {
                Id = p.Id,
                Nome = p.Nome,
                CPF = p.CPF,
                DataNascimento = p.DataNascimento,
                Sexo = p.Sexo,
                EstadoCivil = p.EstadoCivil,
                Situacao = p.Situacao,
                Categoria = p.Categoria,
                DataInativacao = p.DataInativacao,
                Telefone = p.Telefone,
                Celular = p.Celular,
                Email = p.Email,
                Endereco = p.Endereco,
                Bairro = p.Bairro,
                Cidade = p.Cidade,
                Estado = p.Estado,
                CEP = p.CEP,
                DataBatismo = p.DataBatismo,
                DataMembresia = p.DataMembresia,
                FotoUrl = p.FotoUrl,
                Observacoes = p.Observacoes,
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
            return pessoa is null ? null : Mapear(pessoa);
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

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var email = dto.Email.Trim().ToLowerInvariant();
                var existe = await _db.Pessoas.AnyAsync(p =>
                    p.IgrejaId == igrejaId &&
                    p.Id != pessoa.Id &&
                    p.Email != null &&
                    p.Email.ToLower() == email);

                if (existe)
                    throw new InvalidOperationException("Já existe uma pessoa com este e-mail nesta igreja.");
            }

            pessoa.Telefone = dto.Telefone;
            pessoa.Celular = dto.Celular;
            pessoa.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant();
            pessoa.Endereco = dto.Endereco;
            pessoa.Bairro = dto.Bairro;
            pessoa.Cidade = dto.Cidade;
            pessoa.Estado = dto.Estado;
            pessoa.CEP = dto.CEP;

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

            
            var atribuicoes = await _db.Atribuicoes.AsNoTracking()
                .Where(a => a.PessoaId == pessoaId && a.Ativo)
                .Include(a => a.Departamento)
                .ToListAsync();

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
