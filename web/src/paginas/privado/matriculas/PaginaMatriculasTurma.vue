<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useRoute } from "vue-router";


import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";


import { useAsync } from "../../../aplicacao/composables/useAsync";


import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import Dropdown from "primevue/dropdown";
import InputText from "primevue/inputtext";
import Tag from "primevue/tag";


import { useConfirm } from "primevue/useconfirm";


import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";


import { obterDepartamento } from "../../../aplicacao/servicos/departamentosServico";
import {
  listarPessoasDisponiveisDaTurma,
  listarAlunosDaTurma,
  matricular,
  removerMatricula,
} from "../../../aplicacao/servicos/matriculasServico";
import { listarParentescosDaPessoa } from "../../../aplicacao/servicos/parentescosServico";


import type {
  DepartamentoVM,
  PessoaVM,
  AlunoDaClasseVM,
  ParentescoVM,
} from "../../../aplicacao/modelos/dtos";

const route = useRoute();
const confirm = useConfirm();

const { carregando, erro, run, clearErrors } = useAsync();

const departamentoId = computed(() => Number(route.params.departamentoId));

const turma = ref<DepartamentoVM | null>(null);
const alunos = ref<AlunoDaClasseVM[]>([]);
const pessoas = ref<PessoaVM[]>([]);

const dialogAberto = ref(false);

const form = reactive({
  pessoaId: null as number | null,
  observacao: "",
});


const dialogResponsaveisAberto = ref(false);
const alunoSelecionado = ref<AlunoDaClasseVM | null>(null);
const responsaveisDoAluno = ref<ParentescoVM[]>([]);
const carregandoResponsaveis = ref(false);
const erroResponsaveis = ref("");

function abrirNovo() {
  clearErrors();
  form.pessoaId = null;
  form.observacao = "";
  dialogAberto.value = true;
}

async function carregarTudo() {
  await run(async () => {
    if (!departamentoId.value) throw new Error("Departamento inválido.");

    turma.value = await obterDepartamento(departamentoId.value);

    const [listaPessoas, listaAlunos] = await Promise.all([
      listarPessoasDisponiveisDaTurma(departamentoId.value),
      listarAlunosDaTurma(departamentoId.value),
    ]);

    pessoas.value = listaPessoas;
    alunos.value = listaAlunos;
  }, "Não foi possível carregar as matrículas.");
}

function validarForm(): string {
  if (!form.pessoaId) return "Selecione uma pessoa para matricular.";
  return "";
}

async function salvar() {
  const msg = validarForm();
  if (msg) {
    erro.value = msg;
    return;
  }

  await run(async () => {
    await matricular(departamentoId.value, {
      PessoaId: form.pessoaId as number,
      Observacao: form.observacao?.trim() || null,
    });

    toastSuccess("Aluno matriculado com sucesso.", "Matriculado");

    dialogAberto.value = false;
    alunos.value = await listarAlunosDaTurma(departamentoId.value);
  }, "Não foi possível matricular o aluno.");
}

function confirmarRemover(aluno: AlunoDaClasseVM) {
  if (!aluno.matriculaAtiva) return;

  confirm.require({
    header: "Remover matrícula",
    message: `Deseja remover a matrícula de "${aluno.nome}" desta turma?`,
    icon: "pi pi-exclamation-triangle",
    acceptLabel: "Remover",
    rejectLabel: "Cancelar",
    acceptClass: "p-button-danger",
    accept: async () => {
      await run(async () => {
        await removerMatricula(departamentoId.value, aluno.matriculaId);

        toastSuccess("Matrícula removida com sucesso.", "Removido");

        alunos.value = await listarAlunosDaTurma(departamentoId.value);
      }, "Não foi possível remover a matrícula.");
    },
  });
}

// ── Responsáveis (parentescos) ──
async function abrirResponsaveis(aluno: AlunoDaClasseVM) {
  alunoSelecionado.value = aluno;
  responsaveisDoAluno.value = [];
  erroResponsaveis.value = "";
  carregandoResponsaveis.value = true;
  dialogResponsaveisAberto.value = true;

  try {
    responsaveisDoAluno.value = await listarParentescosDaPessoa(aluno.pessoaId);
  } catch (e: any) {
    erroResponsaveis.value =
      e?.response?.data?.mensagem ||
      "Não foi possível carregar os responsáveis.";
  } finally {
    carregandoResponsaveis.value = false;
  }
}

function severityRelacionamento(tipo: string) {
  const t = (tipo || "").toLowerCase();
  if (t.includes("pai") || t.includes("mãe") || t.includes("mae"))
    return "info";
  if (t.includes("responsável") || t.includes("responsavel")) return "warning";
  return "secondary";
}

function temAlgumContato(p: ParentescoVM) {
  return Boolean(p.parenteCelular) || Boolean(p.parenteTelefone);
}

function linkTelefone(numero: string | null) {
  if (!numero) return "#";
  const limpo = numero.replace(/\D/g, "");
  return `tel:${limpo}`;
}

onMounted(carregarTudo);
</script>

