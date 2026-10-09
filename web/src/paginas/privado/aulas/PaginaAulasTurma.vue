<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";
import TagSituacaoAula from "../../../components/ui/TagSituacaoAula.vue";

import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";
import {
  OPCOES_FILTRO_SITUACAO,
  aulaAtendeFiltro,
  type FiltroSituacaoAula,
} from "../../../aplicacao/dominio/situacaoAula";

import { useAsync } from "../../../aplicacao/composables/useAsync";

import {
  toastSuccess,
  toastWarn,
} from "../../../aplicacao/servicos/notificacoes";

import { firstFieldError } from "../../../aplicacao/servicos/apiError";

import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import Dropdown from "primevue/dropdown";
import InputText from "primevue/inputtext";
import Calendar from "primevue/calendar";

import { useConfirm } from "primevue/useconfirm";

import { obterDepartamento } from "../../../aplicacao/servicos/departamentosServico";
import { listarMaterias } from "../../../aplicacao/servicos/materiasServico";
import { listarAtribuicoesPorDepartamento } from "../../../aplicacao/servicos/atribuicoesServico";
import {
  listarAulasPorDepartamento,
  criarAula,
  consolidarAula,
  marcarAulaNaoRealizada,
  reabrirAula,
  extrairAlunosSemRegistro,
} from "../../../aplicacao/servicos/aulasServico";

import type {
  AulaVM,
  DepartamentoVM,
  MateriaVM,
} from "../../../aplicacao/modelos/dtos";

const route = useRoute();
const router = useRouter();
const confirm = useConfirm();
const autenticacao = usarAutenticacaoStore();

// RNF 33.5: só o Administrador reabre uma aula fechada.
const podeReabrir = computed(() => autenticacao.isAdmin);

const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

const departamentoId = computed(() => Number(route.params.departamentoId));

const turma = ref<DepartamentoVM | null>(null);
const aulas = ref<AulaVM[]>([]);
const busca = ref("");
const filtroSituacao = ref<FiltroSituacaoAula | null>(null);

const totalPendentes = computed(
  () => aulas.value.filter((a) => a.pendenteFechamento).length,
);

/** Busca local por tema, matéria, professor ou data (dd/mm/aaaa) e filtro por situação (RNF 31.2/31.3). */
const aulasFiltradas = computed(() => {
  const termo = busca.value.trim().toLowerCase();

  return aulas.value.filter((a) => {
    if (!aulaAtendeFiltro(a, filtroSituacao.value)) return false;
    if (!termo) return true;

    return (
      String(a.tema ?? "")
        .toLowerCase()
        .includes(termo) ||
      String(a.nomeMateria ?? "")
        .toLowerCase()
        .includes(termo) ||
      String(a.nomeProfessor ?? "")
        .toLowerCase()
        .includes(termo) ||
      formatarData(a.data).includes(termo)
    );
  });
});
const materias = ref<MateriaVM[]>([]);
const professores = ref<{ id: number; nome: string }[]>([]);

const dialogAberto = ref(false);

const form = reactive({
  data: null as Date | null,
  tema: "",
  materiaId: null as number | null,
  professorId: null as number | null,
});

function abrirChamada(aula: AulaVM) {
  router.push(`/aulas/${aula.id}/chamada`);
}

function abrirPresencas(aula: AulaVM) {
  router.push(`/aulas/${aula.id}/presencas`);
}

function limparForm() {
  form.data = null;
  form.tema = "";
  form.materiaId = null;
  form.professorId = null;
}

function abrirNovo() {
  clearErrors();
  limparForm();
  dialogAberto.value = true;
}

async function carregarTudo() {
  await run(async () => {
    if (!departamentoId.value) throw new Error("Departamento inválido.");

    turma.value = await obterDepartamento(departamentoId.value);

    const [listaAulas, listaMaterias, listaAtribuicoes] = await Promise.all([
      listarAulasPorDepartamento(departamentoId.value),
      listarMaterias(departamentoId.value),
      listarAtribuicoesPorDepartamento(departamentoId.value, {
        funcao: "Professor",
        ativo: true,
      }),
    ]);

    aulas.value = listaAulas;
    materias.value = listaMaterias;
    professores.value = listaAtribuicoes.map((a) => ({
      id: a.pessoaId,
      nome: a.pessoaNome,
    }));
  }, "Não foi possível carregar as aulas.");
}

function validarRapido(): string {
  if (!form.data) return "Informe a data da aula.";
  if (!form.materiaId) return "Selecione a matéria.";
  if (!form.professorId) return "Selecione o professor.";
  return "";
}

