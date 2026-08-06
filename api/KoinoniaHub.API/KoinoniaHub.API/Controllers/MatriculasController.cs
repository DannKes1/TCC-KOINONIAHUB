using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/departamentos/{departamentoId:int}")]
    [Authorize]
    public class MatriculasController : ControllerBase
    {
        private readonly IMatriculaServico _servico;
        private readonly IAutorizacaoEbdServico _autorizacao;

        public MatriculasController(IMatriculaServico servico, IAutorizacaoEbdServico autorizacao)
        {
            _servico = servico;
            _autorizacao = autorizacao;
        }

        [HttpPost("matriculas")]
        public async Task<IActionResult> Matricular([FromRoute] int departamentoId, [FromBody] MatriculaCriarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoDepartamentoAsync(igrejaId, usuarioId, perfil, departamentoId);

                var resposta = await _servico.MatricularAsync(igrejaId, departamentoId, dto);
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

        [HttpGet("alunos")]
        public async Task<IActionResult> ListarAlunos([FromRoute] int departamentoId)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoDepartamentoAsync(igrejaId, usuarioId, perfil, departamentoId);

                var resposta = await _servico.ListarAlunosDaClasseAsync(igrejaId, departamentoId);
                return Ok(resposta);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = ex.Message });
            }
        }

        [HttpGet("pessoas-disponiveis")]
        public async Task<IActionResult> ListarPessoasDisponiveis([FromRoute] int departamentoId)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoDepartamentoAsync(igrejaId, usuarioId, perfil, departamentoId);

                var resposta = await _servico.ListarPessoasDisponiveisAsync(igrejaId, departamentoId);
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



        [HttpDelete("matriculas/{matriculaId:int}")]
        public async Task<IActionResult> Inativar([FromRoute] int departamentoId, [FromRoute] int matriculaId)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoDepartamentoAsync(igrejaId, usuarioId, perfil, departamentoId);

                var ok = await _servico.InativarMatriculaAsync(igrejaId, departamentoId, matriculaId);
                if (!ok) return NotFound();

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = ex.Message });
            }
        }
    }
}