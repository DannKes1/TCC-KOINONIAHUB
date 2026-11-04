import { cepValido } from "./cep";

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
