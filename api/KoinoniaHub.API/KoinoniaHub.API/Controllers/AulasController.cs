using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/aulas")]
    [Authorize]
    public class AulasController : ControllerBase
    {
        private readonly IAulaServico _servico;
        private readonly IAutorizacaoEbdServico _autorizacao;

        public AulasController(IAulaServico servico, IAutorizacaoEbdServico autorizacao)
        {
            _servico = servico;
            _autorizacao = autorizacao;
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] AulaCriarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoMateriaAsync(igrejaId, usuarioId, perfil, dto.MateriaId);

                var resposta = await _servico.CriarAsync(igrejaId, dto);
                return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
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
        public async Task<IActionResult> Listar([FromQuery] int departamentoId)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoDepartamentoAsync(igrejaId, usuarioId, perfil, departamentoId);

                var resposta = await _servico.ListarPorDepartamentoAsync(igrejaId, departamentoId);
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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId([FromRoute] int id)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoAulaAsync(igrejaId, usuarioId, perfil, id);

                var resposta = await _servico.ObterPorIdAsync(igrejaId, id);
                if (resposta is null) return NotFound();

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

        [HttpPatch("{id:int}/consolidar")]
        public async Task<IActionResult> Consolidar([FromRoute] int id)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoAulaAsync(igrejaId, usuarioId, perfil, id);

                var ok = await _servico.ConsolidarAsync(igrejaId, id);
                if (!ok) return NotFound();

                return NoContent();
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