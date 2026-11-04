// Corretiva G4b: o campo de CEP usa o InputMask do PrimeVue com a máscara 99999-999 e
// autoClear desligado: incompleto fica como "769__-___" e é barrado aqui, junto ao campo;
// vazio é válido (o campo é opcional). A API aceita com ou sem hífen e grava 00000-000.

export const MENSAGEM_CEP = "Informe o CEP no formato 00000-000.";

export function cepValido(valor: string | null | undefined): boolean {
  const texto = String(valor ?? "").trim();
  return texto === "" || /^\d{5}-?\d{3}$/.test(texto);
}
