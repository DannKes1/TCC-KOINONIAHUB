using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class AulaServico : IAulaServico
    {
        private readonly IAulaRepositorio _repositorio;
        private readonly KoinoniaHubDbContext _db;

        public AulaServico(IAulaRepositorio repositorio, KoinoniaHubDbContext db)
        {
            _repositorio = repositorio;
            _db = db;
        }

        public async Task<AulaRespostaDto> CriarAsync(int igrejaId, AulaCriarRequisicaoDto dto)
        {
            // Matéria deve existir e ser da igreja 
            var materia = await _db.Materias
                .Include(m => m.Departamento)
                .FirstOrDefaultAsync(m =>
                    m.Id == dto.MateriaId &&
                    m.Departamento.IgrejaId == igrejaId);

            if (materia is null)
                throw new InvalidOperationException("Matéria não encontrada para esta igreja.");

            //  Professor deve existir e ser da igreja
            // Professor deve ter atribuição ativa de "Professor" 
            var professor = await _db.Pessoas.AsNoTracking()
                .Where(p => p.IgrejaId == igrejaId && p.Id == dto.ProfessorId)
                .Where(p => p.Atribuicoes.Any(a =>
                    a.DepartamentoId == materia.DepartamentoId &&
                    a.Funcao == "Professor" &&
                    a.Ativo))
                .FirstOrDefaultAsync();

            if (professor is null)
                throw new InvalidOperationException("Professor não encontrado ou sem atribuição ativa de Professor neste departamento.");

            var aula = new Aula
            {
                Data = dto.Data,
                Tema = dto.Tema,
                Conteudo = dto.Conteudo,
                Observacoes = dto.Observacoes,
                MateriaId = dto.MateriaId,
                ProfessorId = dto.ProfessorId,
                Consolidada = false
            };

            var criada = await _repositorio.CriarAsync(aula);

            return new AulaRespostaDto
            {
                Id = criada.Id,
                Data = criada.Data,
                Tema = criada.Tema,
                Consolidada = criada.Consolidada,
                QuantidadeVisitantes = criada.QuantidadeVisitantes,
                MateriaId = materia.Id,
                NomeMateria = materia.Nome,
                ProfessorId = professor.Id,
                NomeProfessor = professor.Nome,
                CriadoEm = criada.CriadoEm
            };
        }

        public async Task<List<AulaRespostaDto>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId)
        {
            
            var depOk = await _db.Departamentos.AnyAsync(d => d.IgrejaId == igrejaId && d.Id == departamentoId);
            if (!depOk)
                throw new InvalidOperationException("Departamento não encontrado para esta igreja.");

            var aulas = await _repositorio.ListarPorDepartamentoAsync(igrejaId, departamentoId);

            return aulas.Select(a => new AulaRespostaDto
            {
                Id = a.Id,
                Data = a.Data,
                Tema = a.Tema,
                Consolidada = a.Consolidada,
                QuantidadeVisitantes = a.QuantidadeVisitantes,
                MateriaId = a.MateriaId,
                NomeMateria = a.Materia.Nome,
                ProfessorId = a.ProfessorId,
                NomeProfessor = a.Professor.Nome,
                CriadoEm = a.CriadoEm
            }).ToList();
        }

        public async Task<AulaRespostaDto?> ObterPorIdAsync(int igrejaId, int aulaId)
        {
            var aula = await _repositorio.ObterPorIdAsync(igrejaId, aulaId);
            if (aula is null) return null;

            return new AulaRespostaDto
            {
                Id = aula.Id,
                Data = aula.Data,
                Tema = aula.Tema,
                Consolidada = aula.Consolidada,
                QuantidadeVisitantes = aula.QuantidadeVisitantes,
                MateriaId = aula.MateriaId,
                NomeMateria = aula.Materia.Nome,
                ProfessorId = aula.ProfessorId,
                NomeProfessor = aula.Professor.Nome,
                CriadoEm = aula.CriadoEm
            };
        }

        public async Task<bool> ConsolidarAsync(int igrejaId, int aulaId)
        {
            var aula = await _db.Aulas
                .Include(a => a.Materia)
                    .ThenInclude(m => m.Departamento)
                .FirstOrDefaultAsync(a => a.Id == aulaId && a.Materia.Departamento.IgrejaId == igrejaId);

            if (aula is null) return false;

            if (aula.Consolidada) return true;

            aula.Consolidada = true;
            await _db.SaveChangesAsync();

            return true;
        }

    }

}