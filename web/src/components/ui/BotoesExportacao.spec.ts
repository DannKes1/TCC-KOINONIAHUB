import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import PrimeVue from "primevue/config";

import BotoesExportacao from "./BotoesExportacao.vue";
import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";

// Plano 7.2, "Visibilidade dos botões de exportação" (RNFs 35.4/36.4/37.4):
// presentes para Admin, Pastor e Superintendente; ausentes para Professor e Usuario.
function montarComPerfil(perfil: string, props: Record<string, unknown> = {}) {
  const pinia = createPinia();
  setActivePinia(pinia);

  usarAutenticacaoStore().entrar({
    ExpiraEm: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    UsuarioId: 1,
    EmailUsuario: `${perfil.toLowerCase()}@teste.com`,
    Perfil: perfil,
    IgrejaId: 1,
  });

  return mount(BotoesExportacao, {
    props: { csv: true, imprimir: true, ...props },
    global: { plugins: [pinia, PrimeVue] },
  });
}

describe("BotoesExportacao por perfil", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it.each(["Admin", "Pastor", "Superintendente"])(
    "%s vê os botões de CSV e Imprimir",
    (perfil) => {
      const wrapper = montarComPerfil(perfil);

      expect(wrapper.find('[data-testid="botao-csv"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="botao-imprimir"]').exists()).toBe(true);
    },
  );

  it.each(["Professor", "Usuario"])(
    "%s não vê nenhum botão de exportação",
    (perfil) => {
      const wrapper = montarComPerfil(perfil);

      expect(wrapper.find('[data-testid="botoes-exportacao"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="botao-csv"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="botao-imprimir"]').exists()).toBe(false);
    },
  );

  it("emite os eventos ao clicar", async () => {
    const wrapper = montarComPerfil("Admin");

    await wrapper.get('[data-testid="botao-csv"]').trigger("click");
    await wrapper.get('[data-testid="botao-imprimir"]').trigger("click");

    expect(wrapper.emitted("csv")).toHaveLength(1);
    expect(wrapper.emitted("imprimir")).toHaveLength(1);
  });

  it("mostra só o botão pedido pelas props", () => {
    const wrapper = montarComPerfil("Admin", { imprimir: false });

    expect(wrapper.find('[data-testid="botao-csv"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="botao-imprimir"]').exists()).toBe(false);
  });
});
