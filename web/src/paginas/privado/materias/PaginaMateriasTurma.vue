<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useRoute } from "vue-router";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";

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
import InputText from "primevue/inputtext";
import InputNumber from "primevue/inputnumber";
import Checkbox from "primevue/checkbox";

import { useConfirm } from "primevue/useconfirm";

import { obterDepartamento } from "../../../aplicacao/servicos/departamentosServico";
import {
  listarMaterias,
  criarMateria,
  atualizarMateria,
} from "../../../aplicacao/servicos/materiasServico";

import type {
  DepartamentoVM,
  MateriaVM,
} from "../../../aplicacao/modelos/dtos";

const route = useRoute();
const confirm = useConfirm();

const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

const departamentoId = computed(() => Number(route.params.departamentoId));

const turma = ref<DepartamentoVM | null>(null);
const materias = ref<MateriaVM[]>([]);

const dialogAberto = ref(false);
const editandoId = ref<number | null>(null);

const form = reactive({
  nome: "",
  ordemExibicao: null as number | null,
  ativo: true,
});

function limparForm() {
  form.nome = "";
  form.ordemExibicao = null;
  form.ativo = true;
  editandoId.value = null;
}

function abrirNovo() {
  clearErrors();
  limparForm();
  dialogAberto.value = true;
}

function abrirEdicao(m: MateriaVM) {
  clearErrors();
  editandoId.value = m.id;
  form.nome = m.nome ?? "";
  form.ordemExibicao = m.ordemExibicao ?? null;
  form.ativo = Boolean(m.ativo);
  dialogAberto.value = true;
}

async function carregarTudo() {
  await run(async () => {
    if (!departamentoId.value) throw new Error("Departamento inválido.");

    turma.value = await obterDepartamento(departamentoId.value);
    materias.value = await listarMaterias(departamentoId.value);
  }, "Não foi possível carregar as matérias.");
}

function validarRapido(): string {
  if (!form.nome.trim()) return "Informe o nome da matéria.";
  return "";
}

async function salvar() {
  const msg = validarRapido();
  if (msg) {
    toastWarn(msg);
    return;
  }

  const payload = {
    Nome: form.nome.trim(),
    Descricao: null,
    ImagemUrl: null,

    OrdemExibicao: form.ordemExibicao ?? 0,
    Ativo: Boolean(form.ativo),
    DepartamentoId: departamentoId.value,
  };

  await run(async () => {
    if (editandoId.value === null) {
      await criarMateria(payload as any);
      toastSuccess("Matéria criada com sucesso.", "Criado");
    } else {
      await atualizarMateria(editandoId.value, payload as any);
      toastSuccess("Matéria atualizada com sucesso.", "Salvo");
    }

    dialogAberto.value = false;
    materias.value = await listarMaterias(departamentoId.value);
  }, "Não foi possível salvar a matéria.");
}

function confirmarAlternarAtivo(m: MateriaVM) {
  const novoAtivo = !m.ativo;

  confirm.require({
    header: novoAtivo ? "Ativar matéria" : "Inativar matéria",
    message: novoAtivo
      ? `Deseja ativar "${m.nome}"?`
      : `Deseja inativar "${m.nome}"?`,
    icon: "pi pi-exclamation-triangle",
    acceptLabel: novoAtivo ? "Ativar" : "Inativar",
    rejectLabel: "Cancelar",
    acceptClass: novoAtivo ? "p-button-success" : "p-button-danger",
    accept: async () => {
      await run(
        async () => {
          await atualizarMateria(m.id, {
            Nome: m.nome,
            Descricao: null,
            ImagemUrl: null,
            OrdemExibicao: m.ordemExibicao ?? null,
            Ativo: novoAtivo,
            DepartamentoId: departamentoId.value,
          } as any);

          toastSuccess(
            novoAtivo ? "Matéria ativada." : "Matéria inativada.",
            "Concluído",
          );
          materias.value = await listarMaterias(departamentoId.value);
        },
        novoAtivo
          ? "Não foi possível ativar a matéria."
          : "Não foi possível inativar a matéria.",
      );
    },
  });
}

onMounted(carregarTudo);
</script>

<template>
  <div class="page-container">
    <PageHeader
      :titulo="turma ? `Matérias — ${turma.nome}` : 'Matérias'"
      subtitulo="Cadastre e organize as matérias desta turma"
      voltarPara="/departamentos"
      voltarLabel="Turmas EBD"
    >
      <template #acoes>
        <Button label="Nova Matéria" icon="pi pi-plus" @click="abrirNovo" />
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

    <LoadingOverlay :loading="carregando" texto="Carregando matérias...">
      <DataTable
        :value="materias"
        paginator
        :rows="10"
        rowHover
        sortField="ordemExibicao"
        :sortOrder="1"
        dataKey="id"
        responsiveLayout="scroll"
      >
        <Column field="nome" header="Matéria" sortable />
        <Column
          field="ordemExibicao"
          header="Ordem"
          style="width: 110px"
          sortable
        />

        <Column header="Ativo" style="width: 110px">
          <template #body="{ data }">
            {{ data.ativo ? "Sim" : "Não" }}
          </template>
        </Column>

        <Column header="Ações" style="width: 220px">
          <template #body="{ data }">
            <div style="display: flex; gap: 8px">
              <Button
                icon="pi pi-pencil"
                severity="secondary"
                v-tooltip.top="'Editar'"
                :disabled="carregando"
                @click="abrirEdicao(data)"
              />
              <Button
                :icon="data.ativo ? 'pi pi-ban' : 'pi pi-check'"
                :severity="data.ativo ? 'danger' : 'success'"
                v-tooltip.top="data.ativo ? 'Desativar' : 'Ativar'"
                :disabled="carregando"
                @click="confirmarAlternarAtivo(data)"
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
      header="Matéria"
      style="width: 560px; max-width: 92vw"
    >
      <div class="page-container">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Nome</label>
          <InputText
            v-model="form.nome"
            placeholder="Ex.: Antigo Testamento, Doutrina..."
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Nome') ||
              firstFieldError(fieldErrors, 'nome')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Ordem de exibição (opcional)</label>
          <InputNumber v-model="form.ordemExibicao" placeholder="Ex.: 1" />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'OrdemExibicao') ||
              firstFieldError(fieldErrors, 'ordemExibicao')
            "
          />
        </div>

        <div style="display: flex; align-items: center; gap: 10px">
          <Checkbox v-model="form.ativo" :binary="true" inputId="matAtivo" />
          <label for="matAtivo">Ativo</label>
        </div>
        <FieldError
          :texto="
            firstFieldError(fieldErrors, 'Ativo') ||
            firstFieldError(fieldErrors, 'ativo')
          "
        />

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
          :label="editandoId === null ? 'Criar' : 'Salvar'"
          icon="pi pi-check"
          :loading="carregando"
          @click="salvar"
        />
      </template>
    </Dialog>
  </div>
</template>
