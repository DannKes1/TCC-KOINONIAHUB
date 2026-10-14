import { ref } from "vue";

// Etapa 6.4-B corretiva — ação que ignora um segundo acionamento enquanto a
// primeira execução não termina. No celular, um toque podia disparar dois cliques
// antes de o botão ser desabilitado pela renderização seguinte ("Chamada salva"
// aparecia duas vezes). O bloqueio é síncrono: vale já no segundo clique, sem
// esperar o Vue redesenhar o botão.
export function usarAcaoUnica() {
  const emAndamento = ref(false);

  async function executar<T>(acao: () => Promise<T>): Promise<T | undefined> {
    if (emAndamento.value) return undefined;
    emAndamento.value = true;
    try {
      return await acao();
    } finally {
      emAndamento.value = false;
    }
  }

  return { emAndamento, executar };
}
