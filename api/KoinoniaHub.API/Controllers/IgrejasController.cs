using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/igrejas")]
    public class IgrejasController : ControllerBase
    {
        private readonly IIgrejaServico _igrejaServico;

        public IgrejasController(IIgrejaServico igrejaServico)
        {
            _igrejaServico = igrejaServico;
        }

 
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] IgrejaCriarRequisicaoDto dto)
        {
            var resposta = await _igrejaServico.CriarAsync(dto);
            return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId([FromRoute] int id)
        {
         
            var igrejaIdDoToken = UsuarioAutenticado.ObterIgrejaId(User);
            if (id != igrejaIdDoToken)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensagem = "Acesso permitido apenas aos dados da própria igreja." });

            var resposta = await _igrejaServico.ObterPorIdAsync(id);
            if (resposta is null) return NotFound();
            return Ok(resposta);
        }
    }
}
