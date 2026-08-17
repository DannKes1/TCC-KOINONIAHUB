using System.Security.Cryptography;
using System.Text;

namespace KoinoniaHub.API.Aplicacao.Seguranca
{
    // Gera e valida tokens de convite de primeiro acesso.
    // O banco guarda somente o hash SHA-256; o token em claro
    // é exibido uma única vez ao administrador.
    public static class ConviteTokenHelper
    {
        public static string GerarToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);

            // Base64 "url-safe" para o token poder ir na query string do link.
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        public static string CalcularHash(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
