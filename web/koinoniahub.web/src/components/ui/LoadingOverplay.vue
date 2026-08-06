<script setup lang="ts">
defineProps<{
  loading: boolean;
  texto?: string;
}>();
</script>

<template>
  <div style="position: relative">
    <slot />

    <div
      v-if="loading"
      style="
        position: absolute;
        inset: 0;
        background: rgba(0, 0, 0, 0.25);
        display: flex;
        align-items: center;
        justify-content: center;
        border-radius: 12px;
        z-index: 10;
      "
    >
      <div
        style="
          display: flex;
          align-items: center;
          gap: 10px;
          padding: 12px 14px;
          border-radius: 12px;
          background: rgba(0, 0, 0, 0.35);
        "
      >
        <i class="pi pi-spin pi-spinner"></i>
        <span style="opacity: 0.9">{{ texto ?? "Carregando..." }}</span>
      </div>
    </div>
  </div>
</template>
