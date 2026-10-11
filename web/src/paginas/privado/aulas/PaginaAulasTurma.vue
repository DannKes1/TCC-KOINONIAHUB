<script setup lang="ts">
import { computed, inject, onMounted, reactive, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";
import TagSituacaoAula from "../../../components/ui/TagSituacaoAula.vue";
import CartaoItem from "../../../components/ui/CartaoItem.vue";
import MenuAcoes from "../../../components/ui/MenuAcoes.vue";

import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";
import {
  OPCOES_FILTRO_SITUACAO,
  aulaAtendeFiltro,
  type FiltroSituacaoAula,
} from "../../../aplicacao/dominio/situacaoAula";

import { useAsync } from "../../../aplicacao/composables/useAsync";
import { usarTelaCompacta } from "../../../aplicacao/composables/usarTelaCompacta";
import { usarMostrarMais } from "../../../aplicacao/composables/usarMostrarMais";

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
import type { MenuItem } from "primevue/menuitem";

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

// Etapa 6.4: abaixo de 768 px a lista vira cartões com a ação principal nomeada
// (Fazer/Ver chamada) e as demais no ⋮; no desktop, a tabela de sempre com a
// mesma dupla botão + ⋮ na coluna de ações.
const compacta = usarTelaCompacta();

// Dentro da Página da Turma (RF20), avisa o cabeçalho para atualizar os contadores.
const recarregarResumoDaTurma = inject<(() => Promise<void>) | null>(
  "turmaRecarregarResumo",
  null,
);

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

// Cartões: da mais recente para a mais antiga, em blocos de dez.
const aulasOrdenadas = computed(() =>
  [...aulasFiltradas.value].sort(
    (a, b) => new Date(b.data).getTime() - new Date(a.data).getTime(),
  ),
);
const {
  visiveis: aulasVisiveis,
  restantes: aulasRestantes,
  mostrarMais,
} = usarMostrarMais(aulasOrdenadas, 10);

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

function rotuloChamada(aula: AulaVM) {
  return aula.situacao === "EmAberto" ? "Fazer chamada" : "Ver chamada";
}

// Ações secundárias do ⋮ (as mesmas nos dois layouts). `visible` segue a situação
// da aula e o perfil (RF33: consolidar/não realizada só Em aberto; reabrir só Admin).
function itensAcoes(aula: AulaVM): MenuItem[] {
  const emAberto = aula.situacao === "EmAberto";
  return [
    {
      label: "Ver presenças registradas",
      icon: "pi pi-list-check",
      command: () => abrirPresencas(aula),
    },
    {
      label: "Consolidar chamada",
      icon: "pi pi-lock",
      visible: emAberto,
      command: () => confirmarConsolidar(aula),
    },
    {
      label: "Marcar como Não realizada",
      icon: "pi pi-ban",
      visible: emAberto,
      command: () => confirmarNaoRealizada(aula),
    },
    {
      label: "Reabrir aula",
      icon: "pi pi-lock-open",
      visible: !emAberto && podeReabrir.value,
      command: () => confirmarReabrir(aula),
    },
  ];
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
    await recarregarAulas();
  }, "Não foi possível criar a aula.");
}

async function recarregarAulas() {
  aulas.value = await listarAulasPorDepartamento(departamentoId.value);
  await recarregarResumoDaTurma?.();
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

// Cartão: dia da semana + data ("dom., 11/10/2026").
function formatarDataLonga(iso: string) {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString("pt-BR", {
    weekday: "short",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
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
        <Button
          label="Nova Aula"
          icon="pi pi-plus"
          data-testid="aulas-nova"
          @click="abrirNovo"
        />
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
      <!-- Celular: cartões (Etapa 6.4). -->
      <div v-if="compacta" class="lista-cartoes" data-testid="aulas-cartoes">
        <CartaoItem
          v-for="aula in aulasVisiveis"
          :key="aula.id"
          :destaque="aula.pendenteFechamento"
          data-testid="aula-cartao"
        >
          <template #titulo>{{ formatarDataLonga(aula.data) }}</template>
          <template #chips>
            <TagSituacaoAula
              :situacao="aula.situacao"
              :pendenteFechamento="aula.pendenteFechamento"
            />
          </template>
          <template #meta>
            {{ aula.nomeMateria }} · {{ aula.nomeProfessor }}
            <template v-if="aula.tema"><br />Tema: {{ aula.tema }}</template>
          </template>
          <template #acoes>
            <Button
              :label="rotuloChamada(aula)"
              icon="pi pi-clipboard"
              class="acao-principal"
              :severity="aula.situacao === 'EmAberto' ? undefined : 'secondary'"
              :outlined="aula.situacao !== 'EmAberto'"
              :disabled="carregando"
              data-testid="aula-chamada"
              @click="abrirChamada(aula)"
            />
            <MenuAcoes
              :itens="itensAcoes(aula)"
              rotulo="Mais ações da aula"
              :desabilitado="carregando"
            />
          </template>
        </CartaoItem>

        <p v-if="aulasVisiveis.length === 0" class="lista-vazia">
          Nenhuma aula encontrada para a busca ou o filtro.
        </p>

        <Button
          v-if="aulasRestantes > 0"
          :label="`Mostrar mais (${aulasRestantes} restantes)`"
          severity="secondary"
          text
          class="mostrar-mais"
          data-testid="aulas-mostrar-mais"
          @click="mostrarMais"
        />
      </div>

      <!-- Desktop: tabela, com a ação principal nomeada e as demais no ⋮. -->
      <DataTable
        v-else
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

        <Column field="nomeMateria" header="Matéria" sortable />
        <Column field="nomeProfessor" header="Professor" sortable />
        <Column field="tema" header="Tema" />

        <Column header="Situação" style="width: 220px">
          <template #body="{ data }">
            <TagSituacaoAula
              :situacao="data.situacao"
              :pendenteFechamento="data.pendenteFechamento"
            />
          </template>
        </Column>

        <Column header="Ações" style="width: 230px">
          <template #body="{ data }">
            <div style="display: flex; gap: 8px; align-items: center">
              <Button
                :label="rotuloChamada(data)"
                icon="pi pi-clipboard"
                size="small"
                :severity="data.situacao === 'EmAberto' ? undefined : 'secondary'"
                :outlined="data.situacao !== 'EmAberto'"
                :disabled="carregando"
                data-testid="aula-chamada"
                @click="abrirChamada(data)"
              />
              <MenuAcoes
                :itens="itensAcoes(data)"
                rotulo="Mais ações da aula"
                :desabilitado="carregando"
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