<template>
  <div class="page-container">
    <PageHeader
      :titulo="turma ? `Matrículas — ${turma.nome}` : 'Matrículas'"
      subtitulo="Gerencie os alunos matriculados nesta turma"
      voltarPara="/departamentos"
      voltarLabel="Turmas EBD"
    >
      <template #acoes>
        <Button label="Matricular" icon="pi pi-plus" @click="abrirNovo" />
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

    <LoadingOverlay :loading="carregando" texto="Carregando matrículas...">
      <DataTable
        :value="alunos"
        paginator
        :rows="10"
        rowHover
        sortField="nome"
        :sortOrder="1"
        dataKey="matriculaId"
        responsiveLayout="scroll"
      >
        <Column field="nome" header="Aluno" sortable />
        <Column field="statusPessoa" header="Status da Pessoa" />

        <Column header="Matrícula ativa" style="width: 140px">
          <template #body="{ data }">
            {{ data.matriculaAtiva ? "Sim" : "Não" }}
          </template>
        </Column>

        <Column header="Ações" style="width: 200px">
          <template #body="{ data }">
            <div style="display: flex; gap: 8px">
              <Button
                icon="pi pi-id-card"
                severity="info"
                :disabled="carregando"
                v-tooltip.top="'Ver responsáveis'"
                @click="abrirResponsaveis(data)"
              />
              <Button
                v-if="data.matriculaAtiva"
                icon="pi pi-trash"
                severity="danger"
                :disabled="carregando"
                v-tooltip.top="'Remover matrícula'"
                @click="confirmarRemover(data)"
              />
            </div>
          </template>
        </Column>
      </DataTable>
    </LoadingOverlay>

   
    <Dialog
      v-model:visible="dialogAberto"
      modal
      :closable="!carregando"
      :dismissableMask="!carregando"
      header="Matricular aluno"
      style="width: 560px; max-width: 92vw"
    >
      <div class="page-container">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Pessoa</label>
          <Dropdown
            v-model="form.pessoaId"
            :options="pessoas"
            optionLabel="nome"
            optionValue="id"
            filter
            placeholder="Selecione uma pessoa..."
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Observação (opcional)</label>
          <InputText
            v-model="form.observacao"
            placeholder="Ex.: Transferido, visitante, etc."
          />
        </div>
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="carregando"
          @click="dialogAberto = false"
        />
        <Button
          label="Matricular"
          icon="pi pi-check"
          :loading="carregando"
          @click="salvar"
        />
      </template>
    </Dialog>

  
    <Dialog
      v-model:visible="dialogResponsaveisAberto"
      modal
      :closable="true"
      :dismissableMask="true"
      :header="
        alunoSelecionado
          ? `Responsáveis de ${alunoSelecionado.nome}`
          : 'Responsáveis'
      "
      style="width: 640px; max-width: 96vw"
    >
      <InlineMessage :texto="erroResponsaveis" tipo="erro" />

      <LoadingOverlay
        :loading="carregandoResponsaveis"
        texto="Carregando responsáveis..."
      >
        <div
          v-if="
            !carregandoResponsaveis &&
            !erroResponsaveis &&
            responsaveisDoAluno.length === 0
          "
          style="padding: 12px 0; opacity: 0.75"
        >
          Esta pessoa não possui responsáveis cadastrados. Para vincular um
          responsável, acesse o cadastro de pessoas.
        </div>

        <div
          v-if="responsaveisDoAluno.length > 0"
          style="display: flex; flex-direction: column; gap: 10px"
        >
          <div
            v-for="r in responsaveisDoAluno"
            :key="r.id"
            style="
              display: flex;
              flex-direction: column;
              gap: 6px;
              padding: 12px;
              border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
              border-radius: 8px;
            "
          >
            <div
              style="
                display: flex;
                justify-content: space-between;
                align-items: center;
                gap: 8px;
                flex-wrap: wrap;
              "
            >
              <strong style="font-size: 15px">{{ r.parenteNome }}</strong>
              <Tag
                :value="r.tipoRelacionamento"
                :severity="severityRelacionamento(r.tipoRelacionamento)"
              />
            </div>

            <div
              v-if="temAlgumContato(r)"
              style="display: flex; gap: 12px; flex-wrap: wrap; font-size: 14px"
            >
              <a
                v-if="r.parenteCelular"
                :href="linkTelefone(r.parenteCelular)"
                style="
                  display: inline-flex;
                  align-items: center;
                  gap: 6px;
                  text-decoration: none;
                "
              >
                <i class="pi pi-mobile"></i>
                {{ r.parenteCelular }}
              </a>

              <a
                v-if="r.parenteTelefone"
                :href="linkTelefone(r.parenteTelefone)"
                style="
                  display: inline-flex;
                  align-items: center;
                  gap: 6px;
                  text-decoration: none;
                "
              >
                <i class="pi pi-phone"></i>
                {{ r.parenteTelefone }}
              </a>
            </div>

            <div v-else style="opacity: 0.65; font-size: 13px">
              <i class="pi pi-info-circle"></i>
              Sem telefone/celular cadastrado.
            </div>
          </div>
        </div>
      </LoadingOverlay>

      <template #footer>
        <Button
          label="Fechar"
          severity="secondary"
          @click="dialogResponsaveisAberto = false"
        />
      </template>
    </Dialog>
  </div>
</template>
