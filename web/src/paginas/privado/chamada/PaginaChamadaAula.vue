<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute } from "vue-router";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import TagSituacaoAula from "../../../components/ui/TagSituacaoAula.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";

import {
  toastSuccess,
  toastWarn,
} from "../../../aplicacao/servicos/notificacoes";

import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import InputNumber from "primevue/inputnumber";
import Checkbox from "primevue/checkbox";
import InputText from "primevue/inputtext";
import Tag from "primevue/tag";

import { useConfirm } from "primevue/useconfirm";

import {
  obterAula,
  consolidarAula,
  extrairAlunosSemRegistro,
} from "../../../aplicacao/servicos/aulasServico";
import {
  listarChamadaCompleta,
  registrarChamada,
} from "../../../aplicacao/servicos/chamadasServico";

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

// RNF 32.2 / CSU10 FA1: só aulas Em aberto permitem lançar ou alterar a chamada;
// Consolidada e Não realizada ficam em modo somente leitura.
const somenteLeitura = computed(
  () => aula.value !== null && aula.value.situacao !== "EmAberto",
);

const mensagemSomenteLeitura = computed(() => {
  if (aula.value?.situacao === "Consolidada")
    return "Esta aula está consolidada. A chamada está em modo somente leitura; apenas o Administrador pode reabri-la para correção.";
  if (aula.value?.situacao === "NaoRealizada")
    return "Esta aula foi marcada como Não realizada: não possui registros de presença e não entra nos cálculos de frequência. Apenas o Administrador pode reabri-la.";
  return "";
});

// Alunos apontados pela API no 400 da consolidação (RNFs 32.6/33.3, CSU11 FA1).
const idsSemRegistro = ref<Set<number>>(new Set());

function semRegistro(alunoDepartamentoId: number) {
  return idsSemRegistro.value.has(alunoDepartamentoId);
}

function classeLinha(linha: LinhaChamada) {
  return semRegistro(linha.alunoDepartamentoId) ? "linha-sem-registro" : "";
}

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
  if (somenteLeitura.value) return;

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
    idsSemRegistro.value = new Set();

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

// RF33 / CSU11: a API recusa com 400 e a lista dos alunos sem registro quando a
// chamada está incompleta (FA1); as linhas são destacadas para o professor completar.
function confirmarConsolidar() {
  if (somenteLeitura.value) return;

  confirm.require({
    header: "Consolidar aula",
    message:
      "Ao consolidar, a chamada não poderá mais ser alterada. Deseja continuar?",
    icon: "pi pi-exclamation-triangle",
    acceptLabel: "Consolidar",
    rejectLabel: "Cancelar",
    acceptClass: "p-button-danger",
    accept: async () => {
      try {
        await run(
          async () => {
            await consolidarAula(aulaId.value);
            toastSuccess("Aula consolidada com sucesso.", "Consolidada");
            idsSemRegistro.value = new Set();
            aula.value = await obterAula(aulaId.value);
          },
          "Não foi possível consolidar a aula.",
          { throwOnError: true },
        );
      } catch (e) {
        const alunos = extrairAlunosSemRegistro(e);
        if (alunos.length > 0) {
          idsSemRegistro.value = new Set(
            alunos.map((a) => a.alunoDepartamentoId),
          );
          toastWarn(
            "Marque presente ou ausente para os alunos destacados e salve a chamada antes de consolidar.",
            "Chamada incompleta",
          );
        }
      }
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
        aula ? `Professor: ${aula.nomeProfessor}` : 'Registro de presença'
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
          :disabled="carregando || somenteLeitura"
          @click="marcarTodosPresentes"
        />
        <Button
          label="Limpar"
          icon="pi pi-times-circle"
          severity="secondary"
          v-tooltip.top="'Desmarca todas as presenças'"
          :disabled="carregando || somenteLeitura"
          @click="limparPresencas"
        />
        <Button
          label="Salvar"
          icon="pi pi-save"
          :loading="carregando"
          :disabled="somenteLeitura"
          @click="salvar"
        />
        <Button
          label="Consolidar"
          icon="pi pi-lock"
          severity="danger"
          v-tooltip.top="'Encerra a chamada (impede alterações futuras)'"
          :disabled="carregando || somenteLeitura"
          @click="confirmarConsolidar"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <div
      v-if="aula"
      style="display: flex; align-items: center; gap: 10px; flex-wrap: wrap"
    >
      <span style="font-weight: 600">Situação da aula:</span>
      <TagSituacaoAula
        :situacao="aula.situacao"
        :pendenteFechamento="aula.pendenteFechamento"
      />
    </div>

    <InlineMessage
      v-if="somenteLeitura"
      :texto="mensagemSomenteLeitura"
      tipo="aviso"
    />

    <InlineMessage
      v-if="idsSemRegistro.size > 0"
      :texto="`${idsSemRegistro.size} aluno(s) sem registro de presença ou ausência (destacados na lista). Marque e salve a chamada para poder consolidar.`"
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
          :disabled="carregando || somenteLeitura"
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
        :rowClass="classeLinha"
      >
        <Column field="nomeAluno" header="Aluno" sortable>
          <template #body="{ data }">
            <span style="display: inline-flex; align-items: center; gap: 8px">
              {{ data.nomeAluno }}
              <Tag
                v-if="semRegistro(data.alunoDepartamentoId)"
                value="Sem registro"
                severity="warning"
              />
            </span>
          </template>
        </Column>

        <Column header="Presente" style="width: 140px">
          <template #body="{ data }">
            <Checkbox
              v-model="data.presente"
              :binary="true"
              :disabled="somenteLeitura || carregando"
            />
          </template>
        </Column>

        <Column header="Observação" style="min-width: 260px">
          <template #body="{ data }">
            <InputText
              v-model="data.observacao"
              placeholder="Opcional"
              :disabled="somenteLeitura || carregando"
              style="width: 100%"
            />
          </template>
        </Column>
      </DataTable>
    </LoadingOverlay>
  </div>
</template>

<style scoped>
:deep(tr.linha-sem-registro > td) {
  background: #fff4e5;
}
</style>
