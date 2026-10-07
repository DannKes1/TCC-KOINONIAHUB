import type { HistoricoPresencaPessoaVM } from "../modelos/dtos";

// Indicadores de frequência calculados no front a partir do histórico de presenças
// (RF34), usados pelo painel do usuário comum (RF3) e pelo diálogo de histórico da
// tela de Pessoas. Seguem a regra do CSU07: só registros de aulas Consolidadas
// contam; Em aberto e Não realizada aparecem no histórico, mas não nos totais.

// Janela padrão de "Minha Frequência" (RelatoriosController: últimos 90 dias). O
// painel do aluno usa a mesma janela para os indicadores baterem com a tela RF6.
export const JANELA_PADRAO_DIAS = 90;

export type ResumoPresencas = {
  aulasConsolidadas: number;
  presencas: number;
  faltas: number;
  // null quando não há aula consolidada (evita 0% enganoso).
  percentual: number | null;
};

export function inicioDaJanela(
  dias: number = JANELA_PADRAO_DIAS,
  hoje: Date = new Date(),
): Date {
  const inicio = new Date(hoje);
  inicio.setDate(inicio.getDate() - dias);
  return inicio;
}

// Registros com data da aula dentro da janela (inclusive). Datas inválidas ficam fora.
export function filtrarJanela(
  registros: HistoricoPresencaPessoaVM[],
  dias: number = JANELA_PADRAO_DIAS,
  hoje: Date = new Date(),
): HistoricoPresencaPessoaVM[] {
  const inicio = inicioDaJanela(dias, hoje).getTime();
  const fim = hoje.getTime();
  return registros.filter((r) => {
    const t = new Date(r.dataAula).getTime();
    return !Number.isNaN(t) && t >= inicio && t <= fim;
  });
}

export function resumirPresencas(
  registros: HistoricoPresencaPessoaVM[],
): ResumoPresencas {
  const consolidadas = registros.filter(
    (r) => r.situacaoAula === "Consolidada",
  );
  const presencas = consolidadas.filter((r) => r.presente).length;
  const aulasConsolidadas = consolidadas.length;
  return {
    aulasConsolidadas,
    presencas,
    faltas: aulasConsolidadas - presencas,
    percentual:
      aulasConsolidadas === 0
        ? null
        : Math.round((presencas / aulasConsolidadas) * 100),
  };
}

export function formatarPercentual(valor: number | null): string {
  return valor === null ? "-" : `${valor}%`;
}
