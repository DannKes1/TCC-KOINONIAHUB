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

        // Valida um convite de primeiro acesso (tela pública).
        // Retorna o e-mail/nome apenas para a página cumprimentar a pessoa.
        [HttpGet("primeiro-acesso/{token}")]
        public async Task<IActionResult> ValidarPrimeiroAcesso([FromRoute] string token)
        {
            try
            {
                var resposta = await _authServico.ValidarConviteAsync(token);
                if (resposta is null)
                    return NotFound(new { mensagem = "Convite inválido ou já utilizado. Solicite um novo link ao administrador." });

                return Ok(resposta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Consome o convite: a própria pessoa define a senha (uso único).
        [HttpPost("primeiro-acesso")]
        public async Task<IActionResult> AtivarPrimeiroAcesso([FromBody] PrimeiroAcessoAtivarRequisicaoDto dto)
        {
            try
            {
                var resposta = await _authServico.AtivarPrimeiroAcessoAsync(dto);
                return Ok(new
                {
                    mensagem = "Senha definida com sucesso. Você já pode entrar no sistema.",
                    email = resposta.Email
                });
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
