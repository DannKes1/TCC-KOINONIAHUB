using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Infraestrutura.Repositorios
{
    public class AlunoDepartamentoRepositorio : IAlunoDepartamentoRepositorio
    {
        private readonly KoinoniaHubDbContext _db;

        public AlunoDepartamentoRepositorio(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<AlunoDepartamento?> ObterAtivaAsync(int igrejaId, int departamentoId, int pessoaId)
        {
            return await _db.AlunosDepartamentos
                .Include(m => m.Pessoa)
                .Include(m => m.Departamento)
                .FirstOrDefaultAsync(m =>
                    m.Ativo &&
                    m.DepartamentoId == departamentoId &&
                    m.PessoaId == pessoaId &&
                    m.Departamento.IgrejaId == igrejaId &&
                    m.Pessoa.IgrejaId == igrejaId);
        }

        public async Task<AlunoDepartamento> CriarAsync(AlunoDepartamento matricula)
        {
            _db.AlunosDepartamentos.Add(matricula);
            await _db.SaveChangesAsync();
            return matricula;
        }

        public async Task<List<AlunoDepartamento>> ListarAlunosDaClasseAsync(int igrejaId, int departamentoId)
        {
            return await _db.AlunosDepartamentos
                .AsNoTracking()
                .Include(m => m.Pessoa)
                .Include(m => m.Departamento)
                .Where(m =>
                    m.DepartamentoId == departamentoId &&
                    m.Departamento.IgrejaId == igrejaId &&
                    m.Pessoa.IgrejaId == igrejaId)
                .OrderByDescending(m => m.Ativo)
                .ThenBy(m => m.Pessoa.Nome)
                .ToListAsync();
        }

        public async Task<AlunoDepartamento?> ObterPorIdAsync(int igrejaId, int departamentoId, int matriculaId)
        {
            return await _db.AlunosDepartamentos
                .Include(m => m.Pessoa)
                .Include(m => m.Departamento)
                .FirstOrDefaultAsync(m =>
                    m.Id == matriculaId &&
                    m.DepartamentoId == departamentoId &&
                    m.Departamento.IgrejaId == igrejaId &&
                    m.Pessoa.IgrejaId == igrejaId);
        }

        public async Task AtualizarAsync(AlunoDepartamento matricula)
        {
            _db.AlunosDepartamentos.Update(matricula);
            await _db.SaveChangesAsync();
        }
    }
}