import { mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";

import TabelaParentescos from "./TabelaParentescos.vue";
import type { ParentescoVM } from "../../aplicacao/modelos/dtos";

// Etapa 6.6 (RF12 / RNF 12.3; CSU13 passo 2 e FA1; Plano 6.15 e 7.2 "Parentesco
// derivado"): a linha derivada recebe a tag "derivado" e não tem o botão de
// remover; a explícita continua com o botão e emite `remover` com o item.
const explicita: ParentescoVM = {
  id: 1,
  pessoaId: 10,
  parenteId: 20,
  tipoRelacionamento: "Mãe",
  parenteNome: "Maria",
  parenteCelular: "69999990001",
  derivado: false,
};

const derivada: ParentescoVM = {
  id: 2,
  pessoaId: 10,
  parenteId: 30,
  tipoRelacionamento: "Filho(a)",
  parenteNome: "João",
  parenteCelular: null,
  derivado: true,
};

function montar(itens: ParentescoVM[]) {
  return mount(TabelaParentescos, {
    props: { itens, carregando: false },
    global: { plugins: [PrimeVue] },
  });
}

describe("TabelaParentescos (RF12 / RNF 12.3)", () => {
  it("linha explícita: sem tag e com o botão de remover", () => {
    const wrapper = montar([explicita]);

    const linha = wrapper.get('[data-testid="parentesco-1"]');
    expect(linha.text()).toContain("Mãe");
    expect(linha.find('[data-testid="tag-derivado"]').exists()).toBe(false);
    expect(wrapper.findAll('[data-testid="parentesco-remover"]')).toHaveLength(1);
    expect(wrapper.find('[data-testid="nota-derivados"]').exists()).toBe(false);
  });

  it("linha derivada: tag 'derivado', botão de remover ausente e explicação", () => {
    const wrapper = montar([explicita, derivada]);

    const linha = wrapper.get('[data-testid="parentesco-2"]');
    expect(linha.text()).toContain("Filho(a)");
    expect(linha.get('[data-testid="tag-derivado"]').text()).toBe("derivado");
    // Só a explícita tem botão; a derivada mostra o ícone com a explicação (12.3).
    expect(wrapper.findAll('[data-testid="parentesco-remover"]')).toHaveLength(1);
    const info = wrapper.get('[data-testid="parentesco-info-derivado"]');
    expect(info.element.tagName).toBe("BUTTON");
    expect(info.attributes("type")).toBe("button");
    expect(info.attributes("aria-label")).toContain("João");
    expect(info.attributes("aria-expanded")).toBe("false");
    expect(wrapper.find('[data-testid="explicacao-derivado"]').exists()).toBe(false);
    expect(wrapper.get('[data-testid="nota-derivados"]').text()).toContain(
      "só podem ser removidos por lá",
    );
  });

  it("clicar no ⓘ mostra a explicação na própria linha; clicar de novo esconde", async () => {
    // Sem tooltip nem Popover: nada montado fora da tabela (balão órfão ao fechar o
    // diálogo) e funciona no toque, onde `title` não aparece.
    const wrapper = montar([explicita, derivada]);
    // A DataTable recria a célula ao re-renderizar: reconsultar o botão a cada passo.
    const info = () => wrapper.get('[data-testid="parentesco-info-derivado"]');

    await info().trigger("click");

    const linha = wrapper.get('[data-testid="parentesco-2"]');
    expect(linha.get('[data-testid="explicacao-derivado"]').text()).toBe(
      "Vínculo registrado no cadastro de João. Para removê-lo, abra os parentescos dessa pessoa.",
    );
    expect(info().attributes("aria-expanded")).toBe("true");
    // A explicação fica dentro da linha derivada, não na explícita nem fora da tabela.
    expect(wrapper.get('[data-testid="parentesco-1"]').find('[data-testid="explicacao-derivado"]').exists()).toBe(false);
    expect(wrapper.findAll('[data-testid="explicacao-derivado"]')).toHaveLength(1);

    await info().trigger("click");

    expect(wrapper.find('[data-testid="explicacao-derivado"]').exists()).toBe(false);
    expect(info().attributes("aria-expanded")).toBe("false");
  });

  it("recarregar a lista fecha a explicação aberta", async () => {
    const wrapper = montar([explicita, derivada]);
    await wrapper.get('[data-testid="parentesco-info-derivado"]').trigger("click");
    expect(wrapper.find('[data-testid="explicacao-derivado"]').exists()).toBe(true);

    await wrapper.setProps({ itens: [{ ...explicita }, { ...derivada }] });

    expect(wrapper.find('[data-testid="explicacao-derivado"]').exists()).toBe(false);
  });

  it("remover emite o item explícito", async () => {
    const wrapper = montar([explicita, derivada]);

    await wrapper.get('[data-testid="parentesco-remover"]').trigger("click");

    expect(wrapper.emitted("remover")).toHaveLength(1);
    expect(wrapper.emitted("remover")![0]![0]).toEqual(explicita);
  });

  it("lista vazia mantém a mensagem padrão", () => {
    const wrapper = montar([]);

    expect(wrapper.text()).toContain("Nenhum parentesco cadastrado.");
  });
});
