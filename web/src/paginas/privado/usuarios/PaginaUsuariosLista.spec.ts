import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import PrimeVue from "primevue/config";
import Dropdown from "primevue/dropdown";
import Tooltip from "primevue/tooltip";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaUsuariosLista from "./PaginaUsuariosLista.vue";
import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";
import {
  atualizarUsuario,
  criarUsuario,
} from "../../../aplicacao/servicos/usuariosServico";

// RF43 / RNF 43.1 (aderência, Entrega A): a coluna Termo da lista de Usuários mostra a
// situação do aceite na tag e, abaixo, a data e hora do aceite; conta sem aceite fica
// "Pendente" sem data.
const ACEITO_EM = "2026-10-01T12:34:00Z";
const ANTERIOR_EM = "2026-09-10T08:05:00Z";

function usuario(id: number, email: string, aceiteTermo: unknown) {
  return {
    id,
    igrejaId: 1,
    email,
    perfil: "Professor",
    ativo: true,
    pessoaId: id,
    nomePessoa: `Pessoa ${id}`,
    convitePendente: false,
    aceiteTermo,
  };
}

vi.mock("../../../aplicacao/servicos/usuariosServico", () => ({
  listarUsuarios: vi.fn(async () => [
    usuario(1, "aceito@teste.com", { versao: "1.0", aceitoEm: ACEITO_EM, meio: "web", vigente: true }),
    usuario(2, "pendente@teste.com", null),
    usuario(3, "anterior@teste.com", { versao: "0.9", aceitoEm: ANTERIOR_EM, meio: "web", vigente: false }),
  ]),
  obterUsuario: vi.fn(),
  criarUsuario: vi.fn(),
  atualizarUsuario: vi.fn(),
  gerarConviteUsuario: vi.fn(),
}));

// Uma pessoa sem conta (id 10), com e-mail no cadastro, para o diálogo Novo usuário.
vi.mock("../../../aplicacao/servicos/pessoasServico", () => ({
  listarPessoas: vi.fn(async () => [
    { id: 10, nome: "Pessoa Dez", email: "dez@teste.com", situacao: "Ativo", possuiConta: false },
  ]),
}));

function quando(iso: string) {
  return new Date(iso).toLocaleString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

async function montar() {
  const pinia = createPinia();
  setActivePinia(pinia);
  usarAutenticacaoStore().entrar({
    ExpiraEm: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    UsuarioId: 1,
    EmailUsuario: "admin@teste.com",
    Perfil: "Admin",
    IgrejaId: 1,
  });

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/usuarios", component: PaginaUsuariosLista }],
  });
  await router.push("/usuarios");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    attachTo: document.body,
    global: { plugins: [pinia, router, PrimeVue], directives: { tooltip: Tooltip } },
  });
  await flushPromises();
  return wrapper;
}

describe("PaginaUsuariosLista — coluna Termo (RF43, RNF 43.1)", () => {
  // O Dropdown (Select) do PrimeVue consulta matchMedia; o jsdom não o implementa.
  beforeAll(() => {
    Object.defineProperty(window, "matchMedia", {
      configurable: true,
      writable: true,
      value: () => ({
        matches: false,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
      }),
    });
  });

  beforeEach(() => {
    document.body.innerHTML = "";
  });

  it("mostra a versão na tag e a data e hora do aceite; pendente sem data", async () => {
    const wrapper = await montar();

    const linhas = wrapper.findAll("tbody tr");
    const porEmail = Object.fromEntries(linhas.map((l) => [l.text().includes("aceito@") ? "aceito" : l.text().includes("pendente@") ? "pendente" : "anterior", l]));
    expect(Object.keys(porEmail).sort()).toEqual(["aceito", "anterior", "pendente"]);

    // Colunas: Pessoa · E-mail · Perfil · Status · Termo · Ações.
    const celulaTermo = (linha: (typeof linhas)[number]) => linha.findAll("td")[4]!;

    const aceito = celulaTermo(porEmail["aceito"]!);
    expect(aceito.find(".p-tag").text()).toBe("Aceito v1.0");
    expect(aceito.find(".p-tag").classes()).toContain("p-tag-success");
    expect(aceito.find('[data-testid="usuario-termo-data"]').text()).toBe(`em ${quando(ACEITO_EM)}`);

    const pendente = celulaTermo(porEmail["pendente"]!);
    expect(pendente.find(".p-tag").text()).toBe("Pendente");
    expect(pendente.find(".p-tag").classes()).toContain("p-tag-warn");
    expect(pendente.find('[data-testid="usuario-termo-data"]').exists()).toBe(false);

    const anterior = celulaTermo(porEmail["anterior"]!);
    expect(anterior.find(".p-tag").text()).toBe("Versão anterior (v0.9)");
    expect(anterior.find(".p-tag").classes()).toContain("p-tag-warn");
    expect(anterior.find('[data-testid="usuario-termo-data"]').text()).toBe(`em ${quando(ANTERIOR_EM)}`);

    wrapper.unmount();
  });
});

