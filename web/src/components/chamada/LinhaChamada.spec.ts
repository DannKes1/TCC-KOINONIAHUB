import { mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";

import LinhaChamada from "./LinhaChamada.vue";
import type { LinhaChamada as LinhaChamadaVM } from "../../aplicacao/dominio/chamada";

// RF32 / CSU10 (Etapa 6.4-B): tocar na linha alterna a presença; a observação só
// aparece quando pedida e vem com a orientação da RNF 32.5; em aula fechada nada
// é editável.
function montar(linha: Partial<LinhaChamadaVM> = {}, props: Record<string, unknown> = {}) {
  const base: LinhaChamadaVM = {
    alunoDepartamentoId: 7,
    pessoaId: 70,
    nomeAluno: "Ana Luiza",
    presente: false,
    observacao: null,
    registrado: true,
    ...linha,
  };
  return mount(LinhaChamada, {
    props: { linha: base, ...props },
    global: { plugins: [PrimeVue] },
  });
}

describe("LinhaChamada", () => {
  it("tocar na linha emite a presença invertida; a caixa também", async () => {
    const w = montar();

    await w.find('[data-testid="chamada-linha"]').trigger("click");
    expect(w.emitted("update:presente")).toEqual([[true]]);

    // A caixa de seleção não dispara o toque da linha (senão alternaria duas vezes).
    await w.find('[data-testid="chamada-presente"] input').setValue(true);
    expect(w.emitted("update:presente")).toEqual([[true], [true]]);

    w.unmount();
  });

  it("tocar em qualquer ponto da faixa de baixo (fora do botão '+ observação') também alterna", async () => {
    // Regressão: o invólucro do botão tinha @click.stop e, esticado na coluna,
    // engolia o toque em toda a metade de baixo da linha.
    const w = montar();
    await w.find(".linha-chamada-obs").trigger("click");
    await w.find('[data-testid="chamada-nome"]').trigger("click");
    expect(w.emitted("update:presente")).toEqual([[true], [true]]);
    w.unmount();
  });

  it("teclado: Enter e Espaço alternam", async () => {
    const w = montar({ presente: true });
    await w.find('[data-testid="chamada-linha"]').trigger("keydown", { key: "Enter" });
    await w.find('[data-testid="chamada-linha"]').trigger("keydown", { key: " " });
    expect(w.emitted("update:presente")).toEqual([[false], [false]]);
    w.unmount();
  });

  it("'+ observação' abre o campo com a orientação da RNF 32.5 e não alterna a presença", async () => {
    const w = montar();

    expect(w.find('[data-testid="chamada-observacao"]').exists()).toBe(false);
    expect(w.find('[data-testid="dica-observacao"]').exists()).toBe(false);

    await w.find('[data-testid="chamada-abrir-observacao"]').trigger("click");

    expect(w.find('[data-testid="chamada-observacao"]').exists()).toBe(true);
    expect(w.find('[data-testid="dica-observacao"]').text()).toContain("informações de saúde");
    expect(w.emitted("update:presente")).toBeUndefined();

    await w.find('[data-testid="chamada-observacao"]').setValue("chegou atrasado");
    const emitidos = () => w.emitted("update:observacao") ?? [];
    expect(emitidos()[emitidos().length - 1]).toEqual(["chegou atrasado"]);

    await w.find('[data-testid="chamada-remover-observacao"]').trigger("click");
    expect(emitidos()[emitidos().length - 1]).toEqual([null]);
    expect(w.find('[data-testid="chamada-observacao"]').exists()).toBe(false);

    w.unmount();
  });

  it("linha com observação salva já abre com o campo preenchido", () => {
    const w = montar({ observacao: "dispensado" });
    expect((w.find('[data-testid="chamada-observacao"]').element as HTMLInputElement).value).toBe(
      "dispensado",
    );
    w.unmount();
  });

  it("somente leitura: nada alterna, sem '+ observação', texto da observação visível", async () => {
    const w = montar({ presente: true, observacao: "saída antecipada" }, { somenteLeitura: true });

    await w.find('[data-testid="chamada-linha"]').trigger("click");
    expect(w.emitted("update:presente")).toBeUndefined();
    expect(w.find('[data-testid="chamada-abrir-observacao"]').exists()).toBe(false);
    expect(w.find('[data-testid="chamada-observacao"]').exists()).toBe(false);
    expect(w.find('[data-testid="chamada-observacao-texto"]').text()).toContain("saída antecipada");
    expect((w.find('[data-testid="chamada-presente"] input').element as HTMLInputElement).disabled).toBe(true);

    w.unmount();
  });

  it("destaca a linha apontada pela API como sem registro", () => {
    const w = montar({}, { semRegistro: true });
    expect(w.find('[data-testid="chamada-linha"]').classes()).toContain("linha-chamada-sem-registro");
    expect(w.text()).toContain("Sem registro");
    w.unmount();
  });
});
