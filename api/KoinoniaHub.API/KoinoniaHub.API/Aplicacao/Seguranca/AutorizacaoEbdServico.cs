using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Seguranca
{
    public class AutorizacaoEbdServico : IAutorizacaoEbdServico
    {
        private readonly KoinoniaHubDbContext _db;

        public AutorizacaoEbdServico(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        private static bool EhPerfilAdministrativo(string perfil)
        {
            var perfisAdministrativos = new[] { "Admin", "Pastor", "Superintendente" };
            return perfisAdministrativos.Contains(perfil, StringComparer.OrdinalIgnoreCase);
        }

        public async Task GarantirAcessoDepartamentoAsync(int igrejaId, int usuarioId, string perfil, int departamentoId)
        {
            // Perfis administrativos (Admin, Pastor e Superintendente) operam
           
            if (EhPerfilAdministrativo(perfil))
                return;

            // Departamento precisa pertencer à igreja
            var depExiste = await _db.Departamentos.AsNoTracking()
                .AnyAsync(d => d.IgrejaId == igrejaId && d.Id == departamentoId);

            if (!depExiste)
                throw new InvalidOperationException("Departamento não encontrado para esta igreja.");

            // Pegar PessoaId do usuário
            var usuario = await _db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IgrejaId == igrejaId && u.Id == usuarioId && u.Ativo);

            if (usuario is null)
                throw new InvalidOperationException("Usuário não encontrado ou inativo.");

            if (!usuario.PessoaId.HasValue)
                throw new InvalidOperationException("Seu usuário não está vinculado a uma Pessoa (PessoaId).");

            var pessoaId = usuario.PessoaId.Value;

            // Exigir atribuição ativa na turma
            var funcoesPermitidas = new[] { "Professor", "Auxiliar" };

            var possuiAtribuicao = await _db.Atribuicoes.AsNoTracking()
                .AnyAsync(a =>
                    a.Ativo &&
                    a.DepartamentoId == departamentoId &&
                    a.PessoaId == pessoaId &&
                    funcoesPermitidas.Contains(a.Funcao));

            if (!possuiAtribuicao)
                throw new UnauthorizedAccessException("Você não tem permissão para operar esta turma (sem atribuição ativa).");
        }

        public async Task GarantirAcessoMateriaAsync(int igrejaId, int usuarioId, string perfil, int materiaId)
        {
            if (EhPerfilAdministrativo(perfil))
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
            if (EhPerfilAdministrativo(perfil))
                return;

            var aula = await _db.Aulas.AsNoTracking()
                .Include(a => a.Materia)
                .ThenInclude(m => m.Departamento)
                .FirstOrDefaultAsync(a => a.Id == aulaId && a.Materia.Departamento.IgrejaId == igrejaId);

            if (aula is null)
                throw new InvalidOperationException("Aula não encontrada para esta igreja.");

            await GarantirAcessoDepartamentoAsync(igrejaId, usuarioId, perfil, aula.Materia.DepartamentoId);
        }
    }
}