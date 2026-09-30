using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Excecoes;
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

            var materia = await _db.Materias
                .Include(m => m.Departamento)
                .FirstOrDefaultAsync(m =>
                    m.Id == dto.MateriaId &&
                    m.Departamento.IgrejaId == igrejaId);

            if (materia is null)
                throw new InvalidOperationException("Matéria não encontrada para esta igreja.");


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
                Situacao = SituacaoAula.EmAberto
            };

            var criada = await _repositorio.CriarAsync(aula);

            return new AulaRespostaDto
            {
                Id = criada.Id,
                Data = criada.Data,
                Tema = criada.Tema,
                Situacao = criada.Situacao,
                PendenteFechamento = CalcularPendenteFechamento(criada),
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
                Situacao = a.Situacao,
                PendenteFechamento = CalcularPendenteFechamento(a),
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
                Situacao = aula.Situacao,
                PendenteFechamento = CalcularPendenteFechamento(aula),
                QuantidadeVisitantes = aula.QuantidadeVisitantes,
                MateriaId = aula.MateriaId,
                NomeMateria = aula.Materia.Nome,
                ProfessorId = aula.ProfessorId,
                NomeProfessor = aula.Professor.Nome,
                CriadoEm = aula.CriadoEm
            };
        }

        // RF33 / RNFs 32.6, 33.2 e 33.3: só aulas Em aberto são consolidadas, e apenas
        // quando todos os alunos com matrícula ativa na turma (o mesmo conjunto que a
        // tela de chamada lista) possuem registro explícito de presença ou ausência.
        public async Task<bool> ConsolidarAsync(int igrejaId, int aulaId)
        {
            var aula = await _repositorio.ObterPorIdAsync(igrejaId, aulaId);
            if (aula is null) return false;

            if (aula.Situacao != SituacaoAula.EmAberto)
                throw new InvalidOperationException("Somente aulas Em aberto podem ser consolidadas.");

            var departamentoId = aula.Materia.DepartamentoId;

            var alunosSemRegistro = await _db.AlunosDepartamentos
                .AsNoTracking()
                .Where(m =>
                    m.Ativo &&
                    m.DepartamentoId == departamentoId &&
                    m.Departamento.IgrejaId == igrejaId &&
                    !_db.Presencas.Any(p => p.AulaId == aulaId && p.AlunoDepartamentoId == m.Id))
                .OrderBy(m => m.Pessoa.Nome)
                .Select(m => new AlunoSemRegistroRespostaDto
                {
                    AlunoDepartamentoId = m.Id,
                    NomeAluno = m.Pessoa.Nome
                })
                .ToListAsync();

            if (alunosSemRegistro.Count > 0)
                throw new ChamadaIncompletaException(alunosSemRegistro);

            aula.Situacao = SituacaoAula.Consolidada;
            await _db.SaveChangesAsync();

            return true;
        }

        // RF33 / RNFs 33.2, 33.4 e 33.7: só aulas Em aberto e sem nenhum registro de
        // presença podem ser marcadas como Não realizadas. A aula é encerrada sem
        // atribuir presença ou falta e fica fora dos cálculos de frequência (etapa 4).
        public async Task<bool> MarcarNaoRealizadaAsync(int igrejaId, int aulaId)
        {
            var aula = await _repositorio.ObterPorIdAsync(igrejaId, aulaId);
            if (aula is null) return false;

            if (aula.Situacao != SituacaoAula.EmAberto)
                throw new InvalidOperationException("Somente aulas Em aberto podem ser marcadas como Não realizadas.");

            var possuiPresencas = await _db.Presencas.AnyAsync(p => p.AulaId == aulaId);
            if (possuiPresencas)
                throw new InvalidOperationException("Uma aula com registros de presença não pode ser marcada como Não realizada.");

            aula.Situacao = SituacaoAula.NaoRealizada;
            await _db.SaveChangesAsync();

            return true;
        }

        // RF33 / RNFs 33.5 e 33.6 (CSU11, fluxo alternativo 3): o Administrador reabre
        // uma aula Consolidada ou Não realizada, que volta para Em aberto. Nenhum
        // registro de presença é tocado — os existentes ficam preservados para
        // correção e nova consolidação. A restrição ao perfil Admin está na rota.
        public async Task<bool> ReabrirAsync(int igrejaId, int aulaId)
        {
            var aula = await _repositorio.ObterPorIdAsync(igrejaId, aulaId);
            if (aula is null) return false;

            if (aula.Situacao == SituacaoAula.EmAberto)
                throw new InvalidOperationException("A aula já está Em aberto.");

            aula.Situacao = SituacaoAula.EmAberto;
            await _db.SaveChangesAsync();

            return true;
        }

        private static bool CalcularPendenteFechamento(Aula aula) =>
            aula.Situacao == SituacaoAula.EmAberto && aula.Data.Date < DateTime.UtcNow.Date;

    }

}
