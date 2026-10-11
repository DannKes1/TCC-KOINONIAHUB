import { defineComponent, h } from "vue";
import { mount } from "@vue/test-utils";

import { CONSULTA_TELA_COMPACTA, usarTelaCompacta } from "./usarTelaCompacta";

// Etapa 6.4: o interruptor tabela/cartões lê a media query no setup e acompanha a
// mudança de largura. O jsdom não implementa matchMedia; aqui ele é simulado.
type Ouvinte = (evento: { matches: boolean }) => void;

function simularMatchMedia(inicial: boolean) {
  const ouvintes: Ouvinte[] = [];
  const consultasFeitas: string[] = [];

  const lista = {
    matches: inicial,
    addEventListener: (_tipo: string, ouvinte: Ouvinte) => {
      ouvintes.push(ouvinte);
    },
    removeEventListener: (_tipo: string, ouvinte: Ouvinte) => {
      const i = ouvintes.indexOf(ouvinte);
      if (i >= 0) ouvintes.splice(i, 1);
    },
  };

  Object.defineProperty(window, "matchMedia", {
    configurable: true,
    writable: true,
    value: (consulta: string) => {
      consultasFeitas.push(consulta);
      return lista;
    },
  });

  return {
    consultasFeitas,
    ouvintes,
    mudarPara(matches: boolean) {
      lista.matches = matches;
      for (const o of [...ouvintes]) o({ matches });
    },
  };
}

const Componente = defineComponent({
  setup() {
    const compacta = usarTelaCompacta();
    return () => h("span", compacta.value ? "compacta" : "ampla");
  },
});

describe("usarTelaCompacta", () => {
  afterEach(() => {
    // @ts-expect-error limpeza do simulador entre os casos
    delete window.matchMedia;
  });

  it("consulta o ponto de corte de 768 px e começa com o valor atual", () => {
    const sim = simularMatchMedia(true);
    const w = mount(Componente);

    expect(sim.consultasFeitas).toEqual([CONSULTA_TELA_COMPACTA]);
    expect(CONSULTA_TELA_COMPACTA).toBe("(max-width: 768px)");
    expect(w.text()).toBe("compacta");

    w.unmount();
  });

  it("acompanha a mudança de largura e para de ouvir ao desmontar", async () => {
    const sim = simularMatchMedia(false);
    const w = mount(Componente);
    expect(w.text()).toBe("ampla");

    sim.mudarPara(true);
    await w.vm.$nextTick();
    expect(w.text()).toBe("compacta");

    w.unmount();
    expect(sim.ouvintes).toHaveLength(0);
  });

  it("sem matchMedia (ambiente sem janela), assume tela ampla", () => {
    // @ts-expect-error cenário sem suporte
    delete window.matchMedia;
    const w = mount(Componente);
    expect(w.text()).toBe("ampla");
    w.unmount();
  });
});
