using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class PresencaHistoricoServico : IPresencaHistoricoServico
    {
        private readonly KoinoniaHubDbContext _db;

        public PresencaHistoricoServico(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<List<HistoricoPresencaRespostaDto>> ListarPorPessoaAsync(int igrejaId, int pessoaId)
        {
            var pessoaExiste = await _db.Pessoas.AsNoTracking()
                .AnyAsync(p => p.IgrejaId == igrejaId && p.Id == pessoaId);

            if (!pessoaExiste)
                throw new InvalidOperationException("Pessoa não encontrada para esta igreja.");

            var itens = await _db.Presencas
                .AsNoTracking()
                .Where(p =>
                    p.AlunoDepartamento.PessoaId == pessoaId &&
                    p.AlunoDepartamento.Departamento.IgrejaId == igrejaId)
                .Select(p => new HistoricoPresencaRespostaDto
                {
                    AulaId = p.AulaId,
                    DataAula = p.Aula.Data,
                    DepartamentoId = p.Aula.Materia.DepartamentoId,
                    DepartamentoNome = p.Aula.Materia.Departamento.Nome,
                    MateriaId = p.Aula.MateriaId,
                    MateriaNome = p.Aula.Materia.Nome,
                    Presente = p.Presente,
                    Observacao = p.Observacao
                })
                .OrderByDescending(x => x.DataAula)
                .ToListAsync();

            return itens;
        }
    }
}