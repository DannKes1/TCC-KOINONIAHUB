import { clienteHttp } from "./clienteHttp";

export async function loginApi(dto: { Email: string; Senha: string }) {
  const resposta = await clienteHttp.post("/api/auth/login", dto);
  return resposta.data as {
    Token: string;
    ExpiraEm: string;
    UsuarioId: number;
    EmailUsuario: string;
    Perfil: string;
    IgrejaId: number;
  };
}

export async function registrarAdminApi(dto: {
  Igreja: { Nome: string; Cidade?: string | null; Estado?: string | null; Email?: string | null }
  EmailAdmin: string
  SenhaAdmin: string
  NomeAdmin: string
}) {
  const resposta = await clienteHttp.post('/api/auth/registrar-admin', dto)
  return resposta.data
}