// Corretiva G2 (RNF 13.5, RF14): um e-mail por pessoa. Novo usuário vem com o e-mail da
// pessoa escolhida; Editar usuário permite trocar o e-mail de acesso, que só vai no PATCH
// quando muda. Os diálogos são teleportados para o body.
describe("PaginaUsuariosLista — e-mail da conta (Corretiva G2)", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
    vi.mocked(atualizarUsuario).mockClear();
  });

  it("Novo usuário: ao escolher a pessoa, o e-mail dela vem preenchido", async () => {
    const wrapper = await montar();

    await wrapper.find('button[aria-label="Novo Usuário"]').trigger("click");
    await flushPromises();

    const pessoa = wrapper.findAllComponents(Dropdown).find((d) => d.props("labelId") === "usuario-criacao-pessoa");
    expect(pessoa).toBeDefined();
    pessoa!.vm.$emit("update:modelValue", 10);
    await flushPromises();

    expect(document.querySelector<HTMLInputElement>("#usuario-criacao-email")!.value).toBe("dez@teste.com");
    expect(document.querySelector('[data-testid="usuario-criacao-email-dica"]')!.textContent).toContain(
      "O e-mail fica também no cadastro da pessoa.",
    );

    wrapper.unmount();
  });

  it("Editar usuário: o e-mail é editável e só vai no PATCH quando muda", async () => {
    const wrapper = await montar();

    // Primeira linha (ordem por e-mail): aceito@teste.com, id 1.
    await wrapper.findAll('[data-testid="usuario-editar"]')[0]!.trigger("click");
    await flushPromises();

    const campo = document.querySelector<HTMLInputElement>("#usuario-edicao-email")!;
    expect(campo.disabled).toBe(false);
    expect(campo.value).toBe("aceito@teste.com");

    document.querySelector<HTMLButtonElement>('[data-testid="usuario-salvar"]')!.click();
    await flushPromises();
    expect(atualizarUsuario).toHaveBeenCalledWith(1, { Perfil: "Professor", Ativo: true });

    await wrapper.findAll('[data-testid="usuario-editar"]')[0]!.trigger("click");
    await flushPromises();
    const campoDeNovo = document.querySelector<HTMLInputElement>("#usuario-edicao-email")!;
    campoDeNovo.value = "Novo.Email@teste.com";
    campoDeNovo.dispatchEvent(new Event("input"));
    await flushPromises();

    document.querySelector<HTMLButtonElement>('[data-testid="usuario-salvar"]')!.click();
    await flushPromises();
    expect(atualizarUsuario).toHaveBeenLastCalledWith(1, { Perfil: "Professor", Ativo: true, Email: "novo.email@teste.com" });

    wrapper.unmount();
  });
});

