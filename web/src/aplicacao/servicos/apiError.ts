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
