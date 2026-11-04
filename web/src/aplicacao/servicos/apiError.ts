export type ApiError = {
  message: string;
  fieldErrors: Record<string, string[]>;
  status?: number;
};

export function parseApiError(
  err: any,
  fallback = "Não foi possível concluir a operação.",
): ApiError {
  const status = err?.response?.status;
  const data = err?.response?.data;

  const fieldErrors: Record<string, string[]> = {};

  
  if (data?.errors && typeof data.errors === "object") {
    for (const [k, v] of Object.entries(data.errors)) {
      fieldErrors[k] = Array.isArray(v) ? v.map(String) : [String(v)];
    }
   

    const msg =
      "Alguns campos enviados são inválidos. Verifique os dados e tente novamente.";
    return { message: msg, fieldErrors, status };
  }


  
  if (data?.mensagem) {
    return { message: String(data.mensagem), fieldErrors, status };
  }

  if (typeof data === "string" && data.trim()) {
    return { message: data.trim(), fieldErrors, status };
  }

 
  if (!err?.response) {
    return {
      message: err?.message ?? "Não foi possível conectar ao servidor.",
      fieldErrors,
      status,
    };
  }

  
  return {
    message: status ? `${fallback} (HTTP ${status}).` : fallback,
    fieldErrors,
    status,
  };
}


export function firstFieldError(
  fieldErrors: Record<string, string[]>,
  field: string,
): string {
  return fieldErrors[field]?.[0] ?? "";
}

// Corretiva G3: mensagem geral de um erro da API para o aviso no topo de um diálogo.
// Fica vazia quando todos os campos devolvidos pela API (`errors` do
// ValidationProblemDetails) têm um FieldError exibido no diálogo — o erro já está
// junto do campo. Um campo sem lugar na tela mantém o aviso geral, para o erro nunca
// ficar invisível. A comparação ignora a caixa ("Email" e "email").
export function mensagemForaDosCampos(
  erro: string,
  fieldErrors: Record<string, string[]>,
  camposExibidos: string[],
): string {
  const chaves = Object.keys(fieldErrors);
  if (chaves.length === 0) return erro;
  const exibidos = new Set(camposExibidos.map((c) => c.toLowerCase()));
  return chaves.every((c) => exibidos.has(c.toLowerCase())) ? "" : erro;
}
