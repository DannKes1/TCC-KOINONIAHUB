import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";
import { ref } from "vue";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaMateriasTurma from "./PaginaMateriasTurma.vue";
import type { OpcoesConfirmacao } from "../../../aplicacao/servicos/notificacoes";
import { atualizarMateria, criarMateria } from "../../../aplicacao/servicos/materiasServico";

// Etapa 6.4-C (RF20 / RF24–RF26): no celular a aba Matérias vira cartões — um por
// matéria, "Editar" como ação principal e Ativar/Inativar no ⋮ — e, dentro da
// Página da Turma, criar ou ativar/inativar avisa o cabeçalho para recontar.
vi.mock("../../../aplicacao/composables/usarTelaCompacta", () => ({
  usarTelaCompacta: () => ref(true),
}));

const requireConfirmacao = vi.fn<(opcoes: OpcoesConfirmacao) => void>();
vi.mock("primevue/useconfirm", () => ({
  useConfirm: () => ({ require: requireConfirmacao, close: vi.fn() }),
}));

vi.mock("../../../aplicacao/servicos/departamentosServico", () => ({
  obterDepartamento: vi.fn(async (id: number) => ({ id, nome: "Jovens", tipo: "EBD", ativo: true })),
}));

vi.mock("../../../aplicacao/servicos/materiasServico", () => ({
  listarMaterias: vi.fn(async () => [
    { id: 2, nome: "Antiga", ativo: false, ordemExibicao: 2, departamentoId: 7, nomeDepartamento: "Jovens", criadoEm: "", atualizadoEm: null },
    { id: 1, nome: "Samuel", ativo: true, ordemExibicao: 1, departamentoId: 7, nomeDepartamento: "Jovens", criadoEm: "", atualizadoEm: null },
  ]),
  criarMateria: vi.fn(async () => undefined),
  atualizarMateria: vi.fn(async () => undefined),
}));

async function montar() {
  const recarregarResumo = vi.fn(async () => undefined);
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/departamentos/:departamentoId/materias", component: PaginaMateriasTurma }],
  });
  await router.push("/departamentos/7/materias");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    attachTo: document.body,
    global: {
      plugins: [router, PrimeVue],
      directives: { tooltip: Tooltip },
      provide: { turmaRecarregarResumo: recarregarResumo },
    },
  });
  await flushPromises();
  return { wrapper, recarregarResumo };
}

