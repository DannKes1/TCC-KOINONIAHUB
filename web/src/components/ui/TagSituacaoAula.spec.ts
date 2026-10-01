import { mount } from "@vue/test-utils";

import TagSituacaoAula from "./TagSituacaoAula.vue";
import type { SituacaoAula } from "../../aplicacao/modelos/dtos";

// Plano 7.2, "Componente de tag de situação" (RF31 / RNF 31.3): uma tag por
// situação e a tag adicional "Pendente" apenas quando a aula está Em aberto
// com data já ocorrida.
function montar(props: {
  situacao: SituacaoAula;
  pendenteFechamento?: boolean;
}) {
  return mount(TagSituacaoAula, {
    props,
    global: { directives: { tooltip: {} } },
  });
}

describe("TagSituacaoAula (RF31 / RNF 31.3)", () => {
  it("Em aberto: rótulo da monografia e sem tag Pendente", () => {
    const wrapper = montar({ situacao: "EmAberto" });

    expect(wrapper.get('[data-testid="tag-situacao"]').text()).toBe(
      "Em aberto",
    );
    expect(wrapper.find('[data-testid="tag-pendente"]').exists()).toBe(false);
  });

  it("Consolidada: rótulo da monografia e sem tag Pendente", () => {
    const wrapper = montar({ situacao: "Consolidada" });

    expect(wrapper.get('[data-testid="tag-situacao"]').text()).toBe(
      "Consolidada",
    );
    expect(wrapper.find('[data-testid="tag-pendente"]').exists()).toBe(false);
  });

  it("Não realizada: rótulo da monografia e sem tag Pendente", () => {
    const wrapper = montar({ situacao: "NaoRealizada" });

    expect(wrapper.get('[data-testid="tag-situacao"]').text()).toBe(
      "Não realizada",
    );
    expect(wrapper.find('[data-testid="tag-pendente"]').exists()).toBe(false);
  });

  it("Em aberto pendente de fechamento exibe a tag Pendente", () => {
    const wrapper = montar({ situacao: "EmAberto", pendenteFechamento: true });

    const pendente = wrapper.get('[data-testid="tag-pendente"]');
    expect(pendente.text()).toContain("Pendente");
  });

  it("pendenteFechamento é ignorado fora de Em aberto", () => {
    for (const situacao of ["Consolidada", "NaoRealizada"] as const) {
      const wrapper = montar({ situacao, pendenteFechamento: true });

      expect(wrapper.find('[data-testid="tag-pendente"]').exists()).toBe(false);
    }
  });
});
