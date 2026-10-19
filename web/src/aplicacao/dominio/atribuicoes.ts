import type { AtribuicaoVM } from "../modelos/dtos";

export const AVISO_SEM_CONTA = "Sem conta";
export const AVISO_PERFIL_USUARIO = "Conta com perfil Usuário — não acessa a turma";
// Abaixo de 768 px (tela compacta) o texto curto cabe em duas linhas na coluna Pessoa.
export const AVISO_PERFIL_USUARIO_CURTO = "Perfil Usuário — não acessa a turma";

/**
 * RNF 21.5: a atribuição só produz efeito para conta de perfil Professor ou
 * administrativo (RNF 21.4). Sem conta, ou com conta de perfil Usuário, a pessoa não
 * acessa a turma — e a gestão precisa ver isso na lista, porque Pastor e
 * Superintendente não listam usuários. `perfilConta` vem da API (null = sem conta).
 * Devolve o texto da tag ou null quando a conta opera a turma.
 */
export function avisoConta(
  item: Pick<AtribuicaoVM, "perfilConta">,
  compacta = false,
): string | null {
  const perfil = String(item.perfilConta ?? "").trim();
  if (!perfil) return AVISO_SEM_CONTA;
  if (perfil.toLowerCase() === "usuario") {
    return compacta ? AVISO_PERFIL_USUARIO_CURTO : AVISO_PERFIL_USUARIO;
  }
  return null;
}
