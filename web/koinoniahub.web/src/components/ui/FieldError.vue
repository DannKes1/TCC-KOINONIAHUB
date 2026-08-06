<script setup lang="ts">
defineProps<{ texto?: string }>();
</script>

<template>
  <small v-if="texto" style="color:#b00020; display:block; margin-top:4px;">
    {{ texto }}
  </small>
</template>