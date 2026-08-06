using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class MatriculaServico : IMatriculaServico
    {
        private readonly IAlunoDepartamentoRepositorio _repositorio;
        private readonly KoinoniaHubDbContext _db;

        public MatriculaServico(IAlunoDepartamentoRepositorio repositorio, KoinoniaHubDbContext db)
        {
            _repositorio = repositorio;
            _db = db;
        }

        public async Task<MatriculaRespostaDto> MatricularAsync(int igrejaId, int departamentoId, MatriculaCriarRequisicaoDto dto)
        {
            
            var dep = await _db.Departamentos.AsNoTracking()
                .FirstOrDefaultAsync(d => d.IgrejaId == igrejaId && d.Id == departamentoId);

            if (dep is null)
                throw new InvalidOperationException("Departamento não encontrado para esta igreja.");

           
            var pessoa = await _db.Pessoas.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IgrejaId == igrejaId && p.Id == dto.PessoaId);

            if (pessoa is null)
                throw new InvalidOperationException("Pessoa não encontrada para esta igreja.");

            
            var existenteAtiva = await _repositorio.ObterAtivaAsync(igrejaId, departamentoId, dto.PessoaId);
            if (existenteAtiva is not null)
                throw new InvalidOperationException("Esta pessoa já está matriculada (ativa) neste departamento.");

            
            var matricula = new AlunoDepartamento
            {
                PessoaId = dto.PessoaId,
                DepartamentoId = departamentoId,
                DataMatricula = DateTime.UtcNow,
                Ativo = true,
                Observacao = dto.Observacao
            };

            var criada = await _repositorio.CriarAsync(matricula);

            return new MatriculaRespostaDto
            {
                Id = criada.Id,
                PessoaId = pessoa.Id,
                NomePessoa = pessoa.Nome,
                DepartamentoId = dep.Id,
                NomeDepartamento = dep.Nome,
                Ativo = criada.Ativo,
                DataMatricula = criada.DataMatricula,
                DataSaida = criada.DataSaida,
                Observacao = criada.Observacao
            };
        }

        public async Task<List<AlunoDaClasseRespostaDto>> ListarAlunosDaClasseAsync(int igrejaId, int departamentoId)
        {
            var lista = await _repositorio.ListarAlunosDaClasseAsync(igrejaId, departamentoId);

            return lista.Select(m => new AlunoDaClasseRespostaDto
            {
                MatriculaId = m.Id,
                PessoaId = m.PessoaId,
                Nome = m.Pessoa.Nome,
                StatusPessoa = m.Pessoa.Situacao,
                MatriculaAtiva = m.Ativo,
                DataMatricula = m.DataMatricula
            }).ToList();
        }

        public async Task<bool> InativarMatriculaAsync(int igrejaId, int departamentoId, int matriculaId)
        {
            var matricula = await _repositorio.ObterPorIdAsync(igrejaId, departamentoId, matriculaId);
            if (matricula is null) return false;

            if (!matricula.Ativo) return true;

            matricula.Ativo = false;
            matricula.DataSaida = DateTime.UtcNow;

            await _repositorio.AtualizarAsync(matricula);
            return true;
        }


        public async Task<List<PessoaRespostaDto>> ListarPessoasDisponiveisAsync(int igrejaId, int departamentoId)
        {
          
            var depOk = await _db.Departamentos.AsNoTracking()
                .AnyAsync(d => d.IgrejaId == igrejaId && d.Id == departamentoId);

            if (!depOk)
                throw new InvalidOperationException("Departamento não encontrado para esta igreja.");

            
            var jaMatriculadosIds = await _db.AlunosDepartamentos.AsNoTracking()
                .Where(m => m.DepartamentoId == departamentoId && m.Ativo)
                .Select(m => m.PessoaId)
                .ToListAsync();

            
            var disponiveis = await _db.Pessoas.AsNoTracking()
                .Where(p =>
                    p.IgrejaId == igrejaId &&
                    p.Situacao == "Ativo" &&
                    !jaMatriculadosIds.Contains(p.Id))
                .OrderBy(p => p.Nome)
                .Select(p => new PessoaRespostaDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Email = p.Email,
                    Celular = p.Celular,
                    Situacao = p.Situacao,
                    Categoria = p.Categoria
                })
                .ToListAsync();

            return disponiveis;
        }
    }
}