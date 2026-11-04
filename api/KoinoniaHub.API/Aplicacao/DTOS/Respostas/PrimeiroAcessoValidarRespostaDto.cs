namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class PrimeiroAcessoValidarRespostaDto
    {
        public string Email { get; set; } = string.Empty;
        public string? NomePessoa { get; set; }

        // Entrega G: true quando a conta já aceitou a versão vigente do termo (caso da
        // redefinição de acesso, CSU20). A tela dispensa a caixa de aceite e a ativação
        // não exige nem grava um novo aceite.
        public bool AceiteVigente { get; set; }
    }
}
