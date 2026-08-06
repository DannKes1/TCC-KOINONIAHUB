using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/aulas/{aulaId:int}/presencas")]
    [Authorize]
    public class ChamadasController : ControllerBase
    {
        private readonly IChamadaServico _servico;
        private readonly IAutorizacaoEbdServico _autorizacao;

        public ChamadasController(IChamadaServico servico, IAutorizacaoEbdServico autorizacao)
        {
            _servico = servico;
            _autorizacao = autorizacao;
        }

        [HttpPost]
        public async Task<IActionResult> Registrar([FromRoute] int aulaId, [FromBody] ChamadaRegistrarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoAulaAsync(igrejaId, usuarioId, perfil, aulaId);

                var resposta = await _servico.RegistrarAsync(igrejaId, aulaId, dto);
                return Ok(resposta);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromRoute] int aulaId)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoAulaAsync(igrejaId, usuarioId, perfil, aulaId);

                var resposta = await _servico.ListarAsync(igrejaId, aulaId);
                return Ok(resposta);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = ex.Message });
            }
        }

        [HttpGet("/api/aulas/{aulaId:int}/chamada")]
        public async Task<IActionResult> ObterChamadaCompleta([FromRoute] int aulaId)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoAulaAsync(igrejaId, usuarioId, perfil, aulaId);

                var resposta = await _servico.ObterChamadaCompletaAsync(igrejaId, aulaId);
                return Ok(resposta);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}