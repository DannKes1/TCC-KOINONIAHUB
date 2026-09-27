using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Seguranca
{
    public class AutorizacaoEbdServico : IAutorizacaoEbdServico
    {
        private static readonly string[] FuncoesDeAtribuicao = { "Professor", "Auxiliar" };

        private readonly KoinoniaHubDbContext _db;

        public AutorizacaoEbdServico(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task GarantirAcessoDepartamentoAsync(int igrejaId, int usuarioId, string perfil, int departamentoId)
        {
            if (Perfis.EhAdministrativo(perfil))
                return;

            var depExiste = await _db.Departamentos.AsNoTracking()
                .AnyAsync(d => d.IgrejaId == igrejaId && d.Id == departamentoId);

            if (!depExiste)
                throw new InvalidOperationException("Departamento não encontrado para esta igreja.");

            var pessoaId = await ObterPessoaIdDoUsuarioAsync(igrejaId, usuarioId);

            var possuiAtribuicao = await _db.Atribuicoes.AsNoTracking()
                .AnyAsync(a =>
                    a.Ativo &&
                    a.DepartamentoId == departamentoId &&
                    a.PessoaId == pessoaId &&
                    FuncoesDeAtribuicao.Contains(a.Funcao));

            if (!possuiAtribuicao)
                throw new UnauthorizedAccessException("Você não tem permissão para operar esta turma (sem atribuição ativa).");
        }

        public async Task GarantirAcessoMateriaAsync(int igrejaId, int usuarioId, string perfil, int materiaId)
        {
            if (Perfis.EhAdministrativo(perfil))
                return;

            var materia = await _db.Materias.AsNoTracking()
                .Include(m => m.Departamento)
                .FirstOrDefaultAsync(m => m.Id == materiaId && m.Departamento.IgrejaId == igrejaId);

            if (materia is null)
                throw new InvalidOperationException("Matéria não encontrada para esta igreja.");

            await GarantirAcessoDepartamentoAsync(igrejaId, usuarioId, perfil, materia.DepartamentoId);
        }

        public async Task GarantirAcessoAulaAsync(int igrejaId, int usuarioId, string perfil, int aulaId)
        {
            if (Perfis.EhAdministrativo(perfil))
                return;

            var aula = await _db.Aulas.AsNoTracking()
                .Include(a => a.Materia)
                .ThenInclude(m => m.Departamento)
                .FirstOrDefaultAsync(a => a.Id == aulaId && a.Materia.Departamento.IgrejaId == igrejaId);

            if (aula is null)
                throw new InvalidOperationException("Aula não encontrada para esta igreja.");

            await GarantirAcessoDepartamentoAsync(igrejaId, usuarioId, perfil, aula.Materia.DepartamentoId);
        }

        public async Task GarantirAcessoPessoaAsync(int igrejaId, int usuarioId, string perfil, int pessoaId)
        {
            if (Perfis.EhAdministrativo(perfil))
                return;

            var usuario = await ObterUsuarioAtivoAsync(igrejaId, usuarioId);

            if (Perfis.EhUsuarioComum(perfil))
            {
                if (usuario.PessoaId != pessoaId)
                    throw new UnauthorizedAccessException("Você só pode acessar o seu próprio registro.");

                return;
            }

            if (!usuario.PessoaId.HasValue)
                throw new InvalidOperationException("Seu usuário não está vinculado a uma Pessoa (PessoaId).");

            var departamentos = await ListarDepartamentosDaPessoaAsync(igrejaId, usuario.PessoaId.Value);

            var permitido = departamentos.Count > 0 && await _db.AlunosDepartamentos.AsNoTracking()
                .AnyAsync(m =>
                    m.Ativo &&
                    m.PessoaId == pessoaId &&
                    m.Departamento.IgrejaId == igrejaId &&
                    departamentos.Contains(m.DepartamentoId));

            if (!permitido)
                throw new UnauthorizedAccessException("Você só pode consultar alunos das turmas em que possui atribuição ativa.");
        }

        public async Task<List<int>> ListarDepartamentosComAtribuicaoAtivaAsync(int igrejaId, int usuarioId)
        {
            var pessoaId = await ObterPessoaIdDoUsuarioAsync(igrejaId, usuarioId);
            return await ListarDepartamentosDaPessoaAsync(igrejaId, pessoaId);
        }

        private async Task<List<int>> ListarDepartamentosDaPessoaAsync(int igrejaId, int pessoaId)
        {
            return await _db.Atribuicoes.AsNoTracking()
                .Where(a =>
                    a.Ativo &&
                    a.PessoaId == pessoaId &&
                    a.Departamento.IgrejaId == igrejaId &&
                    FuncoesDeAtribuicao.Contains(a.Funcao))
                .Select(a => a.DepartamentoId)
                .Distinct()
                .ToListAsync();
        }

        private async Task<Usuario> ObterUsuarioAtivoAsync(int igrejaId, int usuarioId)
        {
            var usuario = await _db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IgrejaId == igrejaId && u.Id == usuarioId && u.Ativo);

            if (usuario is null)
                throw new InvalidOperationException("Usuário não encontrado ou inativo.");

            return usuario;
        }

        private async Task<int> ObterPessoaIdDoUsuarioAsync(int igrejaId, int usuarioId)
        {
            var usuario = await ObterUsuarioAtivoAsync(igrejaId, usuarioId);

            if (!usuario.PessoaId.HasValue)
                throw new InvalidOperationException("Seu usuário não está vinculado a uma Pessoa (PessoaId).");

            return usuario.PessoaId.Value;
        }
    }
}
