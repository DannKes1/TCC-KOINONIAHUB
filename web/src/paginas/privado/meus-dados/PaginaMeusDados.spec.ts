import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import PrimeVue from "primevue/config";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaMeusDados from "./PaginaMeusDados.vue";
import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";
import {
  atualizarMeusDados,
  obterMeusDados,
} from "../../../aplicacao/servicos/meusDadosServico";
import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";

// RF4 / RNF 4.3 (aderência, Entrega A): a validação prévia de Meus Dados barra
// e-mail inválido, UF fora de 2 letras e celular fora de 8 a 11 dígitos com a mensagem
// no campo, sem chamar a API; o envio normaliza vazio para nulo e a UF para maiúsculas.
vi.mock("../../../aplicacao/servicos/meusDadosServico", () => ({
  obterMeusDados: vi.fn(async () => dadosDaApi()),
  atualizarMeusDados: vi.fn(async () => undefined),
}));

vi.mock("../../../aplicacao/servicos/notificacoes", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  toastSuccess: vi.fn(),
}));

function dadosDaApi() {
  return {
    id: 5,
    nome: "Maria Souza",
    dataNascimento: null,
    sexo: "Feminino",
    estadoCivil: null,
    situacao: "Ativo",
    dataInativacao: null,
    celular: "69999990001",
    email: "maria@teste.com",
    endereco: "Rua A, 1",
    bairro: "Centro",
    cidade: "Ji-Paraná",
    estado: "RO",
    cep: "76900-000",
    criadoEm: "2026-01-01T00:00:00Z",
    atualizadoEm: null,
    aceiteTermo: { versao: "1.0", aceitoEm: "2026-10-01T12:00:00Z", meio: "web", vigente: true },
  };
}

async function montar() {
  const pinia = createPinia();
  setActivePinia(pinia);
  usarAutenticacaoStore().entrar({
    ExpiraEm: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    UsuarioId: 1,
    EmailUsuario: "maria@teste.com",
    Perfil: "Professor",
    IgrejaId: 1,
  });

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/meus-dados", component: PaginaMeusDados }],
  });
  await router.push("/meus-dados");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    global: { plugins: [pinia, router, PrimeVue] },
  });
  await flushPromises();
  return wrapper;
}

async function preencher(wrapper: Awaited<ReturnType<typeof montar>>, id: string, valor: string) {
  await wrapper.find(`input#${id}`).setValue(valor);
}

async function salvar(wrapper: Awaited<ReturnType<typeof montar>>) {
  await wrapper.find('button[aria-label="Salvar"]').trigger("click");
  await flushPromises();
}

describe("PaginaMeusDados — validação prévia e envio (RNF 4.3)", () => {
  beforeEach(() => {
    vi.mocked(atualizarMeusDados).mockClear();
    vi.mocked(obterMeusDados).mockClear();
    vi.mocked(toastSuccess).mockClear();
  });

  it("carrega os dados no formulário", async () => {
    const wrapper = await montar();

    expect((wrapper.find("input#meus-dados-email").element as HTMLInputElement).value).toBe("maria@teste.com");
    expect((wrapper.find("input#meus-dados-uf").element as HTMLInputElement).value).toBe("RO");
    expect(wrapper.text()).toContain("Maria Souza");

    wrapper.unmount();
  });

  it("e-mail inválido e UF com três letras são barrados no campo, sem chamar a API", async () => {
    const wrapper = await montar();

    await preencher(wrapper, "meus-dados-email", "nao-e-email");
    await preencher(wrapper, "meus-dados-uf", "ROO");
    await salvar(wrapper);

    expect(atualizarMeusDados).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain("Informe um e-mail válido.");
    expect(wrapper.text()).toContain("Informe a UF com 2 letras (ex.: RO).");
    expect(wrapper.text()).toContain("Corrija os campos destacados.");

    wrapper.unmount();
  });

  it("celular com menos de 8 dígitos é barrado; letras são removidas ao digitar", async () => {
    const wrapper = await montar();

    await preencher(wrapper, "meus-dados-celular", "69abc99");
    await flushPromises();
    expect((wrapper.find("input#meus-dados-celular").element as HTMLInputElement).value).toBe("6999");

    await salvar(wrapper);

    expect(atualizarMeusDados).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain("Celular inválido. Informe DDD + número (8 a 11 dígitos).");

    wrapper.unmount();
  });

  it("dados válidos: e-mail vazio vai como nulo, UF em maiúsculas, e-mail com um @ aceito como na API", async () => {
    const wrapper = await montar();

    await preencher(wrapper, "meus-dados-email", "   ");
    await preencher(wrapper, "meus-dados-uf", "ro");
    await preencher(wrapper, "meus-dados-cidade", "Ouro Preto do Oeste");
    await salvar(wrapper);

    expect(atualizarMeusDados).toHaveBeenCalledTimes(1);
    expect(atualizarMeusDados).toHaveBeenCalledWith(
      expect.objectContaining({ Email: null, Estado: "RO", Cidade: "Ouro Preto do Oeste", Celular: "69999990001" }),
    );
    expect(toastSuccess).toHaveBeenCalledTimes(1);
    // Depois de salvar, os dados são recarregados.
    expect(obterMeusDados).toHaveBeenCalledTimes(2);
    expect(wrapper.text()).not.toContain("Corrija os campos destacados.");

    await preencher(wrapper, "meus-dados-email", "a@b");
    await salvar(wrapper);

    expect(atualizarMeusDados).toHaveBeenCalledTimes(2);
    expect(vi.mocked(atualizarMeusDados).mock.calls[1]![0]).toEqual(expect.objectContaining({ Email: "a@b" }));

    wrapper.unmount();
  });

  it("Descartar volta aos dados originais e limpa as mensagens de validação", async () => {
    const wrapper = await montar();

    await preencher(wrapper, "meus-dados-email", "nao-e-email");
    await salvar(wrapper);
    expect(wrapper.text()).toContain("Informe um e-mail válido.");

    await wrapper.find('button[aria-label="Descartar"]').trigger("click");
    await flushPromises();

    expect((wrapper.find("input#meus-dados-email").element as HTMLInputElement).value).toBe("maria@teste.com");
    expect(wrapper.text()).not.toContain("Informe um e-mail válido.");
    expect(atualizarMeusDados).not.toHaveBeenCalled();

    wrapper.unmount();
  });
});
