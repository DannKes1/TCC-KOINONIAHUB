using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class ChamadaServico : IChamadaServico
    {
        private readonly IPresencaRepositorio _presencaRepositorio;
        private readonly IAulaRepositorio _aulaRepositorio;
        private readonly KoinoniaHubDbContext _db;

        public ChamadaServico(IPresencaRepositorio presencaRepositorio, IAulaRepositorio aulaRepositorio, KoinoniaHubDbContext db)
        {
            _presencaRepositorio = presencaRepositorio;
            _aulaRepositorio = aulaRepositorio;
            _db = db;
        }



        public async Task<List<ItemChamadaCompletaRespostaDto>> ObterChamadaCompletaAsync(int igrejaId, int aulaId)
        {
            var aula = await _aulaRepositorio.ObterPorIdAsync(igrejaId, aulaId);
            if (aula is null)
                throw new InvalidOperationException("Aula não encontrada para esta igreja.");

            var departamentoId = aula.Materia.DepartamentoId;

            //  Matrículas ativas da classe
            var matriculas = await _db.AlunosDepartamentos
                .AsNoTracking()
                .Include(m => m.Pessoa)
                .Where(m =>
                    m.Ativo &&
                    m.DepartamentoId == departamentoId &&
                    m.Departamento.IgrejaId == igrejaId &&
                    m.Pessoa.IgrejaId == igrejaId)
                .OrderBy(m => m.Pessoa.Nome)
                .ToListAsync();

            //  Presenças já registradas na aula
            var presencas = await _db.Presencas
                .AsNoTracking()
                .Where(p => p.AulaId == aulaId)
                .ToListAsync();

            var resposta = matriculas.Select(m =>
            {
                var p = presencas.FirstOrDefault(x => x.AlunoDepartamentoId == m.Id);

                return new ItemChamadaCompletaRespostaDto
                {
                    AlunoDepartamentoId = m.Id,
                    PessoaId = m.PessoaId,
                    NomeAluno = m.Pessoa.Nome,
                    Presente = p?.Presente ?? false,
                    Observacao = p?.Observacao
                };
            }).ToList();

            return resposta;
        }
        public async Task<List<PresencaRespostaDto>> RegistrarAsync(int igrejaId, int aulaId, ChamadaRegistrarRequisicaoDto dto)
        {
            var aula = await _aulaRepositorio.ObterPorIdAsync(igrejaId, aulaId);
            if (aula is null)
                throw new InvalidOperationException("Aula não encontrada para esta igreja.");

            if (aula.Consolidada)
                throw new InvalidOperationException("A aula está consolidada e não permite alterar a chamada.");

            var departamentoId = aula.Materia.DepartamentoId;

           
            if (dto.QuantidadeVisitantes.HasValue)
            {
                aula.QuantidadeVisitantes = Math.Max(0, dto.QuantidadeVisitantes.Value);
                await _db.SaveChangesAsync();
            }

            
            var idsMatriculas = dto.Itens.Select(i => i.AlunoDepartamentoId).Distinct().ToList();

            var matriculas = await _db.AlunosDepartamentos
                .Include(m => m.Pessoa)
                .Include(m => m.Departamento)
                .Where(m =>
                    idsMatriculas.Contains(m.Id) &&
                    m.DepartamentoId == departamentoId &&
                    m.Departamento.IgrejaId == igrejaId &&
                    m.Pessoa.IgrejaId == igrejaId)
                .ToListAsync();

            if (matriculas.Count != idsMatriculas.Count)
                throw new InvalidOperationException("Uma ou mais matrículas são inválidas para esta aula/classe.");

          
            var respostas = new List<PresencaRespostaDto>();

            foreach (var item in dto.Itens)
            {
                var matricula = matriculas.First(m => m.Id == item.AlunoDepartamentoId);

                var existente = await _presencaRepositorio.ObterPorChaveAsync(aulaId, item.AlunoDepartamentoId);
                if (existente is null)
                {
                    var nova = new Presenca
                    {
                        AulaId = aulaId,
                        AlunoDepartamentoId = item.AlunoDepartamentoId,
                        Presente = item.Presente,
                        Observacao = item.Observacao
                    };

                    var criada = await _presencaRepositorio.CriarAsync(nova);

                    respostas.Add(new PresencaRespostaDto
                    {
                        Id = criada.Id,
                        AulaId = aulaId,
                        AlunoDepartamentoId = item.AlunoDepartamentoId,
                        PessoaId = matricula.PessoaId,
                        NomeAluno = matricula.Pessoa.Nome,
                        Presente = criada.Presente,
                        Observacao = criada.Observacao,
                        CriadoEm = criada.CriadoEm
                    });
                }
                else
                {
                    existente.Presente = item.Presente;
                    existente.Observacao = item.Observacao;

                    await _presencaRepositorio.AtualizarAsync(existente);

                    respostas.Add(new PresencaRespostaDto
                    {
                        Id = existente.Id,
                        AulaId = aulaId,
                        AlunoDepartamentoId = item.AlunoDepartamentoId,
                        PessoaId = matricula.PessoaId,
                        NomeAluno = matricula.Pessoa.Nome,
                        Presente = existente.Presente,
                        Observacao = existente.Observacao,
                        CriadoEm = existente.CriadoEm
                    });
                }
            }

            return respostas.OrderBy(r => r.NomeAluno).ToList();
        }

        public async Task<List<PresencaRespostaDto>> ListarAsync(int igrejaId, int aulaId)
        {
            var lista = await _presencaRepositorio.ListarPorAulaAsync(igrejaId, aulaId);

            return lista.Select(p => new PresencaRespostaDto
            {
                Id = p.Id,
                AulaId = p.AulaId,
                AlunoDepartamentoId = p.AlunoDepartamentoId,
                PessoaId = p.AlunoDepartamento.PessoaId,
                NomeAluno = p.AlunoDepartamento.Pessoa.Nome,
                Presente = p.Presente,
                Observacao = p.Observacao,
                CriadoEm = p.CriadoEm
            }).ToList();
        }
    }
}