using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    // Termo de Uso e Sigilo (RF42 / RF43; Plano 6.3).
    [ApiController]
    [Route("api/termo")]
    public class TermoController : ControllerBase
    {
        private readonly IAceiteTermoServico _servico;

        public TermoController(IAceiteTermoServico servico)
        {
            _servico = servico;
        }

        // Público: usado pelas telas de cadastro inicial e de primeiro acesso antes de
        // existir sessão. Com sessão, devolve também a igreja do usuário; na tela de
        // primeiro acesso, `token` (o convite) identifica a igreja (RNF 42.7).
        [HttpGet("vigente")]
        [AllowAnonymous]
        public async Task<IActionResult> Vigente([FromQuery] string? token)
        {
            int? igrejaId = User.Identity?.IsAuthenticated == true
                ? UsuarioAutenticado.ObterIgrejaId(User)
                : null;

            var resposta = await _servico.ObterVigenteAsync(igrejaId, token);
            return Ok(resposta);
        }

        // Aceite após o login (RNF 2.5 / 13.4): Meio = Login.
        [HttpPost("aceitar")]
        [Authorize]
        public async Task<IActionResult> Aceitar([FromBody] TermoAceitarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);

            try
            {
                var resposta = await _servico.RegistrarAsync(usuarioId, igrejaId, dto.Versao, ObterIpOrigem());
                return Ok(resposta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // RNF 42.8: endereço de origem da requisição, só para auditoria do aceite.
        private string? ObterIpOrigem() => HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
