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
    //
    // Corretiva G6 (RF14, RNF 14.5 proposta): o perfil vai dentro do token. Quando o perfil da
    // conta muda, a sessão aberta com o perfil anterior também é encerrada (401 e cookie
    // apagado, como na inativação); a pessoa entra de novo e recebe o perfil atual. Sem isso,
    // um Administrador rebaixado manteria as rotas de Administrador até o token vencer.
    public sealed class ContaAtivaMiddleware
    {
        public const string Mensagem = "Sua conta de acesso está inativa. Procure o administrador.";
        public const string MensagemPerfilAlterado = "Seu perfil de acesso foi alterado. Entre novamente.";

        private readonly RequestDelegate _proximo;

        public ContaAtivaMiddleware(RequestDelegate proximo)
        {
            _proximo = proximo;
        }

        public async Task InvokeAsync(HttpContext contexto, KoinoniaHubDbContext db)
        {
            var endpoint = contexto.GetEndpoint();
            var publico = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

            if (endpoint is not null && !publico && contexto.User.Identity?.IsAuthenticated == true)
            {
                var recusa = await MotivoDaRecusaAsync(contexto.User, db, contexto.RequestAborted);
                if (recusa is not null)
                {
                    contexto.Response.Cookies.Delete(
                        AuthController.NomeCookieToken,
                        AuthController.OpcoesCookie(DateTime.UtcNow.AddDays(-1)));
                    contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await contexto.Response.WriteAsJsonAsync(new { mensagem = recusa }, contexto.RequestAborted);
                    return;
                }
            }

            await _proximo(contexto);
        }

        // Nulo quando a sessão continua válida; senão, a mensagem do 401: conta inativa (ou
        // token sem identificação) ou perfil do token diferente do perfil atual da conta. Uma
        // consulta só, que lê o perfil da conta ativa. A consulta e a escrita da resposta recebem
        // o token da requisição: se o cliente desistir, a operação é cancelada.
        private static async Task<string?> MotivoDaRecusaAsync(
            ClaimsPrincipal usuario, KoinoniaHubDbContext db, CancellationToken cancelamento)
        {
            if (!int.TryParse(usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var usuarioId) ||
                !int.TryParse(usuario.FindFirst("IgrejaId")?.Value, out var igrejaId))
                return Mensagem;

            var perfilAtual = await db.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == usuarioId && u.IgrejaId == igrejaId && u.Ativo)
                .Select(u => u.Perfil)
                .FirstOrDefaultAsync(cancelamento);

            if (perfilAtual is null)
                return Mensagem;

            var perfilDoToken = usuario.FindFirst(ClaimTypes.Role)?.Value;
            return string.Equals(perfilAtual, perfilDoToken, StringComparison.Ordinal)
                ? null
                : MensagemPerfilAlterado;
        }
    }
}
