<script setup lang="ts">
import { onMounted } from "vue";
import { useToast } from "primevue/usetoast";
import { setToastHandler } from "../../aplicacao/servicos/notificacoes";

const toast = useToast();

onMounted(() => {
  setToastHandler((payload) => toast.add(payload));
});
</script>

<template></template>