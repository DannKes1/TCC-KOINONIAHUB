import { cepParaFormulario, cepValido } from "./cep";

// Corretiva G4b: validação do CEP no campo (a máscara é do InputMask).
describe("cepValido", () => {
  it("aceita vazio e 8 dígitos com ou sem hífen", () => {
    expect(cepValido("")).toBe(true);
    expect(cepValido(null)).toBe(true);
    expect(cepValido("76900-000")).toBe(true);
    expect(cepValido("76900000")).toBe(true);
  });

  it("recusa o CEP incompleto da máscara e qualquer outro formato", () => {
    expect(cepValido("769__-___")).toBe(false);
    expect(cepValido("76900-00")).toBe(false);
    expect(cepValido("7777777777")).toBe(false);
    expect(cepValido("7690A-AAA")).toBe(false);
  });
});

// Corretiva G5: o formulário recebe o mesmo valor que o InputMask mostra, inclusive para
// CEP gravado antes da máscara.
describe("cepParaFormulario", () => {
  it("formata 8 dígitos e mantém vazio", () => {
    expect(cepParaFormulario("76900000")).toBe("76900-000");
    expect(cepParaFormulario("76900-000")).toBe("76900-000");
    expect(cepParaFormulario("76.900-000")).toBe("76900-000");
    expect(cepParaFormulario(null)).toBe("");
    expect(cepParaFormulario("")).toBe("");
  });

  it("CEP antigo: mais de 8 dígitos fica com os 8 que a máscara mostra; sem dígitos fica vazio", () => {
    expect(cepParaFormulario("7777777777")).toBe("77777-777");
    expect(cepValido(cepParaFormulario("7777777777"))).toBe(true);
    expect(cepParaFormulario("string")).toBe("");
  });

  it("CEP antigo incompleto aparece incompleto e continua barrado", () => {
    expect(cepParaFormulario("769")).toBe("769__-___");
    expect(cepValido(cepParaFormulario("769"))).toBe(false);
  });
});
