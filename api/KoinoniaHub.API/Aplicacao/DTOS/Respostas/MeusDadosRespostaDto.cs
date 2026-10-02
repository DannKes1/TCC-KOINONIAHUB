namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    // GET /api/meus-dados: os dados da pessoa (RF4) mais o aceite do termo (RF43).
    public class MeusDadosRespostaDto : PessoaRespostaDto
    {
        public AceiteTermoRespostaDto? AceiteTermo { get; set; }

        public static MeusDadosRespostaDto Montar(PessoaRespostaDto pessoa, AceiteTermoRespostaDto? aceite) => new()
        {
            Id = pessoa.Id,
            Nome = pessoa.Nome,
            DataNascimento = pessoa.DataNascimento,
            Sexo = pessoa.Sexo,
            EstadoCivil = pessoa.EstadoCivil,
            Situacao = pessoa.Situacao,
            DataInativacao = pessoa.DataInativacao,
            Celular = pessoa.Celular,
            Email = pessoa.Email,
            Endereco = pessoa.Endereco,
            Bairro = pessoa.Bairro,
            Cidade = pessoa.Cidade,
            Estado = pessoa.Estado,
            CEP = pessoa.CEP,
            CriadoEm = pessoa.CriadoEm,
            AtualizadoEm = pessoa.AtualizadoEm,
            AceiteTermo = aceite
        };
    }
}
