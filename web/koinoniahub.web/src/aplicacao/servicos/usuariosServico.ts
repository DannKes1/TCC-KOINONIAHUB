import { clienteHttp } from "./clienteHttp";
import type {
  UsuarioVM,
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

export async function criarUsuario(dto: UsuarioCriarDTO) {
  const resposta = await clienteHttp.post("/api/usuarios", dto);
  return normalizarUsuario(resposta.data);
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
