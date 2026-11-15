<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Tag from "primevue/tag";
import Button from "primevue/button";
import SelectButton from "primevue/selectbutton";
import CartaoItem from "../../components/ui/CartaoItem.vue";
import PageHeader from "../../components/ui/PageHeader.vue";
import { usarTelaCompacta } from "../../aplicacao/composables/usarTelaCompacta";
import {
  listarMinhasTurmas,
  type MinhaTurmaVM,
} from "../../aplicacao/servicos/meusDadosServico";

// RF5 / CSU06. Etapa 6.4: no celular, cada turma é um cartão com a ação principal
// nomeada — "Abrir turma" (Página da Turma, RF20) para quem tem atribuição e
// "Minha frequência" para o aluno; no desktop, a tabela de sempre. "Minha
// frequência" só existe para o vínculo Aluno (RF6: frequência de quem está
// matriculado) — para professor/auxiliar a API responde que não há matrícula.
const router = useRouter();
const compacta = usarTelaCompacta();
const turmas = ref<MinhaTurmaVM[]>([]);
const carregando = ref(false);

// Ajuste C3: filtro de situação no cliente, no padrão de Turmas EBD, começando em Ativas.
// Vale para a tabela e para os cartões; a API continua devolvendo as turmas ativas e as
// inativas.
type FiltroSituacao = "ativas" | "inativas" | "todas";
const filtroStatus = ref<FiltroSituacao>("ativas");
const opcoesSituacao: { label: string; value: FiltroSituacao }[] = [
  { label: "Ativas", value: "ativas" },
  { label: "Inativas", value: "inativas" },
  { label: "Todas", value: "todas" },
];

const turmasFiltradas = computed(() =>
  turmas.value.filter(
    (t) =>
      filtroStatus.value === "todas" ||
      (filtroStatus.value === "ativas" ? t.ativo : !t.ativo),
  ),
);

const turmasOrdenadas = computed(() =>
  [...turmasFiltradas.value].sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR")),
);

// Lista vazia: sem vínculo nenhum, ou sem vínculo na situação escolhida.
const textoVazio = computed(() => {
  if (turmas.value.length === 0) return "Você ainda não possui vínculo com turmas.";
  if (filtroStatus.value === "ativas") return "Você não possui vínculo com turmas ativas.";
  return "Você não possui vínculo com turmas inativas.";
});

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
  void router.push(`/departamentos/${t.departamentoId}/minha-frequencia`);
}

// Página da Turma (RF20); sem aba na URL, abre Aulas.
function abrirTurma(t: MinhaTurmaVM) {
  void router.push(`/departamentos/${t.departamentoId}`);
}

onMounted(carregar);
</script>

<template>
  <div class="page-container">
    <!-- Ajuste D2: o cabeçalho padrão das outras telas, com o Recarregar nas ações. -->
    <PageHeader
      titulo="Minhas Turmas"
      subtitulo="Turmas em que você possui matrícula ou atribuição ativa"
    >
      <template #acoes>
        <Button
          label="Recarregar"
          icon="pi pi-refresh"
          severity="secondary"
          :loading="carregando"
          data-testid="minhas-turmas-recarregar"
          @click="carregar"
        />
      </template>
    </PageHeader>

    <!-- Ajuste D5: "Mostrar:" com o tamanho e a cor dos rótulos de filtro das outras telas. -->
    <div style="display: flex; align-items: center; gap: 12px; flex-wrap: wrap">
      <span id="minhas-turmas-filtro-rotulo">Mostrar:</span>
      <SelectButton
        v-model="filtroStatus"
        ariaLabelledby="minhas-turmas-filtro-rotulo"
        :options="opcoesSituacao"
        optionLabel="label"
        optionValue="value"
        :allowEmpty="false"
        data-testid="minhas-turmas-filtro-situacao"
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
            v-if="ehAluno(t)"
            label="Minha frequência"
            icon="pi pi-chart-line"
            class="acao-principal"
            data-testid="minhas-turmas-frequencia"
            @click="verFrequencia(t)"
          />
        </template>
      </CartaoItem>

      <p v-if="!carregando && turmasOrdenadas.length === 0" class="lista-vazia">
        {{ textoVazio }}
      </p>
    </div>

    <!-- Desktop: tabela. -->
    <DataTable v-else :value="turmasFiltradas" :loading="carregando" stripedRows>
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
          {{ data.responsavel ?? "-" }}
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
              v-if="ehAluno(data)"
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
      <template #empty>{{ textoVazio }}</template>
    </DataTable>
  </div>
</template>
