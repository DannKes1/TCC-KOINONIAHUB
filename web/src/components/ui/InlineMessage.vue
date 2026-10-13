<script setup lang="ts">
import { computed } from "vue";

type Severidade = "erro" | "aviso" | "info" | "sucesso";

const props = defineProps<{
  texto?: string;
  tipo?: Severidade;
}>();

// Computado (Etapa 6.4-B): a mesma instância pode alternar entre erro e aviso
// quando a tela mostra uma mensagem por vez.
const cor = computed(() => {
  switch (props.tipo) {
    case "sucesso":
      return "#0f6a3a";
    case "aviso":
      return "#8a5b00";
    case "info":
      return "#1d4ed8";
    default:
      return "#b00020";
  }
});
</script>

<template>
  <div v-if="texto" :style="{ color: cor, fontSize: '0.9rem' }">
    {{ texto }}
  </div>
</template>
