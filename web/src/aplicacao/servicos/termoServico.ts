import { clienteHttp } from "./clienteHttp";
import type {
  AceiteTermoVM,
  TermoIgrejaVM,
  TermoVigenteVM,
} from "../modelos/dtos";

// Termo de Uso e Sigilo (RF42 / RF43; Plano 6.3).

export function normalizarAceiteTermo(bruto: any): AceiteTermoVM | null {
  if (!bruto || typeof bruto !== "object") return null;
  return {
    versao: String(bruto?.Versao ?? bruto?.versao ?? ""),
    aceitoEm: String(bruto?.AceitoEm ?? bruto?.aceitoEm ?? ""),
    meio: String(bruto?.Meio ?? bruto?.meio ?? ""),
    vigente: Boolean(bruto?.Vigente ?? bruto?.vigente ?? false),
  };
}

function normalizarIgreja(bruto: any): TermoIgrejaVM | null {
  if (!bruto || typeof bruto !== "object") return null;
  return {
    nome: String(bruto?.Nome ?? bruto?.nome ?? ""),
    email: (bruto?.Email ?? bruto?.email ?? null) as string | null,
    telefone: (bruto?.Telefone ?? bruto?.telefone ?? null) as string | null,
  };
}

function normalizarTermo(bruto: any): TermoVigenteVM {
  return {
    versao: String(bruto?.Versao ?? bruto?.versao ?? ""),
    vigenteDesde: String(bruto?.VigenteDesde ?? bruto?.vigenteDesde ?? ""),
    texto: String(bruto?.Texto ?? bruto?.texto ?? ""),
    hash: String(bruto?.Hash ?? bruto?.hash ?? ""),
    igreja: normalizarIgreja(bruto?.Igreja ?? bruto?.igreja),
  };
}

// Público. `tokenConvite` identifica a igreja na tela de primeiro acesso.
export async function obterTermoVigente(
  tokenConvite?: string,
): Promise<TermoVigenteVM> {
  const resposta = await clienteHttp.get("/api/termo/vigente", {
    params: tokenConvite ? { token: tokenConvite } : undefined,
  });
  return normalizarTermo(resposta.data);
}

// Aceite após o login (Meio = Login). A API recusa versão diferente da vigente.
export async function aceitarTermo(versao: string): Promise<AceiteTermoVM> {
  const resposta = await clienteHttp.post("/api/termo/aceitar", {
    Versao: versao,
  });
  return normalizarAceiteTermo(resposta.data) as AceiteTermoVM;
}

export function rotuloMeioAceite(meio: string): string {
  switch (meio) {
    case "CadastroInicial":
      return "no cadastro inicial";
    case "PrimeiroAcesso":
      return "no primeiro acesso";
    case "Login":
      return "após o login";
    default:
      return "";
  }
}