// Corretiva G3: o erro da API ao salvar aparece dentro do diálogo — no topo quando a API
// devolve só a mensagem (400 `{ mensagem }`), junto ao campo quando devolve o campo
// (`errors` do ValidationProblemDetails) — nunca no topo da página, e some ao alterar um
// campo ou ao fechar o diálogo.
describe("PaginaUsuariosLista — erro da API dentro do diálogo (Corretiva G3)", () => {
  const EM_USO = "Este e-mail já está em uso por outra pessoa.";
  const rejeicao = (data: unknown) => ({ response: { status: 400, data } });

  beforeEach(() => {
    document.body.innerHTML = "";
    vi.mocked(criarUsuario).mockReset();
    vi.mocked(atualizarUsuario).mockReset();
  });

  function erroCriacao() {
    return document.querySelector('[data-testid="usuario-criacao-erro"]');
  }

  function clicar(testid: string) {
    document.querySelector<HTMLButtonElement>(`[data-testid="${testid}"]`)!.click();
  }

  async function abrirNovoComPessoa(wrapper: Awaited<ReturnType<typeof montar>>) {
    await wrapper.find('button[aria-label="Novo Usuário"]').trigger("click");
    await flushPromises();
    const pessoa = wrapper.findAllComponents(Dropdown).find((d) => d.props("labelId") === "usuario-criacao-pessoa");
    pessoa!.vm.$emit("update:modelValue", 10);
    await flushPromises();
  }

  it("Novo usuário: 400 com mensagem fica no topo do diálogo, some ao digitar e ao fechar", async () => {
    vi.mocked(criarUsuario).mockRejectedValue(rejeicao({ mensagem: EM_USO }));
    const wrapper = await montar();
    await abrirNovoComPessoa(wrapper);

    clicar("usuario-criacao-salvar");
    await flushPromises();
    expect(criarUsuario).toHaveBeenCalledTimes(1);
    expect(erroCriacao()!.textContent).toContain(EM_USO);
    // Nada no topo da página (o diálogo é teleportado para o body, fora do wrapper).
    expect(wrapper.text()).not.toContain(EM_USO);

    const campo = document.querySelector<HTMLInputElement>("#usuario-criacao-email")!;
    campo.value = "outro@teste.com";
    campo.dispatchEvent(new Event("input"));
    await flushPromises();
    expect(erroCriacao()).toBeNull();

    clicar("usuario-criacao-salvar");
    await flushPromises();
    expect(erroCriacao()).not.toBeNull();

    clicar("usuario-criacao-cancelar");
    await flushPromises();
    await abrirNovoComPessoa(wrapper);
    expect(erroCriacao()).toBeNull();

    wrapper.unmount();
  });

  it("Editar usuário: 400 por campo aparece junto ao e-mail, sem aviso no topo do diálogo", async () => {
    vi.mocked(atualizarUsuario).mockRejectedValue(
      rejeicao({ errors: { Email: ["O campo Email não é um endereço de e-mail válido."] } }),
    );
    const wrapper = await montar();

    await wrapper.findAll('[data-testid="usuario-editar"]')[0]!.trigger("click");
    await flushPromises();
    clicar("usuario-salvar");
    await flushPromises();

    expect(atualizarUsuario).toHaveBeenCalledTimes(1);
    expect(document.querySelector('[data-testid="usuario-edicao-email-erro"]')!.textContent).toContain(
      "não é um endereço de e-mail válido",
    );
    expect(document.querySelector('[data-testid="usuario-edicao-erro"]')).toBeNull();
    expect(wrapper.text()).not.toContain("Alguns campos enviados são inválidos");

    wrapper.unmount();
  });

  // Corretiva G4: a validação local fica junto ao campo, sem toast.
  it("Novo usuário sem pessoa: erro junto ao campo e a API não é chamada", async () => {
    const wrapper = await montar();

    await wrapper.find('button[aria-label="Novo Usuário"]').trigger("click");
    await flushPromises();
    clicar("usuario-criacao-salvar");
    await flushPromises();

    expect(document.querySelector('[data-testid="usuario-criacao-pessoa-erro"]')!.textContent)
      .toContain("Selecione a pessoa que será vinculada ao usuário.");
    expect(document.querySelector('[data-testid="usuario-criacao-email-erro"]')!.textContent)
      .toContain("Informe um e-mail de acesso válido.");
    expect(criarUsuario).not.toHaveBeenCalled();

    wrapper.unmount();
  });
});
