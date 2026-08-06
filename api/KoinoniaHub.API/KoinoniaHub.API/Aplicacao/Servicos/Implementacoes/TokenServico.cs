using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class TokenServico : ITokenServico
    {
        private readonly IConfiguration _configuracao;

        public TokenServico(IConfiguration configuracao)
        {
            _configuracao = configuracao;
        }

        public (string token, DateTime expiraEm) GerarToken(Usuario usuario)
        {
            var chave = _configuracao["Jwt:ChaveSecreta"] ?? throw new Exception("Jwt:ChaveSecreta não configurado.");
            var emissor = _configuracao["Jwt:Emissor"] ?? "KoinoniaHub";
            var audiencia = _configuracao["Jwt:Audiencia"] ?? "KoinoniaHub";

            var expiraEm = DateTime.UtcNow.AddHours(8);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Perfil),
                new Claim("IgrejaId", usuario.IgrejaId.ToString())
            };

            var credenciais = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: emissor,
                audience: audiencia,
                claims: claims,
                expires: expiraEm,
                signingCredentials: credenciais
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
        }
    }
}
