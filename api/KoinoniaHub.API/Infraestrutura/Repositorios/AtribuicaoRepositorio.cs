using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Infraestrutura.Repositorios
{
    public class AtribuicaoRepositorio : IAtribuicaoRepositorio
    {
        private readonly KoinoniaHubDbContext _db;

        public AtribuicaoRepositorio(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<Atribuicao> CriarAsync(Atribuicao atribuicao)
        {
            _db.Atribuicoes.Add(atribuicao);
            await _db.SaveChangesAsync();
            return atribuicao;
        }

        public async Task<List<Atribuicao>> ListarAsync(int igrejaId)
        {
            return await _db.Atribuicoes
                .AsNoTracking()
                .Include(a => a.Pessoa)
                .Include(a => a.Departamento)
                .Where(a => a.Departamento.IgrejaId == igrejaId)
                .OrderByDescending(a => a.Ativo)
                .ThenBy(a => a.Funcao)
                .ThenBy(a => a.Pessoa.Nome)
                .ToListAsync();
        }

        public async Task<List<Atribuicao>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId, string? funcao = null, bool? ativo = null)
        {
            var query = _db.Atribuicoes
                .AsNoTracking()
                .Include(a => a.Pessoa)
                .Include(a => a.Departamento)
                .Where(a => a.Departamento.IgrejaId == igrejaId && a.DepartamentoId == departamentoId);

            if (!string.IsNullOrWhiteSpace(funcao))
            {
                var f = funcao.Trim().ToLowerInvariant();
                query = query.Where(a => a.Funcao.ToLower() == f);
            }

            if (ativo.HasValue)
                query = query.Where(a => a.Ativo == ativo.Value);

            return await query
                .OrderByDescending(a => a.Ativo)
                .ThenBy(a => a.Funcao)
                .ThenBy(a => a.Pessoa.Nome)
                .ToListAsync();
        }

        public async Task<Atribuicao?> ObterPorIdAsync(int igrejaId, int atribuicaoId)
        {
            return await _db.Atribuicoes
                .AsNoTracking()
                .Include(a => a.Pessoa)
                .Include(a => a.Departamento)
                .FirstOrDefaultAsync(a => a.Id == atribuicaoId && a.Departamento.IgrejaId == igrejaId);
        }

        public async Task AtualizarAsync(Atribuicao atribuicao)
        {
            _db.Atribuicoes.Update(atribuicao);
            await _db.SaveChangesAsync();
        }
    }
}