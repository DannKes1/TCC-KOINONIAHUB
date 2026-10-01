<script setup lang="ts">
import { computed } from "vue";
import DataTable from "primevue/datatable";
import Column from "primevue/column";

import type { AulaResumidaVM } from "../../aplicacao/modelos/dtos";

// Aulas apresentadas à parte nos relatórios (RNFs 35.5, 37.5, 38.4; RF36; CSU07):
// por padrão as pendentes de fechamento (Em aberto com data já ocorrida); com
// `tipo="nao-realizadas"`, as aulas Não realizadas do resumo do dia.
const props = withDefaults(
  defineProps<{
    aulas: AulaResumidaVM[];
    tipo?: "pendentes" | "nao-realizadas";
    mostrarTurma?: boolean;
  }>(),
  { tipo: "pendentes", mostrarTurma: false },
);

const titulo = computed(() =>
  props.tipo === "nao-realizadas"
    ? `Aulas Não realizadas (${props.aulas.length})`
    : `Pendentes de fechamento (${props.aulas.length})`,
);

const descricao = computed(() =>
  props.tipo === "nao-realizadas"
    ? "Encerradas sem registro de presença; não entram nos totais."
    : "Aulas em aberto com data já ocorrida: não entram nos cálculos até serem consolidadas ou marcadas como Não realizadas.",
);

const cor = computed(() => (props.tipo === "nao-realizadas" ? "#6b7280" : "#8a5b00"));
const fundo = computed(() => (props.tipo === "nao-realizadas" ? "#f3f4f6" : "#fff4e5"));

function formatarData(valor?: string | null) {
  if (!valor) return "-";
  const d = new Date(valor);
  return Number.isNaN(d.getTime()) ? "-" : d.toLocaleDateString("pt-BR");
}
</script>

<template>
  <div
    v-if="aulas.length > 0"
    class="lista-aulas-pendentes"
    data-testid="lista-aulas-pendentes"
    :style="{
      border: `1px solid ${cor}33`,
      borderLeft: `4px solid ${cor}`,
      background: fundo,
      borderRadius: '12px',
      padding: '12px 14px',
    }"
  >
    <div style="display: flex; flex-direction: column; gap: 2px; margin-bottom: 8px">
      <strong :style="{ color: cor }">{{ titulo }}</strong>
      <span style="font-size: 0.85rem; color: #4d4d4d">{{ descricao }}</span>
    </div>

    <DataTable :value="aulas" dataKey="id" size="small" responsiveLayout="scroll">
      <Column header="Data" style="width: 120px">
        <template #body="{ data }">{{ formatarData(data.data) }}</template>
      </Column>
      <Column v-if="mostrarTurma" field="departamento" header="Turma" />
      <Column field="materia" header="Matéria" />
      <Column field="professor" header="Professor" />
    </DataTable>
  </div>
</template>
