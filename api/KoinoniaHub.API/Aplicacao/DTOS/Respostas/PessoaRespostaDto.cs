namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class PessoaRespostaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? CPF { get; set; }
        public DateTime? DataNascimento { get; set; }
        public string? Sexo { get; set; }
        public string? EstadoCivil { get; set; }
        public string Situacao { get; set; } = "Ativo";
        public string Categoria { get; set; } = "Membro";
        public DateTime? DataInativacao { get; set; }
        public string? Telefone { get; set; }
        public string? Celular { get; set; }
        public string? Email { get; set; }
        public string? Endereco { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string? CEP { get; set; }
        public DateTime? DataBatismo { get; set; }
        public DateTime? DataMembresia { get; set; }
        public string? FotoUrl { get; set; }
        public string? Observacoes { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}