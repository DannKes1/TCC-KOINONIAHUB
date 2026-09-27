using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/pessoas/{pessoaId:int}/presencas")]
    [Authorize]
    public class PresencasPessoaController : ControllerBase
    {
        private readonly IPresencaHistoricoServico _servico;
        private readonly IAutorizacaoEbdServico _autorizacao;

        public PresencasPessoaController(IPresencaHistoricoServico servico, IAutorizacaoEbdServico autorizacao)
        {
            _servico = servico;
            _autorizacao = autorizacao;
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromRoute] int pessoaId)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoPessoaAsync(igrejaId, usuarioId, perfil, pessoaId);

                IReadOnlyCollection<int>? departamentosPermitidos = null;
                if (!Perfis.EhAdministrativo(perfil) && !Perfis.EhUsuarioComum(perfil))
                    departamentosPermitidos = await _autorizacao.ListarDepartamentosComAtribuicaoAtivaAsync(igrejaId, usuarioId);

                var resposta = await _servico.ListarPorPessoaAsync(igrejaId, pessoaId, departamentosPermitidos);
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
    }
}
