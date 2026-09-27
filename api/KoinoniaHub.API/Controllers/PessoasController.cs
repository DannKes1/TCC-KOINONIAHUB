using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    [ApiController]
    [Route("api/pessoas")]
    [Authorize]
    public class PessoasController : ControllerBase
    {
        private readonly IPessoaServico _pessoaServico;
        private readonly IPessoaImportacaoServico _importacaoServico;
        private readonly IAutorizacaoEbdServico _autorizacao;

        public PessoasController(
            IPessoaServico pessoaServico,
            IPessoaImportacaoServico importacaoServico,
            IAutorizacaoEbdServico autorizacao)
        {
            _pessoaServico = pessoaServico;
            _importacaoServico = importacaoServico;
            _autorizacao = autorizacao;
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

        // Importa pessoas em lote a partir de um arquivo CSV
        // (ex.: rol de membros mantido pela secretaria).
        [HttpPost("importar")]
        [Authorize(Roles = "Admin,Pastor,Superintendente")]
        [RequestSizeLimit(2_000_000)]
        public async Task<IActionResult> Importar(IFormFile? arquivo)
        {
            if (arquivo is null || arquivo.Length == 0)
                return BadRequest(new { mensagem = "Envie um arquivo CSV." });

            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            if (extensao != ".csv" && extensao != ".txt")
                return BadRequest(new { mensagem = "Formato inválido. Envie um arquivo .csv (você pode baixar o modelo na tela de importação)." });

            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);

            try
            {
                await using var conteudo = arquivo.OpenReadStream();
                var resposta = await _importacaoServico.ImportarCsvAsync(igrejaId, conteudo);
                return Ok(resposta);
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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId([FromRoute] int id)
        {
            var igrejaId = UsuarioAutenticado.ObterIgrejaId(User);
            var usuarioId = UsuarioAutenticado.ObterUsuarioId(User);
            var perfil = UsuarioAutenticado.ObterPerfil(User);

            try
            {
                await _autorizacao.GarantirAcessoPessoaAsync(igrejaId, usuarioId, perfil, id);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }

            if (Perfis.EhAdministrativo(perfil) || Perfis.EhUsuarioComum(perfil))
            {
                var resposta = await _pessoaServico.ObterPorIdAsync(igrejaId, id);
                if (resposta is null) return NotFound();
                return Ok(resposta);
            }

            var reduzida = await _pessoaServico.ObterParaTurmaAsync(igrejaId, id);
            if (reduzida is null) return NotFound();
            return Ok(reduzida);
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
