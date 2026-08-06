using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOS.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/meus-dados")]
    [Authorize]
    public class MeusDadosController : ControllerBase
    {
        private readonly IPessoaServico _pessoaServico;

        public MeusDadosController(IPessoaServico pessoaServico)
        {
            _pessoaServico = pessoaServico;
        }

        [HttpGet]
        public async Task<IActionResult> Obter()
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);

            var resposta = await _pessoaServico.ObterMeusDadosAsync(igrejaId, usuarioId);
            if (resposta is null)
                return NotFound(new { mensagem = "Seu usuário não está vinculado a uma pessoa. Procure o administrador." });

            return Ok(resposta);
        }

        [HttpPut]
        public async Task<IActionResult> Atualizar([FromBody] MeusDadosAtualizarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);

            try
            {
                var ok = await _pessoaServico.AtualizarMeusDadosAsync(igrejaId, usuarioId, dto);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("minhas-turmas")]
        public async Task<IActionResult> ListarMinhasTurmas()
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);

            var resposta = await _pessoaServico.ListarMinhasTurmasAsync(igrejaId, usuarioId);
            return Ok(resposta);
        }
    }
}