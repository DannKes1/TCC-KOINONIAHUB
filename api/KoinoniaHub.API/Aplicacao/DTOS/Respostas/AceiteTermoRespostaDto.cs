namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    // RF43: resumo do aceite mais recente de uma conta.
    public class AceiteTermoRespostaDto
    {
        public string Versao { get; set; } = string.Empty;
        public DateTime AceitoEm { get; set; }
        public string Meio { get; set; } = string.Empty;

        // true quando a versão aceita é a vigente; false indica aceite pendente de nova versão.
        public bool Vigente { get; set; }
    }
}
