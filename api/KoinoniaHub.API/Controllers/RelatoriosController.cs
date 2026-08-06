using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/relatorios/ebd")]
    [Authorize]
    public class RelatoriosController : ControllerBase
    {
        private readonly IRelatorioEbdServico _servico;
        private readonly IAutorizacaoEbdServico _autorizacao;

        public RelatoriosController(IRelatorioEbdServico servico, IAutorizacaoEbdServico autorizacao)
        {
            _servico = servico;
            _autorizacao = autorizacao;
        }

        private static DateTime ToUtc(DateTime dt) =>
            dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc) 
            };

        [HttpGet("frequencia-turma")]
        public async Task<IActionResult> FrequenciaTurma(
            [FromQuery] int departamentoId,
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            var inicio = ToUtc((dataInicio ?? DateTime.UtcNow.AddDays(-30)));
            var fim = ToUtc((dataFim ?? DateTime.UtcNow));

            if (fim.Date < inicio.Date)
                return BadRequest(new { mensagem = "dataFim não pode ser menor que dataInicio." });

            try
            {
                await _autorizacao.GarantirAcessoDepartamentoAsync(igrejaId, usuarioId, perfil, departamentoId);

                var resposta = await _servico.ObterFrequenciaTurmaAsync(igrejaId, departamentoId, inicio, fim);
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

        [HttpGet("acompanhamento")]
        public async Task<IActionResult> Acompanhamento(
            [FromQuery] int departamentoId,
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim,
            [FromQuery] decimal limiarAtencao = 75m,
            [FromQuery] decimal limiarCritico = 50m,
            [FromQuery] int faltasConsecutivasCritico = 3)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            
            var inicio = ToUtc((dataInicio ?? DateTime.UtcNow.AddDays(-60)));
            var fim = ToUtc((dataFim ?? DateTime.UtcNow));

            if (fim.Date < inicio.Date)
                return BadRequest(new { mensagem = "dataFim não pode ser menor que dataInicio." });

            try
            {
                await _autorizacao.GarantirAcessoDepartamentoAsync(igrejaId, usuarioId, perfil, departamentoId);

                var resposta = await _servico.ObterPainelAcompanhamentoAsync(
                    igrejaId, departamentoId, inicio, fim,
                    limiarAtencao, limiarCritico, faltasConsecutivasCritico);

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

      
        [HttpGet("ranking-faltas")]
        public async Task<IActionResult> RankingFaltas(
            [FromQuery] int departamentoId,
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim,
            [FromQuery] int top = 10)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            var inicio = ToUtc((dataInicio ?? DateTime.UtcNow.AddDays(-30)));
            var fim = ToUtc((dataFim ?? DateTime.UtcNow));

            if (fim.Date < inicio.Date)
                return BadRequest(new { mensagem = "dataFim não pode ser menor que dataInicio." });

            try
            {
                await _autorizacao.GarantirAcessoDepartamentoAsync(igrejaId, usuarioId, perfil, departamentoId);

                var resposta = await _servico.ObterRankingFaltasAsync(igrejaId, departamentoId, inicio, fim, top);
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

     
        [HttpGet("resumo-dia")]
        public async Task<IActionResult> ResumoDia([FromQuery] DateTime? data)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            var perfisAdministrativos = new[] { "Admin", "Pastor", "Superintendente" };
            if (!perfisAdministrativos.Contains(perfil))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensagem = "Acesso restrito aos perfis de gestão." });

            var dia = ToUtc(data ?? DateTime.UtcNow);
            var resposta = await _servico.ObterResumoDoDiaAsync(igrejaId, dia);
            return Ok(resposta);
        }

        
        [HttpGet("minha-frequencia")]
        public async Task<IActionResult> MinhaFrequencia(
            [FromQuery] int departamentoId,
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);

            // Janela padrão: últimos 90 dias
            var inicio = ToUtc((dataInicio ?? DateTime.UtcNow.AddDays(-90)));
            var fim = ToUtc((dataFim ?? DateTime.UtcNow));

            if (fim.Date < inicio.Date)
                return BadRequest(new { mensagem = "dataFim não pode ser menor que dataInicio." });

            try
            {
                var resposta = await _servico.ObterMinhaFrequenciaTurmaAsync(igrejaId, usuarioId, departamentoId, inicio, fim);
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