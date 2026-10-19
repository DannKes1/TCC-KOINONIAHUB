namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class AtribuicaoRespostaDto
    {
        public int Id { get; set; }

        public int PessoaId { get; set; }
        public string PessoaNome { get; set; } = string.Empty;

        public int DepartamentoId { get; set; }
        public string DepartamentoNome { get; set; } = string.Empty;

        public string Funcao { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }

        public bool Ativo { get; set; }

        // RNF 21.5 (7.1): perfil da conta de acesso da pessoa; null = sem conta. A tela
        // de Atribuições sinaliza "Sem conta" e "Conta com perfil Usuário", que não
        // acessam a turma (RNF 21.4). Pela API porque Pastor e Superintendente gerem
        // atribuições mas não listam usuários (RF16 é Admin). Conta inativa também
        // devolve o perfil: ter conta e estar ativa são assuntos distintos.
        public string? PerfilConta { get; set; }
    }
}