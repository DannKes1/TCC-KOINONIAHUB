using System.Security.Claims;
using KoinoniaHub.API.Controllers;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Seguranca
{
    // Corretiva G5 (RF14, RNF 14.4 proposta): a inativação da conta, ou da pessoa (que inativa
    // a conta), bloqueia o acesso imediatamente, mesmo com sessão aberta. Toda requisição
    // autenticada a um endpoint que não seja público confere se a conta do token continua
    // ativa na mesma igreja; se não, responde 401 e apaga o cookie da sessão. Os endpoints
    // [AllowAnonymous] (login, cadastro inicial, primeiro acesso, termo vigente) passam sem a
    // conferência: um cookie antigo não pode impedir outra pessoa de entrar no mesmo navegador.
    public sealed class ContaAtivaMiddleware
    {
        public const string Mensagem = "Sua conta de acesso está inativa. Procure o administrador.";

        private readonly RequestDelegate _proximo;

        public ContaAtivaMiddleware(RequestDelegate proximo)
        {
            _proximo = proximo;
        }

        public async Task InvokeAsync(HttpContext contexto, KoinoniaHubDbContext db)
        {
            var endpoint = contexto.GetEndpoint();
            var publico = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

            if (endpoint is not null && !publico && contexto.User.Identity?.IsAuthenticated == true
                && !await ContaAtivaAsync(contexto.User, db))
            {
                contexto.Response.Cookies.Delete(
                    AuthController.NomeCookieToken,
                    AuthController.OpcoesCookie(DateTime.UtcNow.AddDays(-1)));
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await contexto.Response.WriteAsJsonAsync(new { mensagem = Mensagem });
                return;
            }

            await _proximo(contexto);
        }

        private static async Task<bool> ContaAtivaAsync(ClaimsPrincipal usuario, KoinoniaHubDbContext db)
        {
            if (!int.TryParse(usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var usuarioId) ||
                !int.TryParse(usuario.FindFirst("IgrejaId")?.Value, out var igrejaId))
                return false;

            return await db.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Id == usuarioId && u.IgrejaId == igrejaId && u.Ativo);
        }
    }
}
