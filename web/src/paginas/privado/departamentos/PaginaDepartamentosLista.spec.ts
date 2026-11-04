import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaDepartamentosLista from "./PaginaDepartamentosLista.vue";
import { criarDepartamento } from "../../../aplicacao/servicos/departamentosServico";

// Corretiva G4: o erro da API ao salvar a turma aparece dentro do diálogo (no topo quando
// a API devolve só a mensagem), nunca no topo da página, e some ao alterar um campo; a
// validação local aparece junto ao campo, sem toast.
vi.mock("primevue/useconfirm", () => ({
  useConfirm: () => ({ require: vi.fn(), close: vi.fn() }),
}));

vi.mock("../../../aplicacao/armazenamentos/autenticacaoStore", () => ({
  usarAutenticacaoStore: () => ({ perfil: "Admin", isAdministrativo: true }),
}));

vi.mock("../../../aplicacao/servicos/departamentosServico", () => ({
  listarDepartamentos: vi.fn(async () => [
    { id: 7, nome: "Jovens", tipo: "EBD", ativo: true, descricao: null, imagemUrl: null },
  ]),
  criarDepartamento: vi.fn(),
  atualizarDepartamento: vi.fn(),
}));

const EM_USO = "Já existe uma turma com este nome.";
const rejeicao = (data: unknown) => ({ response: { status: 400, data } });

async function montar() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/departamentos", component: PaginaDepartamentosLista }],
  });
  await router.push("/departamentos");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    attachTo: document.body,
    global: { plugins: [router, PrimeVue], directives: { tooltip: Tooltip } },
  });
  await flushPromises();
  return wrapper;
}

function erroDoDialogo() {
  return document.querySelector('[data-testid="turma-dialogo-erro"]');
}

function clicar(testid: string) {
  document.querySelector<HTMLButtonElement>(`[data-testid="${testid}"]`)!.click();
}

function digitar(seletor: string, valor: string) {
  const campo = document.querySelector<HTMLInputElement>(seletor)!;
  campo.value = valor;
  campo.dispatchEvent(new Event("input"));
}

describe("PaginaDepartamentosLista — erro dentro do diálogo (Corretiva G4)", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
    vi.mocked(criarDepartamento).mockReset();
  });

  it("Nova Turma: 400 com mensagem fica no topo do diálogo, nada na página, some ao digitar", async () => {
    vi.mocked(criarDepartamento).mockRejectedValue(rejeicao({ mensagem: EM_USO }));
    const wrapper = await montar();

    await wrapper.find('button[aria-label="Nova Turma"]').trigger("click");
    await flushPromises();
    digitar("#departamento-nome", "Jovens");
    await flushPromises();

    clicar("turma-dialogo-salvar");
    await flushPromises();
    expect(criarDepartamento).toHaveBeenCalledTimes(1);
    expect(erroDoDialogo()!.textContent).toContain(EM_USO);
    expect(wrapper.text()).not.toContain(EM_USO);

    digitar("#departamento-nome", "Jovens 2");
    await flushPromises();
    expect(erroDoDialogo()).toBeNull();

    wrapper.unmount();
  });

  it("Nova Turma sem nome: erro junto ao campo e a API não é chamada", async () => {
    const wrapper = await montar();

    await wrapper.find('button[aria-label="Nova Turma"]').trigger("click");
    await flushPromises();
    clicar("turma-dialogo-salvar");
    await flushPromises();

    expect(document.querySelector('[data-testid="turma-nome-erro"]')!.textContent).toContain(
      "Informe o nome do departamento/turma.",
    );
    expect(criarDepartamento).not.toHaveBeenCalled();

    wrapper.unmount();
  });
});
