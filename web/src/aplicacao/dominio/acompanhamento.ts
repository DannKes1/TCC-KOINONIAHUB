// Corretiva G5 (RF36): texto da regra do painel de Acompanhamento com os valores que a API
// vai aplicar (RelatorioEbdServico.ObterPainelAcompanhamentoAsync): campo vazio usa o padrão
// (75 %, 50 % e 3 faltas), os limiares ficam entre 0 e 100, o crítico nunca passa o de
// atenção e as faltas seguidas do crítico valem no mínimo 1. Duas faltas seguidas são
// sempre atenção (número fixo do RF36; o campo "Faltas seguidas (crítico)" ajusta só o
// crítico) e o texto diz isso; com o crítico em 2 faltas ou menos, elas já entram como
// crítico e saem do texto da atenção.

export const FALTAS_SEGUIDAS_ATENCAO = 2;

function numero(valor: number): string {
  return valor.toLocaleString("pt-BR", { maximumFractionDigits: 2 });
}

function limitar(valor: number, minimo: number, maximo: number): number {
  return Math.min(Math.max(valor, minimo), maximo);
}

export function textoRegraAcompanhamento(
  limiarAtencao: number | null | undefined,
  limiarCritico: number | null | undefined,
  faltasCritico: number | null | undefined,
): string {
  const atencao = limitar(limiarAtencao ?? 75, 0, 100);
  const critico = Math.min(limitar(limiarCritico ?? 50, 0, 100), atencao);
  const faltas = Math.max(1, Math.trunc(faltasCritico ?? 3));

  const faltasAtencao =
    faltas > FALTAS_SEGUIDAS_ATENCAO
      ? ` ou ${FALTAS_SEGUIDAS_ATENCAO} faltas seguidas (número fixo)`
      : "";
  const faltasNoCritico = faltas === 1 ? "1 falta" : `${faltas} faltas seguidas`;

  return (
    `Atenção: frequência igual ou abaixo de ${numero(atencao)} %${faltasAtencao}. ` +
    `Crítico: frequência igual ou abaixo de ${numero(critico)} % ou ${faltasNoCritico}.`
  );
}
