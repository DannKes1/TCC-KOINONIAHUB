using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/pessoas/{pessoaId:int}/parentescos")]
    [Authorize]
    public class ParentescosController : ControllerBase
    {
        private readonly IParentescoServico _servico;

        public ParentescosController(IParentescoServico servico)
        {
            _servico = servico;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Pastor,Superintendente")]
        public async Task<IActionResult> Adicionar([FromRoute] int pessoaId, [FromBody] ParentescoCriarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            try
            {
                var resposta = await _servico.AdicionarAsync(igrejaId, pessoaId, dto);
                return Ok(resposta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Pastor,Superintendente,Professor")]
        public async Task<IActionResult> Listar([FromRoute] int pessoaId)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var resposta = await _servico.ListarAsync(igrejaId, pessoaId);
            return Ok(resposta);
        }

        [HttpDelete("{parentescoId:int}")]
        [Authorize(Roles = "Admin,Pastor,Superintendente")]
        public async Task<IActionResult> Remover([FromRoute] int pessoaId, [FromRoute] int parentescoId)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var ok = await _servico.RemoverAsync(igrejaId, pessoaId, parentescoId);
            return ok ? NoContent() : NotFound();
        }
    }
}