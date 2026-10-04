using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KoinoniaHub.API.Aplicacao.Seguranca
{
    // RNF 2.5 / 42.1 na API (decisão do autor na 5.1): uma conta autenticada que
    // ainda não aceitou a versão vigente do Termo de Uso e Sigilo só alcança os
    // endpoints públicos ([AllowAnonymous]) e os marcados com [PermitirSemAceiteTermo];
    // qualquer outro devolve 403 com { mensagem, termoPendente: true }, que o front
    // usa para levar à tela do termo. Requisições anônimas não são tratadas aqui: a
    // autenticação (401) e os endpoints públicos continuam como estão.
    //
    // Registrado como filtro global em Program.cs; roda depois do UseAuthorization,
    // portanto só vê requisições já autenticadas e autorizadas por perfil.
    public sealed class ExigeAceiteTermoFiltro : IAsyncAuthorizationFilter
    {
        public const string Mensagem = "Aceite o Termo de Uso e Sigilo para continuar.";

        private readonly IAceiteTermoServico _aceiteTermoServico;

        public ExigeAceiteTermoFiltro(IAceiteTermoServico aceiteTermoServico)
        {
            _aceiteTermoServico = aceiteTermoServico;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var usuario = context.HttpContext.User;
            if (usuario.Identity?.IsAuthenticated != true)
                return;

            if (PermiteSemAceite(context))
                return;

            var usuarioId = UsuarioAutenticado.ObterUsuarioId(usuario);
            if (await _aceiteTermoServico.PossuiAceiteVigenteAsync(usuarioId))
                return;

            context.Result = new ObjectResult(new { mensagem = Mensagem, termoPendente = true })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        // Liberados: endpoints públicos (um navegador com cookie de conta pendente
        // precisa conseguir fazer login de novo; registrar-admin e primeiro-acesso têm
        // o próprio gate do aceite) e os marcados com [PermitirSemAceiteTermo]. Os
        // atributos entram nos metadados do endpoint quando aplicados à action ou ao
        // controller; o ActionDescriptor é a segunda fonte, pelo mesmo motivo.
        private static bool PermiteSemAceite(AuthorizationFilterContext context)
        {
            var metadadosEndpoint = context.HttpContext.GetEndpoint()?.Metadata;
            if (metadadosEndpoint?.GetMetadata<IAllowAnonymous>() is not null)
                return true;
            if (metadadosEndpoint?.GetMetadata<PermitirSemAceiteTermoAttribute>() is not null)
                return true;

            var metadadosAction = context.ActionDescriptor.EndpointMetadata;
            return metadadosAction.OfType<IAllowAnonymous>().Any()
                || metadadosAction.OfType<PermitirSemAceiteTermoAttribute>().Any();
        }
    }
}
