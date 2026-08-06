<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute } from "vue-router";


import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";


import { useAsync } from "../../../aplicacao/composables/useAsync";


import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Tag from "primevue/tag";


import {
  obterMinhaFrequencia,
  type MinhaFrequenciaTurmaVM,
} from "../../../aplicacao/servicos/meusDadosServico";

const route = useRoute();
const { carregando, erro, run } = useAsync();

const departamentoId = computed(() => Number(route.params.departamentoId));
const dados = ref<MinhaFrequenciaTurmaVM | null>(null);

function formatarData(valor?: string | null) {
  if (!valor) return "-";
  const d = new Date(valor);
  if (Number.isNaN(d.getTime())) return "-";
  return d.toLocaleDateString("pt-BR");
}

function corSituacao(s: string): "success" | "danger" | "secondary" {
  if (s === "Presente") return "success";
  if (s === "Ausente") return "danger";
  return "secondary"; // Não registrado
}

async function carregar() {
  if (!departamentoId.value) {
    erro.value = "Turma inválida.";
    return;
  }

  await run(async () => {
    dados.value = await obterMinhaFrequencia(departamentoId.value);
  }, "Não foi possível carregar sua frequência.");
}

onMounted(carregar);
</script>

<template>
  <div>
    <PageHeader
      titulo="Minha frequência"
      :subtitulo="dados ? dados.nomeDepartamento : 'Sua presença nesta turma'"
      voltarPara="/"
      voltarLabel="Início"
    />

    <InlineMessage v-if="erro" :texto="erro" tipo="erro" />

    <LoadingOverlay :loading="carregando" texto="Carregando sua frequência...">
      <div
        v-if="dados"
        style="display: flex; flex-direction: column; gap: 16px"
      >
       
        <div style="display: flex; gap: 12px; flex-wrap: wrap">
          <div
            style="
              padding: 12px;
              border: 1px solid var(--p-surface-border);
              border-radius: 12px;
              min-width: 130px;
            "
          >
            <div style="opacity: 0.7">% Presença</div>
            <div style="font-size: 20px">
              {{ Number(dados.percentualPresenca).toFixed(2) }}%
            </div>
          </div>
          <div
            style="
              padding: 12px;
              border: 1px solid var(--p-surface-border);
              border-radius: 12px;
              min-width: 130px;
            "
          >
            <div style="opacity: 0.7">Presenças</div>
            <div style="font-size: 20px">{{ dados.presentes }}</div>
          </div>
          <div
            style="
              padding: 12px;
              border: 1px solid var(--p-surface-border);
              border-radius: 12px;
              min-width: 130px;
            "
          >
            <div style="opacity: 0.7">Faltas</div>
            <div style="font-size: 20px">{{ dados.ausentesMarcados }}</div>
          </div>
          <div
            style="
              padding: 12px;
              border: 1px solid var(--p-surface-border);
              border-radius: 12px;
              min-width: 130px;
            "
          >
            <div style="opacity: 0.7">Não registrado</div>
            <div style="font-size: 20px">{{ dados.naoRegistrado }}</div>
          </div>
          <div
            style="
              padding: 12px;
              border: 1px solid var(--p-surface-border);
              border-radius: 12px;
              min-width: 130px;
            "
          >
            <div style="opacity: 0.7">Aulas no período</div>
            <div style="font-size: 20px">{{ dados.totalAulas }}</div>
          </div>
        </div>

        <InlineMessage
          v-if="dados.aulas.length === 0"
          texto="Nenhuma aula registrada neste período."
          tipo="info"
        />

        <DataTable
          v-else
          :value="dados.aulas"
          paginator
          :rows="10"
          rowHover
          dataKey="aulaId"
          responsiveLayout="scroll"
        >
          <Column header="Data" style="width: 140px">
            <template #body="{ data }">
              {{ formatarData(data.data) }}
            </template>
          </Column>
          <Column field="tema" header="Tema">
            <template #body="{ data }">
              {{ data.tema || "-" }}
            </template>
          </Column>
          <Column header="Situação" style="width: 160px">
            <template #body="{ data }">
              <Tag
                :severity="corSituacao(data.situacao)"
                :value="data.situacao"
              />
            </template>
          </Column>
        </DataTable>
      </div>
    </LoadingOverlay>
  </div>
</template>
