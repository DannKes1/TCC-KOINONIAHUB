import { nextTick, reactive, ref } from "vue";

import { usarFormularioDialogo } from "./usarFormularioDialogo";

const rejeicao = (data: unknown) => ({ response: { status: 400, data } });

// Corretiva G5: o composable que concentra o padrão de erro dos diálogos (G3/G4).
describe("usarFormularioDialogo", () => {
  it("400 com mensagem vai para o aviso; o erro local tem prioridade no campo", async () => {
    const f = usarFormularioDialogo();
    const aviso = f.aviso(["Nome"]);

    await f.executar(async () => {
      throw rejeicao({ mensagem: "Regra." });
    }, "Padrão.");
    expect(aviso.value).toBe("Regra.");
    expect(f.erroDoCampo("Nome")).toBe("");

    f.errosLocais.Nome = "Informe o nome.";
    expect(f.erroDoCampo("Nome")).toBe("Informe o nome.");
    expect(f.semErrosLocais()).toBe(false);
  });

  it("400 por campo fica junto ao campo (Pascal ou camel) e o aviso fica vazio", async () => {
    const f = usarFormularioDialogo();
    const aviso = f.aviso(["Email", "CEP"]);

    await f.executar(async () => {
      throw rejeicao({ errors: { email: ["Informe um e-mail válido."], CEP: ["Formato."] } });
    }, "Padrão.");

    expect(f.erroDoCampo("Email")).toBe("Informe um e-mail válido.");
    expect(f.erroDoCampo("CEP", "cep")).toBe("Formato.");
    expect(aviso.value).toBe("");
  });

  it("limpa ao alterar o formulário e ao fechar todos os diálogos da instância", async () => {
    const f = usarFormularioDialogo();
    const aviso = f.aviso([]);
    const criacao = ref(true);
    const edicao = ref(false);
    const formulario = reactive({ nome: "" });
    f.limparAoFechar(criacao, edicao);
    f.limparAoAlterar(formulario);

    const falhar = () =>
      f.executar(async () => {
        throw rejeicao({ mensagem: "Regra." });
      }, "Padrão.");

    await falhar();
    f.errosLocais.Nome = "Informe o nome.";
    formulario.nome = "Ana";
    await nextTick();
    expect(aviso.value).toBe("");
    expect(f.semErrosLocais()).toBe(true);

    await falhar();
    edicao.value = true;
    criacao.value = false;
    await nextTick();
    expect(aviso.value).toBe("Regra.");

    edicao.value = false;
    await nextTick();
    expect(aviso.value).toBe("");
  });
});
