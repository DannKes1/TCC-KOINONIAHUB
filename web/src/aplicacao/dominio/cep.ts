// Corretiva G4b: o campo de CEP usa o InputMask do PrimeVue com a máscara 99999-999 e
// autoClear desligado: incompleto fica como "769__-___" e é barrado aqui, junto ao campo;
// vazio é válido (o campo é opcional). A API aceita com ou sem hífen e grava 00000-000.

export const MENSAGEM_CEP = "Informe o CEP no formato 00000-000.";

export function cepValido(valor: string | null | undefined): boolean {
  const texto = String(valor ?? "").trim();
  return texto === "" || /^\d{5}-?\d{3}$/.test(texto);
}

// Corretiva G5: o valor que entra no formulário é o mesmo que o InputMask mostra. Um CEP
// gravado antes da máscara ("7777777777", "string") aparecia formatado ("77777-777", ou
// vazio), mas o formulário guardava o valor antigo: a validação local recusava um CEP que
// a tela mostrava certo, e só um foco no campo trocava o valor. Aqui o valor gravado passa
// pela mesma regra da máscara: só dígitos, no máximo 8, encaixados em 00000-000.
export function cepParaFormulario(valor: string | null | undefined): string {
  const digitos = String(valor ?? "").replace(/\D/g, "").slice(0, 8);
  if (digitos === "") return "";
  const completo = digitos.padEnd(8, "_");
  return `${completo.slice(0, 5)}-${completo.slice(5)}`;
}
