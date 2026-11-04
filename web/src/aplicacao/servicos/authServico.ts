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
    PessoaId?: number | null;
    // RNF 2.5 / 13.4: true quando a conta ainda não aceitou a versão vigente
    // do Termo de Uso e Sigilo; o front leva para /termo antes de qualquer tela.
    TermoPendente?: boolean;
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
  // RNF 42.1: versão do termo aceita na própria tela de cadastro inicial.
  AceiteTermoVersao: string;
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
    // Entrega G: a conta já aceitou a versão vigente do termo (redefinição de acesso,
    // CSU20); a tela dispensa a caixa e a ativação segue sem novo aceite.
    aceiteVigente: Boolean(
      resposta.data?.AceiteVigente ?? resposta.data?.aceiteVigente ?? false,
    ),
  };
}

export async function ativarPrimeiroAcessoApi(dto: {
  Token: string;
  NovaSenha: string;
  // RNF 42.1: aceite do termo faz parte da ativação do primeiro acesso. Omitido
  // quando a validação do convite informou que a conta já tem o aceite vigente.
  AceiteTermoVersao?: string;
}) {
  const resposta = await clienteHttp.post("/api/auth/primeiro-acesso", dto);
  return resposta.data as { mensagem?: string; email?: string };
}
