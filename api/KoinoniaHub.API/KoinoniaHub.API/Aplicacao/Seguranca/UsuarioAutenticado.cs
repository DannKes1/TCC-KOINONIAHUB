using System.Security.Claims;

namespace KoinoniaHub.API.Aplicacao.Seguranca
{
    public static class UsuarioAutenticado
    {
        public static int ObterIgrejaId(ClaimsPrincipal user)
        {
            var valor = user.FindFirst("IgrejaId")?.Value;
            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidOperationException("Token sem IgrejaId.");

            return int.Parse(valor);
        }

        public static int ObterUsuarioId(ClaimsPrincipal user)
        {
            var valor = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidOperationException("Token sem UsuarioId.");

            return int.Parse(valor);
        }

        public static string ObterPerfil(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? "Usuario";
        }
    }
}
