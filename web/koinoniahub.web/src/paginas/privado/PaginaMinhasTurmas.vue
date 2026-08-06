<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Tag from "primevue/tag";
import Button from "primevue/button";
import {
  listarMinhasTurmas,
  type MinhaTurmaVM,
} from "../../aplicacao/servicos/meusDadosServico";

const router = useRouter();
const turmas = ref<MinhaTurmaVM[]>([]);
const carregando = ref(false);

function severityVinculo(vinculo: string) {
  const v = (vinculo || "").toLowerCase();
  if (v === "professor") return "success";
  if (v === "auxiliar") return "info";
  return "secondary";
}

async function carregar() {
  carregando.value = true;
  try {
    turmas.value = await listarMinhasTurmas();
  } finally {
    carregando.value = false;
  }
}

function verFrequencia(t: MinhaTurmaVM) {
  router.push(`/departamentos/${t.departamentoId}/minha-frequencia`);
}

function abrirTurma(t: MinhaTurmaVM) {
  router.push(`/departamentos/${t.departamentoId}/aulas`);
}

onMounted(carregar);
</script>

<template>
  <div class="page-container">
    <div
      style="
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1rem;
        flex-wrap: wrap;
        gap: 0.75rem;
      "
    >
      <div>
        <h2 style="margin: 0">Minhas Turmas</h2>
        <p style="margin: 0.25rem 0 0; color: #6b7280">
          Turmas em que você possui matrícula ou atribuição ativa
        </p>
      </div>
      <Button
        label="Recarregar"
        icon="pi pi-refresh"
        severity="secondary"
        :loading="carregando"
        @click="carregar"
      />
    </div>

    <DataTable :value="turmas" :loading="carregando" stripedRows>
      <Column field="nome" header="Turma" />
      <Column field="tipo" header="Tipo" style="width: 110px" />
      <Column header="Vínculo" style="width: 140px">
        <template #body="{ data }">
          <Tag
            :value="data.vinculo"
            :severity="severityVinculo(data.vinculo)"
          />
        </template>
      </Column>
      <Column header="Responsável">
        <template #body="{ data }">
          {{ data.responsavel ?? "—" }}
        </template>
      </Column>
      <Column header="Status" style="width: 100px">
        <template #body="{ data }">
          {{ data.ativo ? "Ativa" : "Inativa" }}
        </template>
      </Column>
      <Column header="Ações" style="width: 250px">
        <template #body="{ data }">
          <div style="display: flex; gap: 0.4rem; flex-wrap: wrap">
            <Button
              label="Minha Frequência"
              size="small"
              severity="secondary"
              outlined
              @click="verFrequencia(data)"
            />
            <Button
              v-if="(data.vinculo || '').toLowerCase() !== 'aluno'"
              label="Abrir turma"
              size="small"
              @click="abrirTurma(data)"
            />
          </div>
        </template>
      </Column>
      <template #empty>Você ainda não possui vínculo com turmas.</template>
    </DataTable>
  </div>
</template>
