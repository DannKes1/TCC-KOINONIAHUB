import { onBeforeUnmount, onMounted, ref } from "vue";
import type { Ref } from "vue";

// Etapa 6.4 — interruptor único entre a tabela (desktop) e os cartões (celular),
// no mesmo ponto de corte das media queries do CSS (768 px). Lê o valor já no setup
// para a primeira renderização sair certa (sem piscar tabela no celular) e acompanha
// a mudança de largura/orientação enquanto o componente viver.
export const CONSULTA_TELA_COMPACTA = "(max-width: 768px)";

function consultar(): MediaQueryList | null {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
    return null;
  }
  return window.matchMedia(CONSULTA_TELA_COMPACTA);
}

export function usarTelaCompacta(): Ref<boolean> {
  const lista = consultar();
  const compacta = ref(lista?.matches ?? false);

  const atualizar = (evento: { matches: boolean }) => {
    compacta.value = evento.matches;
  };

  onMounted(() => {
    lista?.addEventListener("change", atualizar);
  });

  onBeforeUnmount(() => {
    lista?.removeEventListener("change", atualizar);
  });

  return compacta;
}
