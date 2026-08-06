<script setup lang="ts">
defineProps<{ texto?: string }>();
</script>

<template>
  <div style="opacity:.7; font-size:.95rem; padding: 8px 0;">
    <i class="pi pi-spin pi-spinner" style="margin-right:8px;"></i>
    {{ texto ?? "Carregando..." }}
  </div>
</template>