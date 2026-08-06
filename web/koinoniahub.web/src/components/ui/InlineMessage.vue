<script setup lang="ts">
type Severidade = "erro" | "aviso" | "info" | "sucesso";

const props = defineProps<{
  texto?: string;
  tipo?: Severidade;
}>();

const cor = (() => {
  switch (props.tipo) {
    case "sucesso": return "#0f6a3a";
    case "aviso": return "#8a5b00";
    case "info": return "#1d4ed8";
    default: return "#b00020";
  }
})();
</script>

<template>
  <div v-if="texto" :style="{ color: cor, fontSize: '0.9rem' }">
    {{ texto }}
  </div>
</template>