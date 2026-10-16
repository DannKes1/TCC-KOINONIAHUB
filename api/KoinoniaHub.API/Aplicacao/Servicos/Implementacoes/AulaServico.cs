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
            criada.Materia = materia;
            criada.Professor = professor;

            return Mapear(criada, possuiPresencas: false);
        }

        public async Task<List<AulaRespostaDto>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId)
        {

            var depOk = await _db.Departamentos.AnyAsync(d => d.IgrejaId == igrejaId && d.Id == departamentoId);
            if (!depOk)
                throw new InvalidOperationException("Departamento não encontrado para esta igreja.");

            var aulas = await _repositorio.ListarPorDepartamentoAsync(igrejaId, departamentoId);

            // Etapa 6.5: uma consulta só diz quais aulas da turma já têm presença
            // (PossuiPresencas), em vez de uma por aula.
            var comPresencas = await _db.Presencas.AsNoTracking()
                .Where(p => p.Aula.Materia.DepartamentoId == departamentoId)
                .Select(p => p.AulaId)
                .Distinct()
                .ToListAsync();
            var idsComPresencas = comPresencas.ToHashSet();

            return aulas.Select(a => Mapear(a, idsComPresencas.Contains(a.Id))).ToList();
        }

        public async Task<AulaRespostaDto?> ObterPorIdAsync(int igrejaId, int aulaId)
        {
            var aula = await _repositorio.ObterPorIdAsync(igrejaId, aulaId);
            if (aula is null) return null;

            var possuiPresencas = await _db.Presencas.AnyAsync(p => p.AulaId == aulaId);
            return Mapear(aula, possuiPresencas);
        }

        // RF45 / RNFs 45.2 e 45.4 (CSU09 FA1): só aula Em aberto; a matéria nova tem
        // de ser da mesma turma da aula e o professor precisa de atribuição ativa de
        // Professor nessa turma (mesma regra do cadastro, RNF 21.3). AtualizadoEm é
        // preenchido pelo DbContext.
        public async Task<AulaRespostaDto?> AtualizarAsync(int igrejaId, int aulaId, AulaAtualizarRequisicaoDto dto)
        {
            var aula = await _repositorio.ObterPorIdAsync(igrejaId, aulaId);
            if (aula is null) return null;

            if (aula.Situacao != SituacaoAula.EmAberto)
                throw new InvalidOperationException("Somente aulas Em aberto podem ser editadas.");

            var departamentoId = aula.Materia.DepartamentoId;

            var materia = await _db.Materias
                .Include(m => m.Departamento)
                .FirstOrDefaultAsync(m =>
                    m.Id == dto.MateriaId &&
                    m.DepartamentoId == departamentoId &&
                    m.Departamento.IgrejaId == igrejaId);

            if (materia is null)
                throw new InvalidOperationException("A matéria informada não pertence à turma desta aula.");

            var professor = await _db.Pessoas
                .Where(p => p.IgrejaId == igrejaId && p.Id == dto.ProfessorId)
                .Where(p => p.Atribuicoes.Any(a =>
                    a.DepartamentoId == departamentoId &&
                    a.Funcao == "Professor" &&
                    a.Ativo))
                .FirstOrDefaultAsync();

            if (professor is null)
                throw new InvalidOperationException("Professor não encontrado ou sem atribuição ativa de Professor neste departamento.");

            aula.Data = dto.Data;
            aula.Tema = dto.Tema;
            aula.Conteudo = dto.Conteudo;
            aula.MateriaId = materia.Id;
            aula.Materia = materia;
            aula.ProfessorId = professor.Id;
            aula.Professor = professor;

            await _db.SaveChangesAsync();

            var possuiPresencas = await _db.Presencas.AnyAsync(p => p.AulaId == aulaId);
            return Mapear(aula, possuiPresencas);
        }

        // RF45 / RNFs 45.2 e 45.5 (CSU09 FA2): só aula Em aberto e sem nenhum registro
        // de presença — a mesma condição da RNF 33.7 para Não realizada. Aula com
        // registros permanece Em aberto até ser consolidada.
        public async Task<bool> ExcluirAsync(int igrejaId, int aulaId)
        {
            var aula = await _repositorio.ObterPorIdAsync(igrejaId, aulaId);
            if (aula is null) return false;

            if (aula.Situacao != SituacaoAula.EmAberto)
                throw new InvalidOperationException("Somente aulas Em aberto podem ser excluídas.");

            var possuiPresencas = await _db.Presencas.AnyAsync(p => p.AulaId == aulaId);
            if (possuiPresencas)
                throw new InvalidOperationException("Uma aula com registros de presença não pode ser excluída.");

            await _repositorio.ExcluirAsync(aula);
            return true;
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

        // Exige Materia (com Departamento) e Professor carregados.
        private static AulaRespostaDto Mapear(Aula aula, bool possuiPresencas) => new()
        {
            Id = aula.Id,
            Data = aula.Data,
            Tema = aula.Tema,
            Conteudo = aula.Conteudo,
            Situacao = aula.Situacao,
            PendenteFechamento = CalcularPendenteFechamento(aula),
            PossuiPresencas = possuiPresencas,
            QuantidadeVisitantes = aula.QuantidadeVisitantes,
            MateriaId = aula.MateriaId,
            NomeMateria = aula.Materia.Nome,
            DepartamentoId = aula.Materia.DepartamentoId,
            NomeDepartamento = aula.Materia.Departamento.Nome,
            ProfessorId = aula.ProfessorId,
            NomeProfessor = aula.Professor.Nome,
            CriadoEm = aula.CriadoEm
        };

    }

}
