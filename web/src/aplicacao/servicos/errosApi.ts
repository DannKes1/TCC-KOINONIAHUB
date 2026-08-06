export function extrairMensagemErroApi(
  erro: any,
  fallback = "Não foi possível concluir a operação."
): string {
  const data = erro?.response?.data;

  // 1) string pura
  if (typeof data === "string" && data.trim()) return data;

  // 2) { mensagem: "..." }
  if (data?.mensagem) return String(data.mensagem);

  // 3) ValidationProblemDetails: { title, errors: { campo: [msg] } }
  if (data?.errors && typeof data.errors === "object") {
    const mensagens = Object.values(data.errors)
      .flat()
      .map((m) => String(m))
      .filter(Boolean);

    if (mensagens.length) return mensagens.join(" ");
    if (data?.title) return String(data.title);
  }

  // 4) fallback com HTTP
  const status = erro?.response?.status;
  if (status) return `${fallback} (HTTP ${status}).`;

  return erro?.message ?? fallback;
}