describe("PaginaMateriasTurma — cartões no celular (6.4-C)", () => {
  beforeEach(() => {
    requireConfirmacao.mockClear();
    vi.mocked(criarMateria).mockClear();
    vi.mocked(atualizarMateria).mockClear();
    document.body.innerHTML = "";
  });

  it("um cartão por matéria, na ordem de exibição, com Editar e o ⋮", async () => {
    const { wrapper } = await montar();

    const cartoes = wrapper.findAll('[data-testid="materia-cartao"]');
    expect(cartoes.map((c) => c.find(".cartao-item-titulo").text())).toEqual(["Samuel", "Antiga"]);
    expect(cartoes[0]!.text()).toContain("Ativa");
    expect(cartoes[0]!.text()).toContain("Ordem de exibição: 1");
    expect(cartoes[1]!.text()).toContain("Inativa");
    expect(cartoes[0]!.find('[data-testid="materia-editar"]').exists()).toBe(true);
    expect(cartoes[0]!.find('[data-testid="menu-acoes"]').exists()).toBe(true);
    expect(wrapper.find(".p-datatable").exists()).toBe(false);
    wrapper.unmount();
  });

  it("criar uma matéria pelo diálogo manda o cabeçalho da turma recontar", async () => {
    const { wrapper, recarregarResumo } = await montar();

    const botaoNova = wrapper.findAll("button").find((b) => b.text().includes("Nova Matéria"));
    await botaoNova!.trigger("click");
    await flushPromises();

    const campoNome = document.querySelector<HTMLInputElement>(".p-dialog input.p-inputtext");
    expect(campoNome).not.toBeNull();
    campoNome!.value = "Reis";
    campoNome!.dispatchEvent(new Event("input"));
    await flushPromises();

    const criar = Array.from(document.querySelectorAll<HTMLButtonElement>(".p-dialog button"))
      .find((b) => b.textContent?.includes("Criar"));
    criar!.click();
    await flushPromises();

    expect(criarMateria).toHaveBeenCalledTimes(1);
    expect(vi.mocked(criarMateria).mock.calls[0]![0]).toMatchObject({ Nome: "Reis", DepartamentoId: 7 });
    expect(recarregarResumo).toHaveBeenCalledTimes(1);
    wrapper.unmount();
  });

  it("inativar pelo ⋮ confirma, atualiza e manda o cabeçalho recontar", async () => {
    const { wrapper, recarregarResumo } = await montar();

    await wrapper.findAll('[data-testid="materia-cartao"]')[0]!.get('[data-testid="menu-acoes"]').trigger("click");
    await flushPromises();
    const inativar = Array.from(document.querySelectorAll<HTMLElement>(".p-menu-item-link, .p-menuitem-link"))
      .find((el) => el.textContent?.includes("Inativar"));
    expect(inativar).toBeDefined();
    inativar!.click();
    await flushPromises();

    const opcoes = requireConfirmacao.mock.calls[requireConfirmacao.mock.calls.length - 1]![0];
    expect(opcoes.header).toBe("Inativar matéria");
    await opcoes.accept!();
    await flushPromises();

    expect(atualizarMateria).toHaveBeenCalledWith(1, expect.objectContaining({ Ativo: false }));
    expect(recarregarResumo).toHaveBeenCalledTimes(1);
    wrapper.unmount();
  });

  // Corretiva G4: o erro de regra da API aparece dentro do diálogo, nunca no topo da
  // página, e some ao alterar um campo; nome vazio fica junto ao campo, sem toast.
  it("criar com 400 da API: aviso no topo do diálogo, nada na página, some ao digitar", async () => {
    const EM_USO = "Já existe uma matéria com este nome nesta turma.";
    vi.mocked(criarMateria).mockRejectedValueOnce({ response: { status: 400, data: { mensagem: EM_USO } } });
    const { wrapper, recarregarResumo } = await montar();

    await wrapper.findAll("button").find((b) => b.text().includes("Nova Matéria"))!.trigger("click");
    await flushPromises();
    const campoNome = document.querySelector<HTMLInputElement>("#materia-nome")!;
    campoNome.value = "Reis";
    campoNome.dispatchEvent(new Event("input"));
    await flushPromises();

    document.querySelector<HTMLButtonElement>('[data-testid="materia-dialogo-salvar"]')!.click();
    await flushPromises();

    expect(criarMateria).toHaveBeenCalledTimes(1);
    expect(document.querySelector('[data-testid="materia-dialogo-erro"]')!.textContent).toContain(EM_USO);
    expect(wrapper.text()).not.toContain(EM_USO);
    expect(recarregarResumo).not.toHaveBeenCalled();

    campoNome.value = "Reis 2";
    campoNome.dispatchEvent(new Event("input"));
    await flushPromises();
    expect(document.querySelector('[data-testid="materia-dialogo-erro"]')).toBeNull();

    wrapper.unmount();
  });

  it("criar sem nome: erro junto ao campo e a API não é chamada", async () => {
    const { wrapper } = await montar();

    await wrapper.findAll("button").find((b) => b.text().includes("Nova Matéria"))!.trigger("click");
    await flushPromises();
    document.querySelector<HTMLButtonElement>('[data-testid="materia-dialogo-salvar"]')!.click();
    await flushPromises();

    expect(document.querySelector('[data-testid="materia-nome-erro"]')!.textContent).toContain("Informe o nome da matéria.");
    expect(criarMateria).not.toHaveBeenCalled();

    wrapper.unmount();
  });
});
