<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import TagSituacaoAula from "../../../components/ui/TagSituacaoAula.vue";
import CartaoItem from "../../../components/ui/CartaoItem.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";
import { usarTelaCompacta } from "../../../aplicacao/composables/usarTelaCompacta";

import { obterAula } from "../../../aplicacao/servicos/aulasServico";
import { listarPresencasRegistradas } from "../../../aplicacao/servicos/chamadasServico";

import type { AulaVM, PresencaVM } from "../../../aplicacao/modelos/dtos";

import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import Tag from "primevue/tag";

const route = useRoute();
const router = useRouter();

const { carregando, erro, run } = useAsync();

// Etapa 6.4-B: no celular a lista vira cartões; no desktop, a tabela de sempre.
const compacta = usarTelaCompacta();

const aulaId = Number(route.params.aulaId ?? 0);

const aula = ref<AulaVM | null>(null);
const presencas = ref<PresencaVM[]>([]);

const presencasOrdenadas = computed(() =>
  [...presencas.value].sort((a, b) =>
    a.nomeAluno.localeCompare(b.nomeAluno, "pt-BR"),
  ),
);

// Voltar para a Página da Turma (RF20), aba Aulas, com o nome da turma no rótulo.
const voltarPara = computed(() =>
  aula.value?.departamentoId
    ? `/departamentos/${aula.value.departamentoId}/aulas`
    : "/minhas-turmas",
);
const voltarLabel = computed(
  () => aula.value?.nomeDepartamento || "Minhas Turmas",
);

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
      :titulo="aula ? `Presenças — ${formatarData(aula.data)}` : 'Presenças'"
      :subtitulo="
        aula
          ? `${aula.nomeMateria}${aula.tema ? ` · ${aula.tema}` : ''} — registros já salvos desta aula`
          : 'Consulta apenas dos registros já salvos para esta aula'
      "
      :voltarPara="voltarPara"
      :voltarLabel="voltarLabel"
    >
      <template #acoes>
        <Button
          :label="aula && aula.situacao !== 'EmAberto' ? 'Ver chamada' : 'Abrir chamada'"
          icon="pi pi-check-square"
          severity="secondary"
          data-testid="presencas-abrir-chamada"
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
        grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
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
        grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
        gap: 12px;
      "
    >
      <div><strong>Data:</strong> {{ formatarData(aula.data) }}</div>
      <div><strong>Matéria:</strong> {{ aula.nomeMateria }}</div>
      <div><strong>Professor:</strong> {{ aula.nomeProfessor }}</div>
      <div style="display: flex; align-items: center; gap: 8px; flex-wrap: wrap">
        <strong>Situação:</strong>
        <TagSituacaoAula
          :situacao="aula.situacao"
          :pendenteFechamento="aula.pendenteFechamento"
        />
      </div>
    </div>

    <LoadingOverlay
      :loading="carregando"
      texto="Carregando presenças registradas..."
    >
      <!-- Celular (Etapa 6.4-B): cartões. -->
      <div v-if="compacta" class="lista-cartoes" data-testid="presencas-cartoes">
        <CartaoItem
          v-for="item in presencasOrdenadas"
          :key="item.id"
          :titulo="item.nomeAluno"
          data-testid="presenca-cartao"
        >
          <template #chips>
            <Tag
              :value="item.presente ? 'Presente' : 'Ausente'"
              :severity="severityPresenca(item.presente)"
            />
          </template>
          <template #meta>
            <template v-if="item.observacao">Obs.: {{ item.observacao }}<br /></template>
            Registrado em {{ formatarDataHora(item.criadoEm) }}
          </template>
        </CartaoItem>
        <p v-if="!carregando && presencas.length === 0" class="lista-vazia">
          Nenhuma presença registrada para esta aula.
        </p>
      </div>

      <DataTable
        v-else
        :value="presencas"
        paginator
        :rows="10"
        rowHover
        sortField="nomeAluno"
        :sortOrder="1"
        dataKey="id"
        responsiveLayout="scroll"
      >
        <template #empty>
          <div style="padding: 14px; opacity: 0.7">
            Nenhuma presença registrada para esta aula.
          </div>
        </template>
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
