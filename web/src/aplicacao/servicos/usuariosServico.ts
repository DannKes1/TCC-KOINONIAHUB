import { clienteHttp } from "./clienteHttp";
import type {
  UsuarioVM,
  UsuarioCriadoVM,
  ConviteVM,
  UsuarioCriarDTO,
  UsuarioAtualizarDTO,
  UsuarioResetarSenhaDTO,
} from "../modelos/dtos";

function normalizarUsuario(bruto: any): UsuarioVM {
  return {
    id: Number(bruto?.Id ?? bruto?.id ?? 0),
    igrejaId: Number(bruto?.IgrejaId ?? bruto?.igrejaId ?? 0),
    email: String(bruto?.Email ?? bruto?.email ?? ""),
    perfil: String(bruto?.Perfil ?? bruto?.perfil ?? "Usuario"),
    ativo: Boolean(bruto?.Ativo ?? bruto?.ativo ?? false),
    pessoaId:
      bruto?.PessoaId === null || bruto?.pessoaId === null
        ? null
        : Number(bruto?.PessoaId ?? bruto?.pessoaId ?? 0) || null,
    nomePessoa: (bruto?.NomePessoa ?? bruto?.nomePessoa ?? null) as
      | string
      | null,
    convitePendente: Boolean(
      bruto?.ConvitePendente ?? bruto?.convitePendente ?? false,
    ),
  };
}

function normalizarConvite(bruto: any, usuarioId: number): ConviteVM {
  return {
    usuarioId: Number(bruto?.UsuarioId ?? bruto?.usuarioId ?? usuarioId),
    email: String(bruto?.Email ?? bruto?.email ?? ""),
    nomePessoa: (bruto?.NomePessoa ?? bruto?.nomePessoa ?? null) as
      | string
      | null,
    token: String(bruto?.Token ?? bruto?.token ?? ""),
    expiraEm: String(bruto?.ExpiraEm ?? bruto?.expiraEm ?? ""),
  };
}

export async function listarUsuarios() {
  const resposta = await clienteHttp.get("/api/usuarios");
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarUsuario);
}

export async function obterUsuario(id: number) {
  const resposta = await clienteHttp.get(`/api/usuarios/${id}`);
  return normalizarUsuario(resposta.data);
}

export async function criarUsuario(
  dto: UsuarioCriarDTO,
): Promise<UsuarioCriadoVM> {
  const resposta = await clienteHttp.post("/api/usuarios", dto);
  const usuario = normalizarUsuario(resposta.data);

  return {
    ...usuario,
    conviteToken: (resposta.data?.ConviteToken ??
      resposta.data?.conviteToken ??
      null) as string | null,
    conviteExpiraEm: (resposta.data?.ConviteExpiraEm ??
      resposta.data?.conviteExpiraEm ??
      null) as string | null,
  };
}

// Gera (ou regenera) um convite de primeiro acesso para uma conta existente.
export async function gerarConviteUsuario(id: number): Promise<ConviteVM> {
  const resposta = await clienteHttp.post(`/api/usuarios/${id}/convite`);
  return normalizarConvite(resposta.data, id);
}

export async function atualizarUsuario(id: number, dto: UsuarioAtualizarDTO) {
  await clienteHttp.patch(`/api/usuarios/${id}`, dto);
}

export async function resetarSenhaUsuario(
  id: number,
  dto: UsuarioResetarSenhaDTO,
) {
  await clienteHttp.patch(`/api/usuarios/${id}/resetar-senha`, dto);
}
