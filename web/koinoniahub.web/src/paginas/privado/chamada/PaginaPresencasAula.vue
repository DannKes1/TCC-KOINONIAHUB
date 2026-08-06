<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

// UI base
import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";

// Composable
import { useAsync } from "../../../aplicacao/composables/useAsync";

// Serviços
import { obterAula } from "../../../aplicacao/servicos/aulasServico";
import { listarPresencasRegistradas } from "../../../aplicacao/servicos/chamadasServico";

// Tipos
import type { AulaVM, PresencaVM } from "../../../aplicacao/modelos/dtos";

// PrimeVue
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import Tag from "primevue/tag";

const route = useRoute();
const router = useRouter();

const { carregando, erro, run } = useAsync();

const aulaId = Number(route.params.aulaId ?? 0);

const aula = ref<AulaVM | null>(null);
const presencas = ref<PresencaVM[]>([]);

function formatarData(valor?: string | null) {
  if (!valor) return "-";

  const data = new Date(valor);
  if (Number.isNaN(data.getTime())) return "-";

  return data.toLocaleDateString("pt-BR");
}

function formatarDataHora(valor?: string | null) {
  if (!valor) return "-";

  const data = new Date(valor);
  if (Number.isNaN(data.getTime())) return "-";

  return data.toLocaleString("pt-BR");
}

function severityPresenca(presente: boolean) {
  return presente ? "success" : "danger";
}

function abrirChamada() {
  router.push(`/aulas/${aulaId}/chamada`);
}

const totalRegistros = computed(() => presencas.value.length);

const totalPresentes = computed(
  () => presencas.value.filter((item) => item.presente).length,
);

const totalAusentes = computed(
  () => presencas.value.filter((item) => !item.presente).length,
);

async function carregarTela() {
  await run(async () => {
    const [aulaCarregada, presencasCarregadas] = await Promise.all([
      obterAula(aulaId),
      listarPresencasRegistradas(aulaId),
    ]);

    aula.value = aulaCarregada;
    presencas.value = presencasCarregadas;
  }, "Não foi possível carregar as presenças registradas da aula.");
}

onMounted(carregarTela);
</script>

<template>
  <div class="page-container">
    <PageHeader
      :titulo="`Presenças Registradas - ${aula?.tema || 'Aula'}`"
      subtitulo="Consulta apenas dos registros já salvos para esta aula"
      voltarPara="/departamentos"
      voltarLabel="Turmas EBD"
    >
      <template #acoes>
        <Button
          label="Abrir Chamada"
          icon="pi pi-check-square"
          severity="secondary"
          v-tooltip.top="'Ir para o registro de chamada desta aula'"
          @click="abrirChamada"
        />
        <Button
          label="Recarregar"
          icon="pi pi-refresh"
          severity="secondary"
          :loading="carregando"
          @click="carregarTela"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <div
      style="
        display: grid;
        grid-template-columns: repeat(3, minmax(180px, 1fr));
        gap: 12px;
      "
    >
      <div
        style="
          border: 1px solid rgba(0, 0, 0, 0.08);
          border-radius: 12px;
          padding: 12px;
        "
      >
        <div style="font-size: 12px; opacity: 0.7">Total de registros</div>
        <div style="font-size: 24px; font-weight: 700">
          {{ totalRegistros }}
        </div>
      </div>

      <div
        style="
          border: 1px solid rgba(0, 0, 0, 0.08);
          border-radius: 12px;
          padding: 12px;
        "
      >
        <div style="font-size: 12px; opacity: 0.7">Presentes</div>
        <div style="font-size: 24px; font-weight: 700">
          {{ totalPresentes }}
        </div>
      </div>

      <div
        style="
          border: 1px solid rgba(0, 0, 0, 0.08);
          border-radius: 12px;
          padding: 12px;
        "
      >
        <div style="font-size: 12px; opacity: 0.7">Ausentes</div>
        <div style="font-size: 24px; font-weight: 700">{{ totalAusentes }}</div>
      </div>
    </div>

    <div
      v-if="aula"
      style="
        display: grid;
        grid-template-columns: repeat(4, minmax(180px, 1fr));
        gap: 12px;
      "
    >
      <div><strong>Data:</strong> {{ formatarData(aula.data) }}</div>
      <div><strong>Matéria:</strong> {{ aula.nomeMateria }}</div>
      <div><strong>Professor:</strong> {{ aula.nomeProfessor }}</div>
      <div>
        <strong>Status:</strong>
        <Tag
          :value="aula.consolidada ? 'Consolidada' : 'Aberta'"
          :severity="aula.consolidada ? 'success' : 'warning'"
        />
      </div>
    </div>

    <LoadingOverlay
      :loading="carregando"
      texto="Carregando presenças registradas..."
    >
      <DataTable
        :value="presencas"
        paginator
        :rows="10"
        rowHover
        sortField="nomeAluno"
        :sortOrder="1"
        dataKey="id"
        responsiveLayout="scroll"
        emptyMessage="Nenhuma presença registrada para esta aula."
      >
        <Column field="nomeAluno" header="Aluno" sortable />

        <Column header="Presença" style="width: 120px">
          <template #body="{ data }">
            <Tag
              :value="data.presente ? 'Presente' : 'Ausente'"
              :severity="severityPresenca(data.presente)"
            />
          </template>
        </Column>

        <Column field="observacao" header="Observação" />

        <Column header="Registrado em" style="width: 180px">
          <template #body="{ data }">
            {{ formatarDataHora(data.criadoEm) }}
          </template>
        </Column>
      </DataTable>
    </LoadingOverlay>
  </div>
</template>
