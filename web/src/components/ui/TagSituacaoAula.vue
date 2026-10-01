<script setup lang="ts">
import { computed } from "vue";
import Tag from "primevue/tag";

import type { SituacaoAula } from "../../aplicacao/modelos/dtos";
import {
  rotuloSituacaoAula,
  severidadeSituacaoAula,
} from "../../aplicacao/dominio/situacaoAula";

// Tag de situação da aula (RF31 / RNF 31.3, Plano 6.2): uma tag por situação
// (Em aberto / Consolidada / Não realizada) e a tag adicional "Pendente" quando
// a aula está Em aberto com data já ocorrida.
const props = defineProps<{
  situacao: SituacaoAula;
  pendenteFechamento?: boolean;
}>();

const rotulo = computed(() => rotuloSituacaoAula(props.situacao));
const severidade = computed(() => severidadeSituacaoAula(props.situacao));
const pendente = computed(
  () => props.situacao === "EmAberto" && Boolean(props.pendenteFechamento),
);
</script>

<template>
  <span
    class="tag-situacao-aula"
    style="display: inline-flex; gap: 6px; flex-wrap: wrap; align-items: center"
  >
    <Tag :value="rotulo" :severity="severidade" data-testid="tag-situacao" />
    <Tag
      v-if="pendente"
      value="Pendente"
      severity="warning"
      icon="pi pi-exclamation-triangle"
      data-testid="tag-pendente"
      v-tooltip.top="
        'Aula em aberto com data já ocorrida: pendente de fechamento.'
      "
    />
  </span>
</template>
