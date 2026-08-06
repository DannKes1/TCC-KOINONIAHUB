using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string NomeCookieToken = "kh_token";
        private readonly IAuthServico _authServico;

        public AuthController(IAuthServico authServico)
        {
            _authServico = authServico;
        }

        [HttpPost("registrar-admin")]
        public async Task<IActionResult> RegistrarAdmin([FromBody] RegistrarAdminRequisicaoDto dto)
        {
            try
            {
                var resposta = await _authServico.RegistrarAdminAsync(dto);
                GravarCookieToken(resposta.Token, resposta.ExpiraEm);
                return Ok(resposta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequisicaoDto dto)
        {
            try
            {
                var resposta = await _authServico.LoginAsync(dto);
                GravarCookieToken(resposta.Token, resposta.ExpiraEm);
                return Ok(resposta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Encerra a sessão removendo o cookie httpOnly do navegador.
        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(NomeCookieToken, OpcoesCookie(DateTime.UtcNow.AddDays(-1)));
            return NoContent();
        }

        
        private void GravarCookieToken(string token, DateTime expiraEm)
        {
            Response.Cookies.Append(NomeCookieToken, token, OpcoesCookie(expiraEm));
        }

     
        private CookieOptions OpcoesCookie(DateTime expiraEm) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Expires = expiraEm
        };
    }
}