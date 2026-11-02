using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Controllers
{
    // Vivacidade da API para o healthcheck do contêiner (implantação, Entrega F): responde
    // 200 assim que o host está no ar, sem tocar no banco e sem expor versão ou ambiente.
    // Público e fora do gate do termo: a sonda não tem sessão.
    [ApiController]
    [Route("api/saude")]
    public class SaudeController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Obter() => Ok(new { situacao = "ok" });
    }
}
