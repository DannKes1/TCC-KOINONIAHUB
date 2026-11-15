<script setup lang="ts">
import { useRouter } from "vue-router";

import Button from "primevue/button";
import Column from "primevue/column";
import DataTable from "primevue/datatable";

import CartaoItem from "../ui/CartaoItem.vue";
import { usarTelaCompacta } from "../../aplicacao/composables/usarTelaCompacta";
import type { MinhaTurmaVM } from "../../aplicacao/servicos/meusDadosServico";

// Ajuste C2: bloco "Turmas em que sou aluno(a)" do Painel administrativo e do Painel do
// Professor, para a conta que também está matriculada numa turma. O Painel continua o do
// perfil (RF3); o bloco só leva à própria frequência (RF6). Recebe só turmas ativas (o
// filtro fica no Painel). Abaixo de 768 px, cartões; no desktop, tabela.
defineProps<{
  turmas: MinhaTurmaVM[];
}>();

const router = useRouter();
const compacta = usarTelaCompacta();

function abrirMinhaFrequencia(turma: MinhaTurmaVM) {
  void router.push(`/departamentos/${turma.departamentoId}/minha-frequencia`);
}
</script>

<template>
  <div class="card-tabela" data-testid="painel-turmas-como-aluno">
    <div class="card-tabela-titulo">Turmas em que sou aluno(a)</div>

    <div v-if="compacta" class="lista-cartoes">
      <CartaoItem
        v-for="t in turmas"
        :key="t.departamentoId"
        :titulo="t.nome"
        data-testid="painel-turma-como-aluno"
      >
        <template #acoes>
          <Button
            label="Minha frequência"
            icon="pi pi-chart-line"
            class="acao-principal"
            data-testid="painel-minha-frequencia"
            @click="abrirMinhaFrequencia(t)"
          />
        </template>
      </CartaoItem>
    </div>

    <DataTable
      v-else
      :value="turmas"
      dataKey="departamentoId"
      responsiveLayout="scroll"
    >
      <Column field="nome" header="Turma" />
      <Column header="Ação" style="width: 190px">
        <template #body="{ data }">
          <Button
            label="Minha frequência"
            icon="pi pi-chart-line"
            size="small"
            severity="secondary"
            outlined
            data-testid="painel-minha-frequencia"
            @click="abrirMinhaFrequencia(data)"
          />
        </template>
      </Column>
    </DataTable>
  </div>
</template>
