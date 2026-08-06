<script setup lang="ts">
import Toast from "primevue/toast";
import ConfirmDialog from "primevue/confirmdialog";
import ToastBridge from "./components/ui/ToastBridge.vue";
</script>

<template>
  <Toast />
  <ConfirmDialog />
  <ToastBridge />
  <RouterView />
</template>
