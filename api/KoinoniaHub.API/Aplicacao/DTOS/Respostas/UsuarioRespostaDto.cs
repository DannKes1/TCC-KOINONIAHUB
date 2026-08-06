namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class UsuarioRespostaDto
    {
        public int Id { get; set; }
        public int IgrejaId { get; set; }

        public string Email { get; set; } = string.Empty;
        public string Perfil { get; set; } = "Usuario";
        public bool Ativo { get; set; }

        public int? PessoaId { get; set; }
        public string? NomePessoa { get; set; }
    }
}