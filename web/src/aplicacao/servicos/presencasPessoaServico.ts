import { clienteHttp } from "./clienteHttp";
import type { HistoricoPresencaPessoaVM } from "../modelos/dtos";

function normalizarHistorico(bruto: any): HistoricoPresencaPessoaVM {
  return {
    aulaId: Number(bruto?.Id ?? bruto?.AulaId ?? bruto?.aulaId ?? 0),
    dataAula: String(bruto?.DataAula ?? bruto?.dataAula ?? ""),
    departamentoId: Number(bruto?.DepartamentoId ?? bruto?.departamentoId ?? 0),
    departamentoNome: String(
      bruto?.DepartamentoNome ?? bruto?.departamentoNome ?? "",
    ),
    materiaId: Number(bruto?.MateriaId ?? bruto?.materiaId ?? 0),
    materiaNome: String(bruto?.MateriaNome ?? bruto?.materiaNome ?? ""),
    presente: Boolean(bruto?.Presente ?? bruto?.presente ?? false),
    observacao: (bruto?.Observacao ?? bruto?.observacao ?? null) as string | null,
  };
}

export async function listarHistoricoPresencasDaPessoa(pessoaId: number) {
  const resposta = await clienteHttp.get(`/api/pessoas/${pessoaId}/presencas`);
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarHistorico);
}