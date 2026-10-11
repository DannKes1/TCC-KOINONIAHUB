import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import type { MenuItem } from "primevue/menuitem";

import MenuAcoes from "./MenuAcoes.vue";

// Etapa 6.4: o ⋮ reúne as ações secundárias de um item; itens com `visible: false`
// não entram e, sem itens visíveis, o botão nem aparece.
function montar(itens: MenuItem[]) {
  return mount(MenuAcoes, {
    attachTo: document.body,
    props: { itens, rotulo: "Mais ações da aula" },
    global: { plugins: [PrimeVue] },
  });
}

describe("MenuAcoes", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
  });

  it("abre o menu com os itens visíveis e executa o comando do item", async () => {
    const executado: string[] = [];
    const w = montar([
      { label: "Ver presenças", icon: "pi pi-list-check", command: () => executado.push("presencas") },
      { label: "Consolidar", icon: "pi pi-lock", visible: false, command: () => executado.push("consolidar") },
      { label: "Reabrir", icon: "pi pi-lock-open", visible: () => true, command: () => executado.push("reabrir") },
    ]);

    const botao = w.find('[data-testid="menu-acoes"]');
    expect(botao.exists()).toBe(true);
    expect(botao.attributes("aria-label")).toBe("Mais ações da aula");

    await botao.trigger("click");
    await flushPromises();

    const rotulos = Array.from(document.querySelectorAll(".p-menu .p-menu-item-label")).map(
      (el) => el.textContent?.trim(),
    );
    expect(rotulos).toEqual(["Ver presenças", "Reabrir"]);

    (document.querySelector(".p-menu .p-menu-item-link") as HTMLElement).click();
    await flushPromises();
    expect(executado).toEqual(["presencas"]);

    w.unmount();
  });

  it("não renderiza o botão quando nenhum item está visível", () => {
    const w = montar([{ label: "Consolidar", visible: false }]);
    expect(w.find('[data-testid="menu-acoes"]').exists()).toBe(false);
    w.unmount();
  });
});
