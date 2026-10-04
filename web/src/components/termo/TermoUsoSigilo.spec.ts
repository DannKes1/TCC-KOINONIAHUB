import { mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";

import TermoUsoSigilo from "./TermoUsoSigilo.vue";
import type { TermoVigenteVM } from "../../aplicacao/modelos/dtos";

// Plano 7.2 (RNFs 42.1, 42.5, 42.7): o botão só habilita depois de marcar
// "Li e aceito"; o aceite emite a versão exibida; a igreja e o canal de contato
// aparecem fora do texto fixo do termo.
const termo: TermoVigenteVM = {
  versao: "1.0",
  vigenteDesde: "2026-10-01T00:00:00Z",
  texto: "TERMO DE USO E SIGILO DO KOINONIAHUB\n\nTexto de teste.",
  hash: "abc",
  igreja: {
    nome: "Igreja de Teste",
    email: "contato@igreja.teste",
    telefone: "(69) 99999-0000",
  },
};

function montar(props: Record<string, unknown> = {}) {
  return mount(TermoUsoSigilo, {
    props: { termo, modelValue: false, ...props },
    global: { plugins: [PrimeVue] },
  });
}

describe("TermoUsoSigilo", () => {
  it("mostra versão, igreja e canal de contato fora do texto", () => {
    const wrapper = montar();

    expect(wrapper.get('[data-testid="termo-versao"]').text()).toContain(
      "Versão 1.0",
    );
    expect(wrapper.get('[data-testid="termo-igreja-nome"]').text()).toBe(
      "Igreja de Teste",
    );
    expect(wrapper.get('[data-testid="termo-igreja-contato"]').text()).toBe(
      "contato@igreja.teste · (69) 99999-0000",
    );
    expect(wrapper.get('[data-testid="termo-texto"]').text()).toContain(
      "Texto de teste.",
    );
  });

  it("sem igreja ou contato, deixa explícito que falta definir (não inventa)", () => {
    const wrapper = montar({ termo: { ...termo, igreja: null } });

    expect(wrapper.get('[data-testid="termo-igreja-nome"]').text()).toBe(
      "[igreja em cadastro]",
    );
    expect(wrapper.get('[data-testid="termo-igreja-contato"]').text()).toBe(
      "[contato a ser definido pela igreja]",
    );
  });

  it("a prop `igreja` substitui a igreja vinda da API (cadastro inicial)", () => {
    const wrapper = montar({
      igreja: { nome: "Igreja Digitada", email: null, telefone: null },
    });

    expect(wrapper.get('[data-testid="termo-igreja-nome"]').text()).toBe(
      "Igreja Digitada",
    );
    expect(wrapper.get('[data-testid="termo-igreja-contato"]').text()).toBe(
      "[contato a ser definido pela igreja]",
    );
  });

  it("botão desabilitado até marcar 'Li e aceito'; ao aceitar emite a versão", async () => {
    const wrapper = montar();
    const botao = () => wrapper.get('[data-testid="botao-aceitar-termo"]');

    expect(botao().attributes("disabled")).toBeDefined();

    await wrapper.get('input[type="checkbox"]').setValue(true);
    const emitidos = wrapper.emitted("update:modelValue") ?? [];
    expect(emitidos[emitidos.length - 1]).toEqual([true]);

    await wrapper.setProps({ modelValue: true });
    expect(botao().attributes("disabled")).toBeUndefined();

    await botao().trigger("click");
    expect(wrapper.emitted("aceitar")).toEqual([["1.0"]]);
  });

  it("não emite aceite com a caixa desmarcada", async () => {
    const wrapper = montar();

    await wrapper.get('[data-testid="botao-aceitar-termo"]').trigger("click");

    expect(wrapper.emitted("aceitar")).toBeUndefined();
  });

  it("com mostrarBotao=false, exibe a caixa de aceite mas não o botão (embutido)", () => {
    const wrapper = montar({ mostrarBotao: false });

    expect(wrapper.find('input[type="checkbox"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="botao-aceitar-termo"]').exists()).toBe(
      false,
    );
  });

  it("mostra o erro de carregamento no lugar do termo", () => {
    const wrapper = montar({ termo: null, erro: "Falhou" });

    expect(wrapper.get('[data-testid="termo-erro"]').text()).toBe("Falhou");
    expect(wrapper.find('[data-testid="termo-texto"]').exists()).toBe(false);
  });
});
