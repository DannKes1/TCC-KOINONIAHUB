using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace KoinoniaHub.API.Aplicacao.Seguranca
{
    // Corretiva da 7.3 (decisão do autor): o 403 de [Authorize(Roles)] e da FallbackPolicy
    // passa a ter corpo { mensagem }, como o resto da API. Antes saía do middleware de
    // autorização com corpo vazio. Nenhum código de status muda: sem sessão continua 401
    // (Challenge), que segue para o tratamento padrão.
    public sealed class RespostaAutorizacaoHandler : IAuthorizationMiddlewareResultHandler
    {
        public const string MensagemSemPermissao = "Você não tem permissão para esta operação.";

        private readonly AuthorizationMiddlewareResultHandler _padrao = new();

        public async Task HandleAsync(
            RequestDelegate next,
            HttpContext context,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Forbidden)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { mensagem = MensagemSemPermissao });
                return;
            }

            await _padrao.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}
