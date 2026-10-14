import { getCurrentScope, onScopeDispose, ref } from "vue";

// Etapa 6.4-B corretiva — ação que ignora um segundo acionamento enquanto a
// primeira execução não termina. O bloqueio é síncrono: vale já no segundo clique,
// sem esperar o Vue redesenhar o botão.
//
// Ajuste da corretiva (toque duplo no celular): com a API local a chamada era salva
// em menos de 80 ms — mais rápido que o segundo toque de um toque duplo (medido:
// dois POSTs com 77 ms de intervalo). O segundo toque chegava com a primeira
// execução já concluída e virava uma segunda gravação legítima, com o segundo
// "Chamada salva". Por isso `emAndamento` fica ligado por mais `esperaMs` depois de
// a ação terminar (500 ms, acima do intervalo de um toque duplo): o botão continua
// desabilitado e o acionamento nesse intervalo é ignorado.
export const ESPERA_PADRAO_MS = 500;

export function usarAcaoUnica(esperaMs = ESPERA_PADRAO_MS) {
  const emAndamento = ref(false);
  let temporizador: ReturnType<typeof setTimeout> | undefined;

  function liberar() {
    temporizador = undefined;
    emAndamento.value = false;
  }

  async function executar<T>(acao: () => Promise<T>): Promise<T | undefined> {
    if (emAndamento.value) return undefined;
    emAndamento.value = true;
    try {
      return await acao();
    } finally {
      if (esperaMs > 0) temporizador = setTimeout(liberar, esperaMs);
      else liberar();
    }
  }

  if (getCurrentScope()) {
    onScopeDispose(() => {
      if (temporizador) clearTimeout(temporizador);
    });
  }

  return { emAndamento, executar };
}
