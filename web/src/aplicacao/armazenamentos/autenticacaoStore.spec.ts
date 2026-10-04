import { createPinia, setActivePinia } from "pinia";

import { usarAutenticacaoStore } from "./autenticacaoStore";
import { clienteHttp } from "../servicos/clienteHttp";

// Plano 7.2 (RNF 2.5 / 13.4): a store guarda a pendência do termo vinda do
// login, persiste no storage e limpa tudo ao sair.
function sessao(extras: Record<string, unknown> = {}) {
  return {
    ExpiraEm: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    UsuarioId: 7,
    EmailUsuario: "pessoa@teste.com",
    Perfil: "Professor",
    IgrejaId: 1,
    PessoaId: 3,
    ...extras,
  };
}

describe("autenticacaoStore — termo pendente", () => {
  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
    vi.spyOn(clienteHttp, "post").mockResolvedValue({ data: null } as any);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("lê TermoPendente do login (PascalCase) e persiste no storage", () => {
    const store = usarAutenticacaoStore();

    store.entrar(sessao({ TermoPendente: true }));

    expect(store.termoPendente).toBe(true);
    expect(store.autenticado).toBe(true);
    const salvo = JSON.parse(localStorage.getItem("koinoniahub_sessao") ?? "{}");
    expect(salvo.termoPendente).toBe(true);
  });

  it("sem o campo, a pendência é falsa (contas antigas / resposta sem o campo)", () => {
    const store = usarAutenticacaoStore();

    store.entrar(sessao());

    expect(store.termoPendente).toBe(false);
  });

  it("marcarTermoPendente(false) libera e atualiza o storage", () => {
    const store = usarAutenticacaoStore();
    store.entrar(sessao({ termoPendente: true }));

    store.marcarTermoPendente(false);

    expect(store.termoPendente).toBe(false);
    const salvo = JSON.parse(localStorage.getItem("koinoniahub_sessao") ?? "{}");
    expect(salvo.termoPendente).toBe(false);
  });

  it("carregarDoStorage recupera a pendência", () => {
    usarAutenticacaoStore().entrar(sessao({ TermoPendente: true }));

    setActivePinia(createPinia());
    const nova = usarAutenticacaoStore();
    nova.carregarDoStorage();

    expect(nova.autenticado).toBe(true);
    expect(nova.termoPendente).toBe(true);
  });

  it("sair limpa a sessão e a pendência", async () => {
    const store = usarAutenticacaoStore();
    store.entrar(sessao({ TermoPendente: true }));

    await store.sair();

    expect(store.sessaoAtiva).toBe(false);
    expect(store.termoPendente).toBe(false);
    expect(localStorage.getItem("koinoniahub_sessao")).toBeNull();
    expect(clienteHttp.post).toHaveBeenCalledWith("/api/auth/logout");
  });
});
