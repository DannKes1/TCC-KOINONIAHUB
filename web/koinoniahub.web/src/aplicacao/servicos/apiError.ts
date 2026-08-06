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

  // 1) ValidationProblemDetails: { errors: { field: ["msg"] }, title, status }
  if (data?.errors && typeof data.errors === "object") {
    for (const [k, v] of Object.entries(data.errors)) {
      fieldErrors[k] = Array.isArray(v) ? v.map(String) : [String(v)];
    }
    // O título padrão do ASP.NET vem em inglês; usa mensagem amigável
    const msg =
      "Alguns campos enviados são inválidos. Verifique os dados e tente novamente.";
    return { message: msg, fieldErrors, status };
  }

  // 2) { mensagem: "..." }
  if (data?.mensagem) {
    return { message: String(data.mensagem), fieldErrors, status };
  }

  // 3) string
  if (typeof data === "string" && data.trim()) {
    return { message: data.trim(), fieldErrors, status };
  }

  // 4) rede / timeout
  if (!err?.response) {
    return {
      message: err?.message ?? "Não foi possível conectar ao servidor.",
      fieldErrors,
      status,
    };
  }

  // 5) fallback com status
  return {
    message: status ? `${fallback} (HTTP ${status}).` : fallback,
    fieldErrors,
    status,
  };
}

// atalho: pega o primeiro erro de um campo
export function firstFieldError(
  fieldErrors: Record<string, string[]>,
  field: string,
): string {
  return fieldErrors[field]?.[0] ?? "";
}
