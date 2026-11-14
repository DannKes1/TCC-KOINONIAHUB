<script setup lang="ts">
import { useRouter } from "vue-router";

import Button from "primevue/button";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import Tag from "primevue/tag";

import CartaoItem from "../ui/CartaoItem.vue";
import { usarTelaCompacta } from "../../aplicacao/composables/usarTelaCompacta";
import type { MinhaTurmaVM } from "../../aplicacao/servicos/meusDadosServico";

// Ajuste C2: bloco "Turmas em que sou aluno(a)" do Painel administrativo e do Painel do
// Professor, para a conta que também está matriculada numa turma. O Painel continua o do
// perfil (RF3); o bloco só leva à própria frequência (RF6), como no Painel do usuário: o
// botão fica desabilitado se a turma estiver inativa. Abaixo de 768 px, cartões; no
// desktop, tabela.
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
        <template v-if="!t.ativo" #chips>
          <Tag value="Inativa" severity="secondary" />
        </template>
        <template #acoes>
          <Button
            label="Minha frequência"
            icon="pi pi-chart-line"
            class="acao-principal"
            :disabled="!t.ativo"
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
      <Column header="Turma">
        <template #body="{ data }">
          {{ data.nome }}
          <Tag
            v-if="!data.ativo"
            value="Inativa"
            severity="secondary"
            style="margin-left: 8px"
          />
        </template>
      </Column>
      <Column header="Ação" style="width: 190px">
        <template #body="{ data }">
          <Button
            label="Minha frequência"
            icon="pi pi-chart-line"
            size="small"
            severity="secondary"
            outlined
            :disabled="!data.ativo"
            data-testid="painel-minha-frequencia"
            @click="abrirMinhaFrequencia(data)"
          />
        </template>
      </Column>
    </DataTable>
  </div>
</template>