async function salvar() {
  const msg = validarRapido();
  if (msg) {
    toastWarn(msg);
    return;
  }

  await run(async () => {
    const dataIso = (form.data as Date).toISOString();

    await criarAula({
      Data: dataIso,
      Tema: form.tema?.trim() || null,
      MateriaId: form.materiaId as number,
      ProfessorId: form.professorId as number,
    });

    toastSuccess("Aula criada com sucesso.", "Criada");

    dialogAberto.value = false;
    aulas.value = await listarAulasPorDepartamento(departamentoId.value);
  }, "Não foi possível criar a aula.");
}

async function recarregarAulas() {
  aulas.value = await listarAulasPorDepartamento(departamentoId.value);
}

// RF33 / CSU11: consolidar exige aula Em aberto e chamada completa. Quando a API
// devolve 400 com alunosSemRegistro[], a mensagem lista os alunos (FA1).
function confirmarConsolidar(aula: AulaVM) {
  if (aula.situacao !== "EmAberto") return;

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
            await consolidarAula(aula.id);
            toastSuccess("Aula consolidada com sucesso.", "Consolidada");
            await recarregarAulas();
          },
          "Não foi possível consolidar a aula.",
          { throwOnError: true },
        );
      } catch (e) {
        const semRegistro = extrairAlunosSemRegistro(e);
        if (semRegistro.length > 0) {
          toastWarn(
            `Complete a chamada antes de consolidar. Sem registro: ${semRegistro
              .map((a) => a.nomeAluno)
              .join(", ")}.`,
            "Chamada incompleta",
          );
        }
      }
    },
  });
}

// RF33 / CSU11 FA2: só Em aberto e sem nenhum registro de presença.
function confirmarNaoRealizada(aula: AulaVM) {
  if (aula.situacao !== "EmAberto") return;

  confirm.require({
    header: "Marcar como Não realizada",
    message:
      "A aula será encerrada sem atribuir presença ou falta aos alunos e não entrará nos cálculos de frequência. Deseja continuar?",
    icon: "pi pi-exclamation-triangle",
    acceptLabel: "Marcar como Não realizada",
    rejectLabel: "Cancelar",
    acceptClass: "p-button-danger",
    accept: async () => {
      await run(async () => {
        await marcarAulaNaoRealizada(aula.id);
        toastSuccess("Aula marcada como Não realizada.", "Não realizada");
        await recarregarAulas();
      }, "Não foi possível marcar a aula como Não realizada.");
    },
  });
}

// RF33 / CSU11 FA3 (RNFs 33.5/33.6): só Admin; Consolidada ou Não realizada
// volta para Em aberto, preservando os registros de presença.
function confirmarReabrir(aula: AulaVM) {
  if (aula.situacao === "EmAberto" || !podeReabrir.value) return;

  confirm.require({
    header: "Reabrir aula",
    message:
      aula.situacao === "Consolidada"
        ? "A aula voltará para Em aberto e os registros de presença serão preservados para correção e nova consolidação. Deseja continuar?"
        : "A aula voltará para Em aberto e poderá receber chamada. Deseja continuar?",
    icon: "pi pi-exclamation-triangle",
    acceptLabel: "Reabrir",
    rejectLabel: "Cancelar",
    acceptClass: "p-button-warning",
    accept: async () => {
      await run(async () => {
        await reabrirAula(aula.id);
        toastSuccess(
          "Aula reaberta. A situação voltou para Em aberto.",
          "Reaberta",
        );
        await recarregarAulas();
      }, "Não foi possível reabrir a aula.");
    },
  });
}

function formatarData(iso: string) {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString();
}

onMounted(carregarTudo);
</script>

