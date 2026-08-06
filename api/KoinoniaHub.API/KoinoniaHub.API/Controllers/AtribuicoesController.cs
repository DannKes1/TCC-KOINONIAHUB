using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/atribuicoes")]
    [Authorize]
    public class AtribuicoesController : ControllerBase
    {
        private readonly IAtribuicaoServico _servico;

        public AtribuicoesController(IAtribuicaoServico servico)
        {
            _servico = servico;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Pastor,Superintendente")]
        public async Task<IActionResult> Criar([FromBody] AtribuicaoCriarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            try
            {
                var resposta = await _servico.CriarAsync(igrejaId, dto);
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

        [HttpGet("departamento/{departamentoId:int}")]
        public async Task<IActionResult> ListarPorDepartamento([FromRoute] int departamentoId, [FromQuery] string? funcao, [FromQuery] bool? ativo)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var resposta = await _servico.ListarPorDepartamentoAsync(igrejaId, departamentoId, funcao, ativo);
            return Ok(resposta);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId([FromRoute] int id)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var resposta = await _servico.ObterPorIdAsync(igrejaId, id);
            return resposta is null ? NotFound() : Ok(resposta);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Pastor,Superintendente")]
        public async Task<IActionResult> Atualizar([FromRoute] int id, [FromBody] AtribuicaoAtualizarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            try
            {
                var ok = await _servico.AtualizarAsync(igrejaId, id, dto);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPatch("{id:int}/encerrar")]
        [Authorize(Roles = "Admin,Pastor,Superintendente")]
        public async Task<IActionResult> Encerrar([FromRoute] int id)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            var ok = await _servico.EncerrarAsync(igrejaId, id);
            return ok ? NoContent() : NotFound();
        }
    }
}