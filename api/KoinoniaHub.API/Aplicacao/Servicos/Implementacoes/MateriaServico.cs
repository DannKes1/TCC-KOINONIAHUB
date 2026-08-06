using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class MateriaServico : IMateriaServico
    {
        private readonly IMateriaRepositorio _repositorio;
        private readonly KoinoniaHubDbContext _db;

        public MateriaServico(IMateriaRepositorio repositorio, KoinoniaHubDbContext db)
        {
            _repositorio = repositorio;
            _db = db;
        }

        public async Task<MateriaRespostaDto> CriarAsync(int igrejaId, MateriaCriarRequisicaoDto dto)
        {
            var dep = await _db.Departamentos.AsNoTracking()
                .FirstOrDefaultAsync(d => d.IgrejaId == igrejaId && d.Id == dto.DepartamentoId);

            if (dep is null)
                throw new InvalidOperationException("Departamento não encontrado para esta igreja.");

            var nome = dto.Nome.Trim();

            var existe = await _db.Materias.AnyAsync(m =>
                m.DepartamentoId == dto.DepartamentoId &&
                m.Departamento.IgrejaId == igrejaId &&
                m.Nome.ToLower() == nome.ToLower());

            if (existe)
                throw new InvalidOperationException("Já existe uma matéria com este nome neste departamento.");

            var materia = new Materia
            {
                Nome = nome,
                Descricao = dto.Descricao,
                ImagemUrl = dto.ImagemUrl,
                OrdemExibicao = dto.OrdemExibicao,
                Ativo = dto.Ativo,
                DepartamentoId = dto.DepartamentoId
            };

            var criada = await _repositorio.CriarAsync(materia);

            return new MateriaRespostaDto
            {
                Id = criada.Id,
                Nome = criada.Nome,
                Ativo = criada.Ativo,
                OrdemExibicao = criada.OrdemExibicao,
                DepartamentoId = dep.Id,
                NomeDepartamento = dep.Nome,
                CriadoEm = criada.CriadoEm,
                AtualizadoEm = criada.AtualizadoEm
            };
        }

        public async Task<List<MateriaRespostaDto>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId)
        {
            var depOk = await _db.Departamentos.AnyAsync(d => d.IgrejaId == igrejaId && d.Id == departamentoId);
            if (!depOk)
                throw new InvalidOperationException("Departamento não encontrado para esta igreja.");

            var lista = await _repositorio.ListarPorDepartamentoAsync(igrejaId, departamentoId);

            return lista.Select(m => new MateriaRespostaDto
            {
                Id = m.Id,
                Nome = m.Nome,
                Ativo = m.Ativo,
                OrdemExibicao = m.OrdemExibicao,
                DepartamentoId = m.DepartamentoId,
                NomeDepartamento = m.Departamento.Nome,
                CriadoEm = m.CriadoEm,
                AtualizadoEm = m.AtualizadoEm
            }).ToList();
        }

        public async Task<MateriaRespostaDto?> ObterPorIdAsync(int igrejaId, int materiaId)
        {
            var m = await _repositorio.ObterPorIdAsync(igrejaId, materiaId);
            if (m is null) return null;

            return new MateriaRespostaDto
            {
                Id = m.Id,
                Nome = m.Nome,
                Ativo = m.Ativo,
                OrdemExibicao = m.OrdemExibicao,
                DepartamentoId = m.DepartamentoId,
                NomeDepartamento = m.Departamento.Nome,
                CriadoEm = m.CriadoEm,
                AtualizadoEm = m.AtualizadoEm
            };
        }

        public async Task<bool> AtualizarAsync(int igrejaId, int materiaId, MateriaAtualizarRequisicaoDto dto)
        {
            var materia = await _db.Materias
                .Include(m => m.Departamento)
                .FirstOrDefaultAsync(m => m.Id == materiaId && m.Departamento.IgrejaId == igrejaId);

            if (materia is null) return false;

            var nome = dto.Nome.Trim();

            var existe = await _db.Materias.AnyAsync(m =>
                m.Id != materiaId &&
                m.DepartamentoId == materia.DepartamentoId &&
                m.Departamento.IgrejaId == igrejaId &&
                m.Nome.ToLower() == nome.ToLower());

            if (existe)
                throw new InvalidOperationException("Já existe uma matéria com este nome neste departamento.");

            materia.Nome = nome;
            materia.Descricao = dto.Descricao;
            materia.ImagemUrl = dto.ImagemUrl;
            materia.OrdemExibicao = dto.OrdemExibicao;
            materia.Ativo = dto.Ativo;

            await _repositorio.AtualizarAsync(materia);
            return true;
        }
    }
}