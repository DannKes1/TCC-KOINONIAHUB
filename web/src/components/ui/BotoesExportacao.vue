<script setup lang="ts">
import { computed } from "vue";
import Button from "primevue/button";

import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";

// RNFs 35.4 / 36.4 / 37.4: exportação em CSV e impressão formatada apenas para os
// perfis administrativos (Admin, Pastor, Superintendente). Professor e Auxiliar
// consultam em tela: para eles o componente não renderiza nada. A regra fica em
// um único lugar e é a que o spec do Plano 7.2 verifica.
const props = withDefaults(
  defineProps<{
    csv?: boolean;
    imprimir?: boolean;
    rotuloCsv?: string;
    desabilitado?: boolean;
  }>(),
  { csv: true, imprimir: false, rotuloCsv: "CSV", desabilitado: false },
);

const emit = defineEmits<{
  (e: "csv"): void;
  (e: "imprimir"): void;
}>();

const autenticacao = usarAutenticacaoStore();
const podeExportar = computed(() => autenticacao.isAdministrativo);
</script>

<template>
  <span
    v-if="podeExportar"
    class="botoes-exportacao nao-imprimir"
    data-testid="botoes-exportacao"
    style="display: inline-flex; gap: 8px; align-items: center"
  >
    <Button
      v-if="props.csv"
      data-testid="botao-csv"
      :label="props.rotuloCsv"
      icon="pi pi-download"
      text
      size="small"
      :disabled="props.desabilitado"
      @click="emit('csv')"
    />
    <Button
      v-if="props.imprimir"
      data-testid="botao-imprimir"
      label="Imprimir"
      icon="pi pi-print"
      outlined
      severity="secondary"
      :disabled="props.desabilitado"
      @click="emit('imprimir')"
    />
  </span>
</template>
