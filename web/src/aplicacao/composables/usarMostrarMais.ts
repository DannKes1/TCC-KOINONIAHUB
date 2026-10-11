import { computed, ref, watch } from "vue";
import type { Ref } from "vue";

// Etapa 6.4 — nas listas em cartões (celular) não há paginador: mostram-se os
// primeiros `passo` itens e um botão "Mostrar mais". Quando a lista de origem muda
// (busca, filtro, recarga), volta ao primeiro bloco.
export function usarMostrarMais<T>(lista: Ref<T[]>, passo = 10) {
  const limite = ref(passo);

  const visiveis = computed(() => lista.value.slice(0, limite.value));
  const restantes = computed(() =>
    Math.max(0, lista.value.length - limite.value),
  );

  function mostrarMais() {
    limite.value += passo;
  }

  function reiniciar() {
    limite.value = passo;
  }

  watch(lista, reiniciar);

  return { visiveis, restantes, mostrarMais, reiniciar };
}
