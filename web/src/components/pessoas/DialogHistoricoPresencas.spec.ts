import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import { ref } from "vue";

import DialogHistoricoPresencas from "./DialogHistoricoPresencas.vue";
import { listarHistoricoPresencasDaPessoa } from "../../aplicacao/servicos/presencasPessoaServico";
import type { HistoricoPresencaPessoaVM } from "../../aplicacao/modelos/dtos";

// Corretiva da 7.3 (RF34 / CSU14; CSU07 para os totais): o diálogo carrega o
// histórico ao abrir, conta só aulas Consolidadas e lista todos os registros — em
// cartões abaixo de 768 px, em tabela no desktop.
const compacta = ref(true);
vi.mock("../../aplicacao/composables/usarTelaCompacta", () => ({
  usarTelaCompacta: () => compacta,
}));

const registros: HistoricoPresencaPessoaVM[] = [
  { aulaId: 1, dataAula: "2026-10-05T12:00:00Z", departamentoId: 7, departamentoNome: "Jovens", materiaId: 1, materiaNome: "Gênesis", presente: true, observacao: null, situacaoAula: "Consolidada" },
  { aulaId: 2, dataAula: "2026-10-12T12:00:00Z", departamentoId: 7, departamentoNome: "Jovens", materiaId: 1, materiaNome: "Gênesis", presente: false, observacao: "avisou", situacaoAula: "Consolidada" },
  { aulaId: 3, dataAula: "2026-10-19T12:00:00Z", departamentoId: 7, departamentoNome: "Jovens", materiaId: 1, materiaNome: "Gênesis", presente: true, observacao: null, situacaoAula: "EmAberto" },
];

vi.mock("../../aplicacao/servicos/presencasPessoaServico", () => ({
  listarHistoricoPresencasDaPessoa: vi.fn(async () => registros),
}));

function montar(visible = true, departamentoId?: number) {
  return mount(DialogHistoricoPresencas, {
    attachTo: document.body,
    props: { visible, pessoaId: 12, nome: "Caio", departamentoId },
    global: { plugins: [PrimeVue] },
  });
}

// Ajuste B2: o mesmo aluno com aulas na turma 7 (Jovens) e na turma 8 (Adultos). Lista
// própria dos dois casos do recorte, para não mudar os números dos casos acima.
const duasTurmas: HistoricoPresencaPessoaVM[] = [
  { aulaId: 11, dataAula: "2026-10-05T12:00:00Z", departamentoId: 7, departamentoNome: "Jovens", materiaId: 1, materiaNome: "Gênesis", presente: true, observacao: null, situacaoAula: "Consolidada" },
  { aulaId: 12, dataAula: "2026-10-12T12:00:00Z", departamentoId: 8, departamentoNome: "Adultos", materiaId: 2, materiaNome: "Êxodo", presente: false, observacao: null, situacaoAula: "Consolidada" },
  { aulaId: 13, dataAula: "2026-10-19T12:00:00Z", departamentoId: 8, departamentoNome: "Adultos", materiaId: 2, materiaNome: "Êxodo", presente: false, observacao: null, situacaoAula: "Consolidada" },
];

describe("DialogHistoricoPresencas (RF34 / CSU14)", () => {
  beforeEach(() => {
    vi.mocked(listarHistoricoPresencasDaPessoa).mockClear();
    document.body.innerHTML = "";
    compacta.value = true;
  });

  it("carrega o histórico da pessoa ao abrir e conta só as aulas Consolidadas (CSU07)", async () => {
    const wrapper = montar();
    await flushPromises();

    expect(listarHistoricoPresencasDaPessoa).toHaveBeenCalledWith(12);
    const texto = (id: string) => document.querySelector(`[data-testid="${id}"]`)!.textContent?.trim();
    expect(texto("historico-consolidadas")).toBe("2");
    expect(texto("historico-presencas")).toBe("1");
    expect(texto("historico-faltas")).toBe("1");
    expect(texto("historico-percentual")).toBe("50%");
    expect(texto("historico-total")).toBe("3");
    expect(document.body.textContent).toContain("Histórico de Presenças - Caio");
    wrapper.unmount();
  });

  it("no celular lista em cartões, do mais recente ao mais antigo, sem tabela", async () => {
    const wrapper = montar();
    await flushPromises();

    const cartoes = Array.from(document.querySelectorAll('[data-testid="historico-cartao"]'));
    expect(cartoes).toHaveLength(3);
    expect(cartoes[0]!.textContent).toContain("Em aberto");
    expect(cartoes[1]!.textContent).toContain("Ausente");
    expect(cartoes[1]!.textContent).toContain("avisou");
    expect(document.querySelector('[data-testid="historico-tabela"]')).toBeNull();
    wrapper.unmount();
  });

  it("no desktop lista na tabela de sempre", async () => {
    compacta.value = false;
    const wrapper = montar();
    await flushPromises();

    expect(document.querySelector('[data-testid="historico-tabela"]')).not.toBeNull();
    expect(document.querySelectorAll('[data-testid="historico-cartao"]')).toHaveLength(0);
    expect(document.querySelectorAll('[data-testid="historico-tabela"] tbody tr')).toHaveLength(3);
    wrapper.unmount();
  });

  it("aberto pela turma (aba Alunos), mostra só as aulas dela, com os totais dela e o aviso do recorte", async () => {
    vi.mocked(listarHistoricoPresencasDaPessoa).mockResolvedValueOnce(duasTurmas);
    const wrapper = montar(true, 7);
    await flushPromises();

    const cartoes = Array.from(document.querySelectorAll('[data-testid="historico-cartao"]'));
    expect(cartoes).toHaveLength(1);
    expect(cartoes[0]!.textContent).toContain("Jovens");
    const texto = (id: string) => document.querySelector(`[data-testid="${id}"]`)!.textContent?.trim();
    expect(texto("historico-consolidadas")).toBe("1");
    expect(texto("historico-faltas")).toBe("0");
    expect(texto("historico-percentual")).toBe("100%");
    expect(texto("historico-total")).toBe("1");
    expect(texto("historico-recorte-turma")).toBe("Mostrando somente as presenças nesta turma.");
    wrapper.unmount();
  });

  it("sem turma (tela Pessoas), mostra as aulas de todas as turmas e não avisa recorte", async () => {
    vi.mocked(listarHistoricoPresencasDaPessoa).mockResolvedValueOnce(duasTurmas);
    const wrapper = montar();
    await flushPromises();

    const cartoes = Array.from(document.querySelectorAll('[data-testid="historico-cartao"]'));
    expect(cartoes).toHaveLength(3);
    expect(document.body.textContent).toContain("Adultos");
    expect(document.querySelector('[data-testid="historico-total"]')!.textContent?.trim()).toBe("3");
    expect(document.querySelector('[data-testid="historico-recorte-turma"]')).toBeNull();
    wrapper.unmount();
  });

  it("fechado, não carrega nada", async () => {
    const wrapper = montar(false);
    await flushPromises();

    expect(listarHistoricoPresencasDaPessoa).not.toHaveBeenCalled();
    wrapper.unmount();
  });
});
