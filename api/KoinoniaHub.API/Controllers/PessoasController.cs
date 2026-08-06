using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/pessoas")]
    [Authorize]
    public class PessoasController : ControllerBase
    {
        private readonly IPessoaServico _pessoaServico;
        private readonly KoinoniaHubDbContext _db;

        public PessoasController(IPessoaServico pessoaServico, KoinoniaHubDbContext db)
        {
            _pessoaServico = pessoaServico;
            _db = db;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Pastor,Superintendente")]
        public async Task<IActionResult> Criar([FromBody] PessoaCriarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            try
            {
                var resposta = await _pessoaServico.CriarAsync(igrejaId, dto);
                return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Pastor,Superintendente")]
        public async Task<IActionResult> Listar()
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var resposta = await _pessoaServico.ListarAsync(igrejaId);
            return Ok(resposta);
        }

        //[HttpGet("{id:int}")]
        //public async Task<IActionResult> ObterPorId([FromRoute] int id)
        //{
        //    var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

        //    var resposta = await _pessoaServico.ObterPorIdAsync(igrejaId, id);
        //    if (resposta is null) return NotFound();

        //    return Ok(resposta);
        //}

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId([FromRoute] int id)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            
            if (string.Equals(perfil, "Usuario", StringComparison.OrdinalIgnoreCase))
            {
                var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
                var usuario = await _db.Usuarios.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == usuarioId && u.IgrejaId == igrejaId);

                if (usuario?.PessoaId != id)
                    return StatusCode(403, new { mensagem = "Você só pode visualizar seus próprios dados." });
            }

            var resposta = await _pessoaServico.ObterPorIdAsync(igrejaId, id);
            if (resposta is null) return NotFound();
            return Ok(resposta);
        }



        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Pastor,Superintendente")]
        public async Task<IActionResult> Atualizar([FromRoute] int id, [FromBody] PessoaAtualizarRequisicaoDto dto)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            try
            {
                var ok = await _pessoaServico.AtualizarAsync(igrejaId, id, dto);
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
