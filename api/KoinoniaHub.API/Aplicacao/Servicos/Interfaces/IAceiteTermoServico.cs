using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    // Termo de Uso e Sigilo (RF42/RF43). Única porta para validar a versão vigente,
    // montar e gravar registros de aceite e consultá-los.
    public interface IAceiteTermoServico
    {
        // Versão vigente com texto e hash; a igreja vem do token autenticado ou de um
        // token de convite (tela pública de primeiro acesso), quando houver.
        Task<TermoVigenteRespostaDto> ObterVigenteAsync(int? igrejaId = null, string? tokenConvite = null);

        // Valida que `versao` é a vigente e devolve um AceiteTermo NÃO rastreado, para
        // o chamador gravar na mesma transação de outras alterações (cadastro inicial,
        // primeiro acesso). Lança InvalidOperationException quando não é a vigente.
        AceiteTermo Montar(int igrejaId, string? versao, string meio, string? ip, Usuario? usuario = null, int usuarioId = 0);

        // Fluxo do login (POST /api/termo/aceitar): grava o aceite da versão vigente.
        // Idempotente: um aceite já existente da mesma versão é devolvido sem duplicar.
        Task<AceiteTermoRespostaDto> RegistrarAsync(int usuarioId, int igrejaId, string? versao, string? ip);

        Task<bool> PossuiAceiteVigenteAsync(int usuarioId);

        // Aceite mais recente da conta, ou null (RF43 — Meus Dados).
        Task<AceiteTermoRespostaDto?> ObterUltimoAceiteAsync(int usuarioId);

        // Aceite mais recente por usuário da igreja (RF43 — listagem de usuários).
        Task<Dictionary<int, AceiteTermoRespostaDto>> ObterUltimosAceitesAsync(int igrejaId);
    }
}
