using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/departamentos")]
    [Authorize]
    public class DepartamentosController : ControllerBase
    {
        private readonly IDepartamentoServico _servico;

        public DepartamentosController(IDepartamentoServico servico)
        {
            _servico = servico;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Pastor,Superintendente")]
        public async Task<IActionResult> Criar([FromBody] DepartamentoCriarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            try
            {
                var resposta = await _servico.CriarAsync(igrejaId, dto);
                return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
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
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            var resposta = await _servico.ListarAsync(igrejaId, usuarioId, perfil);
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

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Pastor,Superintendente")]
        public async Task<IActionResult> Atualizar([FromRoute] int id, [FromBody] DepartamentoAtualizarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            try
            {
                var ok = await _servico.AtualizarAsync(igrejaId, id, dto);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}