import { clienteHttp } from "./clienteHttp";

// O JWT não vem no corpo: fica só no cookie httpOnly gravado pela API
// (monografia, seção 4.8). O corpo traz apenas os dados de sessão.
export async function loginApi(dto: { Email: string; Senha: string }) {
  const resposta = await clienteHttp.post("/api/auth/login", dto);
  return resposta.data as {
    ExpiraEm: string;
    UsuarioId: number;
    EmailUsuario: string;
    Perfil: string;
    IgrejaId: number;
  };
}

export async function registrarAdminApi(dto: {
  Igreja: {
    Nome: string;
    Cidade?: string | null;
    Estado?: string | null;
    Email?: string | null;
  };
  EmailAdmin: string;
  SenhaAdmin: string;
  NomeAdmin: string;
}) {
  const resposta = await clienteHttp.post("/api/auth/registrar-admin", dto);
  return resposta.data;
}

// ---- Primeiro acesso por convite (rotas públicas) ----

export async function validarPrimeiroAcessoApi(token: string) {
  const resposta = await clienteHttp.get(
    `/api/auth/primeiro-acesso/${encodeURIComponent(token)}`,
  );
  return {
    email: String(resposta.data?.Email ?? resposta.data?.email ?? ""),
    nomePessoa: (resposta.data?.NomePessoa ??
      resposta.data?.nomePessoa ??
      null) as string | null,
  };
}

export async function ativarPrimeiroAcessoApi(dto: {
  Token: string;
  NovaSenha: string;
}) {
  const resposta = await clienteHttp.post("/api/auth/primeiro-acesso", dto);
  return resposta.data as { mensagem?: string; email?: string };
}