<template>
  <div class="page-container">
    <PageHeader
      :titulo="turma ? `Aulas — ${turma.nome}` : 'Aulas'"
      subtitulo="Cadastre e gerencie as aulas desta turma"
      voltarPara="/departamentos"
      voltarLabel="Turmas EBD"
    >
      <template #acoes>
        <Button label="Nova Aula" icon="pi pi-plus" @click="abrirNovo" />
        <Button
          label="Recarregar"
          icon="pi pi-refresh"
          severity="secondary"
          :loading="carregando"
          @click="carregarTudo"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <InlineMessage
      v-if="totalPendentes > 0"
      :texto="`${totalPendentes} aula(s) em aberto com data já ocorrida, pendente(s) de fechamento: lance a chamada e consolide, ou marque como Não realizada.`"
      tipo="aviso"
    />

    <div style="display: flex; align-items: center; gap: 12px; flex-wrap: wrap">
      <InputText
        v-model="busca"
        placeholder="Buscar por tema, matéria, professor ou data..."
        style="flex: 1 1 240px; min-width: 0"
      />
      <Dropdown
        v-model="filtroSituacao"
        :options="OPCOES_FILTRO_SITUACAO"
        optionLabel="label"
        optionValue="value"
        showClear
        placeholder="Todas as situações"
        style="flex: 1 1 200px; min-width: 0"
      />
    </div>

    <LoadingOverlay :loading="carregando" texto="Carregando aulas.">
      <DataTable
        :value="aulasFiltradas"
        paginator
        :rows="10"
        rowHover
        sortField="data"
        :sortOrder="-1"
        dataKey="id"
        responsiveLayout="scroll"
      >
        <Column header="Data" style="width: 130px" sortable>
          <template #body="{ data }">
            {{ formatarData(data.data) }}
          </template>
        </Column>

        <Column
          field="nomeMateria"
          header="Matéria"
          sortable
          class="col-celular-oculta"
        />
        <Column
          field="nomeProfessor"
          header="Professor"
          sortable
          class="col-celular-oculta"
        />
        <Column field="tema" header="Tema" class="col-celular-oculta" />

        <Column header="Situação" style="width: 220px">
          <template #body="{ data }">
            <TagSituacaoAula
              :situacao="data.situacao"
              :pendenteFechamento="data.pendenteFechamento"
            />
          </template>
        </Column>

        <Column header="Ações" style="width: 260px">
          <template #body="{ data }">
            <div style="display: flex; gap: 8px; flex-wrap: wrap">
              <Button
                icon="pi pi-clipboard"
                severity="info"
                v-tooltip.top="
                  data.situacao === 'EmAberto'
                    ? 'Fazer chamada'
                    : 'Ver chamada (somente leitura)'
                "
                :disabled="carregando"
                @click="abrirChamada(data)"
              />
              <Button
                icon="pi pi-list-check"
                severity="help"
                v-tooltip.top="'Ver presenças registradas'"
                :disabled="carregando"
                @click="abrirPresencas(data)"
              />
              <Button
                v-if="data.situacao === 'EmAberto'"
                icon="pi pi-lock"
                severity="danger"
                v-tooltip.top="'Consolidar chamada (impede alterações futuras)'"
                :disabled="carregando"
                @click="confirmarConsolidar(data)"
              />
              <Button
                v-if="data.situacao === 'EmAberto'"
                icon="pi pi-ban"
                severity="secondary"
                v-tooltip.top="
                  'Marcar como Não realizada (só sem registros de presença)'
                "
                :disabled="carregando"
                @click="confirmarNaoRealizada(data)"
              />
              <Button
                v-if="data.situacao !== 'EmAberto' && podeReabrir"
                icon="pi pi-lock-open"
                severity="warning"
                v-tooltip.top="'Reabrir aula (somente Administrador)'"
                :disabled="carregando"
                @click="confirmarReabrir(data)"
              />
            </div>
          </template>
        </Column>
        <template #empty>
          <div style="padding: 14px; opacity: 0.7">
            Nenhuma aula encontrada para a busca ou o filtro.
          </div>
        </template>
      </DataTable>
    </LoadingOverlay>

    <Dialog
      v-model:visible="dialogAberto"
      modal
      :closable="!carregando"
      :dismissableMask="!carregando"
      header="Nova aula"
      style="width: 600px; max-width: 92vw"
    >
      <div class="page-container">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Data</label>
          <Calendar v-model="form.data" dateFormat="dd/mm/yy" showIcon />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Data') ||
              firstFieldError(fieldErrors, 'data')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Matéria</label>
          <Dropdown
            v-model="form.materiaId"
            :options="materias"
            optionLabel="nome"
            optionValue="id"
            filter
            placeholder="Selecione a matéria."
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'MateriaId') ||
              firstFieldError(fieldErrors, 'materiaId')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Professor</label>
          <Dropdown
            v-model="form.professorId"
            :options="professores"
            optionLabel="nome"
            optionValue="id"
            filter
            placeholder="Selecione o professor."
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'ProfessorId') ||
              firstFieldError(fieldErrors, 'professorId')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Tema (opcional)</label>
          <InputText v-model="form.tema" placeholder="Ex.: Fé, Salvação." />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Tema') ||
              firstFieldError(fieldErrors, 'tema')
            "
          />
        </div>

        <FieldError
          :texto="
            firstFieldError(fieldErrors, 'DepartamentoId') ||
            firstFieldError(fieldErrors, 'departamentoId')
          "
        />
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="carregando"
          @click="dialogAberto = false"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="carregando"
          @click="salvar"
        />
      </template>
    </Dialog>
  </div>
</template>
