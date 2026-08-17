using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    [Authorize(Roles = "Admin")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioServico _servico;

        public UsuariosController(IUsuarioServico servico)
        {
            _servico = servico;
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] UsuarioCriarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            try
            {
                var resposta = await _servico.CriarParaPessoaAsync(igrejaId, dto);
                return Ok(resposta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Gera (ou regenera) um convite de primeiro acesso para a conta.
        // O token retorna em claro apenas nesta resposta; no banco fica só o hash.
        [HttpPost("{id:int}/convite")]
        public async Task<IActionResult> GerarConvite([FromRoute] int id)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            try
            {
                var resposta = await _servico.GerarConviteAsync(igrejaId, id);
                if (resposta is null) return NotFound();
                return Ok(resposta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var resposta = await _servico.ListarAsync(igrejaId);
            return Ok(resposta);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId([FromRoute] int id)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var resposta = await _servico.ObterPorIdAsync(igrejaId, id);
            if (resposta is null) return NotFound();
            return Ok(resposta);
        }

        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Atualizar([FromRoute] int id, [FromBody] UsuarioAtualizarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioLogadoId = UsuarioAutenticado.ObterUsuarioId(User);

            try
            {
                var ok = await _servico.AtualizarAsync(igrejaId, id, usuarioLogadoId, dto);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPatch("{id:int}/resetar-senha")]
        public async Task<IActionResult> ResetarSenha([FromRoute] int id, [FromBody] UsuarioResetarSenhaRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            var ok = await _servico.ResetarSenhaAsync(igrejaId, id, dto);
            if (!ok) return NotFound();

            return NoContent();
        }
    }
}
