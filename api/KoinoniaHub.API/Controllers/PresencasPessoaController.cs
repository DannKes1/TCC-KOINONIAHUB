using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/pessoas/{pessoaId:int}/presencas")]
    [Authorize]
    public class PresencasPessoaController : ControllerBase
    {
        private readonly IPresencaHistoricoServico _servico;
        private readonly KoinoniaHubDbContext _db;

        public PresencasPessoaController(IPresencaHistoricoServico servico, KoinoniaHubDbContext db)
        {
            _servico = servico;
            _db = db;
        }


        [HttpGet]
        public async Task<IActionResult> Listar([FromRoute] int pessoaId)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            
            if (string.Equals(perfil, "Usuario", StringComparison.OrdinalIgnoreCase))
            {
                var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
                var usuario = await _db.Usuarios.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == usuarioId && u.IgrejaId == igrejaId);

                if (usuario?.PessoaId != pessoaId)
                    return StatusCode(403, new { mensagem = "Você só pode visualizar suas próprias presenças." });
            }

            try
            {
                var resposta = await _servico.ListarPorPessoaAsync(igrejaId, pessoaId);
                return Ok(resposta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}