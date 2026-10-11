<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Tag from "primevue/tag";
import Button from "primevue/button";
import CartaoItem from "../../components/ui/CartaoItem.vue";
import { usarTelaCompacta } from "../../aplicacao/composables/usarTelaCompacta";
import {
  listarMinhasTurmas,
  type MinhaTurmaVM,
} from "../../aplicacao/servicos/meusDadosServico";

// RF5 / CSU06. Etapa 6.4: no celular, cada turma é um cartão com a ação principal
// nomeada — "Abrir turma" (Página da Turma, RF20) para quem tem atribuição e
// "Minha frequência" para o aluno; no desktop, a tabela de sempre.
const router = useRouter();
const compacta = usarTelaCompacta();
const turmas = ref<MinhaTurmaVM[]>([]);
const carregando = ref(false);

const turmasOrdenadas = computed(() =>
  [...turmas.value].sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR")),
);

function severityVinculo(vinculo: string) {
  const v = (vinculo || "").toLowerCase();
  if (v === "professor") return "success";
  if (v === "auxiliar") return "info";
  return "secondary";
}

function ehAluno(t: MinhaTurmaVM) {
  return (t.vinculo || "").toLowerCase() === "aluno";
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

// Página da Turma (RF20); sem aba na URL, abre Aulas.
function abrirTurma(t: MinhaTurmaVM) {
  router.push(`/departamentos/${t.departamentoId}`);
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

    <!-- Celular: cartões (Etapa 6.4). -->
    <div v-if="compacta" class="lista-cartoes" data-testid="minhas-turmas-cartoes">
      <CartaoItem
        v-for="t in turmasOrdenadas"
        :key="t.departamentoId"
        :titulo="t.nome"
        data-testid="minhas-turmas-cartao"
      >
        <template #chips>
          <Tag :value="t.vinculo" :severity="severityVinculo(t.vinculo)" />
          <Tag v-if="!t.ativo" value="Inativa" severity="danger" />
        </template>
        <template #meta>
          {{ t.tipo || "EBD" }}
          <template v-if="t.responsavel"> · Responsável: {{ t.responsavel }}</template>
        </template>
        <template #acoes>
          <Button
            v-if="!ehAluno(t)"
            label="Abrir turma"
            icon="pi pi-arrow-right"
            class="acao-principal"
            data-testid="minhas-turmas-abrir"
            @click="abrirTurma(t)"
          />
          <Button
            label="Minha frequência"
            icon="pi pi-chart-line"
            :class="ehAluno(t) ? 'acao-principal' : undefined"
            :severity="ehAluno(t) ? undefined : 'secondary'"
            :outlined="!ehAluno(t)"
            data-testid="minhas-turmas-frequencia"
            @click="verFrequencia(t)"
          />
        </template>
      </CartaoItem>

      <p v-if="!carregando && turmasOrdenadas.length === 0" class="lista-vazia">
        Você ainda não possui vínculo com turmas.
      </p>
    </div>

    <!-- Desktop: tabela. -->
    <DataTable v-else :value="turmas" :loading="carregando" stripedRows>
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
              data-testid="minhas-turmas-frequencia"
              @click="verFrequencia(data)"
            />
            <Button
              v-if="!ehAluno(data)"
              label="Abrir turma"
              size="small"
              data-testid="minhas-turmas-abrir"
              @click="abrirTurma(data)"
            />
          </div>
        </template>
      </Column>
      <template #empty>Você ainda não possui vínculo com turmas.</template>
    </DataTable>
  </div>
</template>
