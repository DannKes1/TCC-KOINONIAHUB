<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute } from "vue-router";

// UI base
import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";

// Composable
import { useAsync } from "../../../aplicacao/composables/useAsync";

// Notificações padronizadas
import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";

// PrimeVue
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import InputNumber from "primevue/inputnumber";
import Checkbox from "primevue/checkbox";
import InputText from "primevue/inputtext";

// PrimeVue services
import { useConfirm } from "primevue/useconfirm";

// Serviços
import {
  obterAula,
  consolidarAula,
} from "../../../aplicacao/servicos/aulasServico";
import {
  listarChamadaCompleta,
  registrarChamada,
} from "../../../aplicacao/servicos/chamadasServico";

// Tipos
import type {
  AulaVM,
  ItemChamadaCompletaVM,
} from "../../../aplicacao/modelos/dtos";

type LinhaChamada = ItemChamadaCompletaVM;

const route = useRoute();
const confirm = useConfirm();

const { carregando, erro, run } = useAsync();

const aulaId = computed(() => Number(route.params.aulaId));

const aula = ref<AulaVM | null>(null);
const linhas = ref<LinhaChamada[]>([]);

const consolidada = computed(() => Boolean(aula.value?.consolidada));

function formatarData(iso: string) {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString();
}

async function carregarTudo() {
  await run(async () => {
    if (!aulaId.value) throw new Error("Aula inválida.");

    const [a, itens] = await Promise.all([
      obterAula(aulaId.value),
      listarChamadaCompleta(aulaId.value),
    ]);

    aula.value = a;

    linhas.value = (itens ?? []).map((x: ItemChamadaCompletaVM) => ({
      alunoDepartamentoId: x.alunoDepartamentoId,
      pessoaId: x.pessoaId,
      nomeAluno: x.nomeAluno,
      presente: Boolean(x.presente),
      observacao: x.observacao ?? null,
    }));
  }, "Não foi possível carregar a chamada.");
}

function marcarTodosPresentes() {
  linhas.value = linhas.value.map((l) => ({ ...l, presente: true }));
}

const visitantes = ref<number>(0);
watch(
  () => aula.value,
  (a: any) => {
    visitantes.value = Number(
      a?.quantidadeVisitantes ?? a?.QuantidadeVisitantes ?? 0,
    );
  },
  { immediate: true },
);

function limparPresencas() {
  linhas.value = linhas.value.map((l) => ({ ...l, presente: false }));
}

async function salvar() {
  if (consolidada.value) return;

  await run(async () => {
    await registrarChamada(aulaId.value, {
      QuantidadeVisitantes: Number(visitantes.value ?? 0),
      Itens: linhas.value.map((l) => ({
        AlunoDepartamentoId: l.alunoDepartamentoId,
        Presente: Boolean(l.presente),
        Observacao: l.observacao?.trim() ? l.observacao.trim() : null,
      })),
    });

    toastSuccess("Chamada salva com sucesso.", "Salvo");

    const itensAtualizados = await listarChamadaCompleta(aulaId.value);
    linhas.value = itensAtualizados.map((x: ItemChamadaCompletaVM) => ({
      alunoDepartamentoId: x.alunoDepartamentoId,
      pessoaId: x.pessoaId,
      nomeAluno: x.nomeAluno,
      presente: Boolean(x.presente),
      observacao: x.observacao ?? null,
    }));
  }, "Não foi possível salvar a chamada.");
}

function confirmarConsolidar() {
  if (consolidada.value) return;

  confirm.require({
    header: "Consolidar aula",
    message:
      "Ao consolidar, a chamada não poderá mais ser alterada. Deseja continuar?",
    icon: "pi pi-exclamation-triangle",
    acceptLabel: "Consolidar",
    rejectLabel: "Cancelar",
    acceptClass: "p-button-danger",
    accept: async () => {
      await run(async () => {
        await consolidarAula(aulaId.value);

        toastSuccess("Aula consolidada com sucesso.", "Consolidada");

        aula.value = await obterAula(aulaId.value);
      }, "Não foi possível consolidar a aula.");
    },
  });
}

onMounted(carregarTudo);
</script>

<template>
  <div class="page-container">
    <PageHeader
      :titulo="
        aula
          ? `Chamada — ${formatarData(aula.data)} (${aula.nomeMateria})`
          : 'Chamada'
      "
      :subtitulo="
        aula
          ? `Professor: ${aula.nomeProfessor} • Consolidada: ${aula.consolidada ? 'Sim' : 'Não'}`
          : 'Registro de presença'
      "
      voltarPara="/departamentos"
      voltarLabel="Turmas EBD"
    >
      <template #acoes>
        <Button
          label="Recarregar"
          icon="pi pi-refresh"
          severity="secondary"
          :loading="carregando"
          @click="carregarTudo"
        />
        <Button
          label="Todos presentes"
          icon="pi pi-check-circle"
          severity="info"
          v-tooltip.top="'Marca todos os alunos como presentes'"
          :disabled="carregando || consolidada"
          @click="marcarTodosPresentes"
        />
        <Button
          label="Limpar"
          icon="pi pi-times-circle"
          severity="secondary"
          v-tooltip.top="'Desmarca todas as presenças'"
          :disabled="carregando || consolidada"
          @click="limparPresencas"
        />
        <Button
          label="Salvar"
          icon="pi pi-save"
          :loading="carregando"
          :disabled="consolidada"
          @click="salvar"
        />
        <Button
          label="Consolidar"
          icon="pi pi-lock"
          severity="danger"
          v-tooltip.top="'Encerra a chamada (impede alterações futuras)'"
          :disabled="carregando || consolidada"
          @click="confirmarConsolidar"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <InlineMessage
      v-if="consolidada"
      texto="Esta aula está consolidada. A chamada está em modo somente leitura."
      tipo="aviso"
    />

    <LoadingOverlay :loading="carregando" texto="Carregando chamada...">
      <div
        style="
          display: flex;
          align-items: center;
          gap: 0.75rem;
          background: #f8faf9;
          border: 1px solid #e2e8e5;
          border-radius: 8px;
          padding: 0.75rem 1rem;
          margin-bottom: 1rem;
        "
      >
        <i class="pi pi-users" style="font-size: 1.3rem; color: #234f32"></i>
        <div style="flex: 1">
          <div style="font-weight: 600">Visitantes avulsos</div>
          <div style="font-size: 0.85rem; color: #6b7280">
            Pessoas sem matrícula que participaram desta aula — a contagem é
            salva junto com a chamada.
          </div>
        </div>
        <InputNumber
          v-model="visitantes"
          inputId="qtd-visitantes"
          :min="0"
          :max="999"
          showButtons
          :disabled="carregando || consolidada"
          :inputStyle="{ width: '5rem' }"
        />
      </div>

      <DataTable
        :value="linhas"
        paginator
        :rows="10"
        rowHover
        sortField="nomeAluno"
        :sortOrder="1"
        dataKey="alunoDepartamentoId"
        responsiveLayout="scroll"
      >
        <Column field="nomeAluno" header="Aluno" sortable />

        <Column header="Presente" style="width: 140px">
          <template #body="{ data }">
            <Checkbox
              v-model="data.presente"
              :binary="true"
              :disabled="consolidada || carregando"
            />
          </template>
        </Column>

        <Column header="Observação" style="min-width: 260px">
          <template #body="{ data }">
            <InputText
              v-model="data.observacao"
              placeholder="Opcional"
              :disabled="consolidada || carregando"
              style="width: 100%"
            />
          </template>
        </Column>
      </DataTable>
    </LoadingOverlay>
  </div>
</template>
