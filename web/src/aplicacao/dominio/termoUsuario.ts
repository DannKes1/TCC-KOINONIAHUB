import type { AceiteTermoVM } from "../modelos/dtos";

export const TERMO_PENDENTE = "Pendente";
export const DICA_TERMO_PENDENTE =
  "A pessoa ainda não aceitou o Termo de Uso e Sigilo.";

export type ResumoTermoUsuario = {
  // Texto da tag: "Pendente", "Aceito v1.0" ou "Versão anterior (v1.0)".
  rotulo: string;
  severidade: "success" | "warn";
  // Data e hora do aceite formatadas (dd/mm/aaaa hh:mm); vazio sem aceite.
  quando: string;
  dica: string;
};

function formatarQuando(iso: string): string {
  const data = new Date(iso);
  if (Number.isNaN(data.getTime())) return "";
  return data.toLocaleString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

/**
 * RF43 / RNF 43.1: na lista de Usuários, cada conta mostra se possui aceite do
 * Termo de Uso e Sigilo, com a versão e a data e hora do aceite. `aceite` null é
 * conta sem aceite registrado; `vigente` false é aceite de uma versão anterior.
 */
export function resumirTermoUsuario(
  aceite: AceiteTermoVM | null | undefined,
): ResumoTermoUsuario {
  if (!aceite) {
    return {
      rotulo: TERMO_PENDENTE,
      severidade: "warn",
      quando: "",
      dica: DICA_TERMO_PENDENTE,
    };
  }

  const quando = formatarQuando(aceite.aceitoEm);
  if (aceite.vigente) {
    return {
      rotulo: `Aceito v${aceite.versao}`,
      severidade: "success",
      quando,
      dica: `Aceito em ${quando}.`,
    };
  }
  return {
    rotulo: `Versão anterior (v${aceite.versao})`,
    severidade: "warn",
    quando,
    dica: `Aceitou a versão ${aceite.versao} em ${quando}; a versão vigente ainda está pendente.`,
  };
}
