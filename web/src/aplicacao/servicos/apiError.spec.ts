import { mensagemForaDosCampos, parseApiError } from "./apiError";

// Corretiva G3: a mensagem geral de um erro da API vai para o aviso no topo do diálogo
// só quando algum campo devolvido não tem FieldError na tela; com todos os campos
// exibidos, o erro fica junto do campo e o aviso some.
describe("mensagemForaDosCampos (Corretiva G3)", () => {
  it("sem erro por campo, devolve a mensagem geral", () => {
    expect(mensagemForaDosCampos("Este e-mail já está em uso por outra pessoa.", {}, ["Email"])).toBe(
      "Este e-mail já está em uso por outra pessoa.",
    );
  });

  it("todos os campos devolvidos exibidos no diálogo: aviso vazio, sem distinguir a caixa", () => {
    const erros = { email: ["inválido"], Perfil: ["obrigatório"] };
    expect(mensagemForaDosCampos("Alguns campos enviados são inválidos.", erros, ["Email", "Perfil"])).toBe("");
  });

  it("campo devolvido sem lugar no diálogo mantém o aviso geral", () => {
    const erros = { Celular: ["inválido"] };
    expect(mensagemForaDosCampos("Alguns campos enviados são inválidos.", erros, ["Email"])).toBe(
      "Alguns campos enviados são inválidos.",
    );
  });

  it("parseApiError: 400 com { mensagem } não tem erro por campo", () => {
    const parsed = parseApiError({ response: { status: 400, data: { mensagem: "Regra." } } }, "Padrão.");
    expect(parsed).toEqual({ message: "Regra.", fieldErrors: {}, status: 400 });
    expect(mensagemForaDosCampos(parsed.message, parsed.fieldErrors, ["Email"])).toBe("Regra.");
  });
});
