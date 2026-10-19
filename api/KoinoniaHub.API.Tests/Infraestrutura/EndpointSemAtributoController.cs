using Microsoft.AspNetCore.Mvc;

namespace KoinoniaHub.API.Tests.Infraestrutura
{
    // Controller que existe só no projeto de testes (registrado na fábrica por
    // AddApplicationPart) para provar a FallbackPolicy do Program.cs (7.1): uma rota
    // sem [Authorize] nem [AllowAnonymous] precisa devolver 401 a quem não tem sessão.
    // A API em si não tem nenhuma rota assim — e é exatamente isso que a política
    // garante para as próximas.
    [ApiController]
    [Route("api/testes/sem-atributo")]
    public class EndpointSemAtributoController : ControllerBase
    {
        [HttpGet]
        public IActionResult Obter() => Ok(new { ok = true });
    }
}
