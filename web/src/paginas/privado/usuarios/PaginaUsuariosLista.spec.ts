import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaUsuariosLista from "./PaginaUsuariosLista.vue";
import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

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

vi.mock("../../../aplicacao/servicos/pessoasServico", () => ({
  listarPessoas: vi.fn(async () => []),
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
