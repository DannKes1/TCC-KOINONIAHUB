import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";

import DialogImportarPessoas from "./DialogImportarPessoas.vue";
import { importarPessoas } from "../../../aplicacao/servicos/pessoasServico";
import { toastSuccess, toastWarn } from "../../../aplicacao/servicos/notificacoes";

// Corretiva G4 (RF41): o erro da API aparece no topo do diálogo de importação, a
// validação local ("selecione um arquivo") junto ao campo, e nenhum aviso sai em toast —
// o relatório dentro do diálogo já traz as linhas ignoradas e para conferência.
vi.mock("../../../aplicacao/servicos/pessoasServico", () => ({
  importarPessoas: vi.fn(),
}));

vi.mock("../../../aplicacao/servicos/notificacoes", () => ({
  toastSuccess: vi.fn(),
  toastWarn: vi.fn(),
}));

function montar() {
  const wrapper = mount(DialogImportarPessoas, {
    props: { visible: true },
    attachTo: document.body,
    global: { plugins: [PrimeVue], directives: { tooltip: Tooltip } },
  });
  return wrapper;
}

function escolherArquivo() {
  const input = document.querySelector<HTMLInputElement>('[data-testid="importacao-arquivo"]')!;
  Object.defineProperty(input, "files", {
    configurable: true,
    value: [new File(["Nome;Email\nAna;ana@teste.com"], "pessoas.csv", { type: "text/csv" })],
  });
  input.dispatchEvent(new Event("change"));
}

function clicarImportar() {
  document.querySelector<HTMLButtonElement>('[data-testid="importacao-enviar"]')!.click();
}

describe("DialogImportarPessoas — erro dentro do diálogo (Corretiva G4)", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
    vi.mocked(importarPessoas).mockReset();
    vi.mocked(toastSuccess).mockClear();
    vi.mocked(toastWarn).mockClear();
  });

  it("400 da API: mensagem no topo do diálogo, sem toast; some ao escolher outro arquivo", async () => {
    const EXCEDE = "O arquivo excede 1000 linhas.";
    vi.mocked(importarPessoas).mockRejectedValue({ response: { status: 400, data: { mensagem: EXCEDE } } });
    const wrapper = montar();
    await flushPromises();

    escolherArquivo();
    await flushPromises();
    clicarImportar();
    await flushPromises();

    expect(importarPessoas).toHaveBeenCalledTimes(1);
    const dialogo = document.querySelector(".p-dialog")!;
    expect(dialogo.querySelector('[data-testid="importacao-dialogo-erro"]')!.textContent).toContain(EXCEDE);
    // O aviso é o primeiro elemento do corpo do diálogo, acima das instruções.
    expect(dialogo.querySelector(".p-dialog-content")!.textContent!.indexOf(EXCEDE))
      .toBeLessThan(dialogo.querySelector(".p-dialog-content")!.textContent!.indexOf("Importe de uma só vez"));
    expect(toastWarn).not.toHaveBeenCalled();

    escolherArquivo();
    await flushPromises();
    expect(document.querySelector('[data-testid="importacao-dialogo-erro"]')).toBeNull();

    wrapper.unmount();
  });

  it("resultado sem pessoa nova e com linhas para conferência: só o relatório, nenhum toast", async () => {
    vi.mocked(importarPessoas).mockResolvedValue({
      totalLinhas: 2,
      criados: 0,
      ignorados: 2,
      erros: 0,
      paraConferencia: 1,
      itens: [
        { linha: 2, nome: "Ana", status: "Ignorado", mensagem: "Nome já cadastrado (conferir).", paraConferencia: true },
        { linha: 3, nome: "Bia", status: "Ignorado", mensagem: "Este e-mail já está em uso por outra pessoa (linha ignorada).", paraConferencia: false },
      ],
    } as any);
    const wrapper = montar();
    await flushPromises();

    escolherArquivo();
    await flushPromises();
    clicarImportar();
    await flushPromises();

    expect(document.querySelector('[data-testid="importacao-para-conferencia"]')!.textContent).toContain("1");
    expect(toastWarn).not.toHaveBeenCalled();
    expect(toastSuccess).not.toHaveBeenCalled();
    expect(wrapper.emitted("importado")).toBeUndefined();

    wrapper.unmount();
  });
});
