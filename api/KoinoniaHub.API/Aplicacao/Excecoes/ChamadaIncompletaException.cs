using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Excecoes
{
    // RNFs 32.6 / 33.3: a consolidação foi recusada porque há aluno com matrícula
    // ativa sem registro explícito de presença ou ausência. Deriva de
    // InvalidOperationException para continuar sendo um 400 em qualquer catch
    // genérico; o AulasController a trata antes para devolver também a lista
    // (Plano de Desenvolvimento, 6.2).
    public class ChamadaIncompletaException : InvalidOperationException
    {
        public IReadOnlyList<AlunoSemRegistroRespostaDto> AlunosSemRegistro { get; }

        public ChamadaIncompletaException(IReadOnlyList<AlunoSemRegistroRespostaDto> alunosSemRegistro)
            : base(MontarMensagem(alunosSemRegistro.Count))
        {
            AlunosSemRegistro = alunosSemRegistro;
        }

        private static string MontarMensagem(int quantidade) =>
            quantidade == 1
                ? "A chamada deve ser concluída antes da consolidação: 1 aluno com matrícula ativa está sem registro de presença ou ausência."
                : $"A chamada deve ser concluída antes da consolidação: {quantidade} alunos com matrícula ativa estão sem registro de presença ou ausência.";
    }
}
