<script setup lang="ts">
import { useRouter } from "vue-router";

const props = defineProps<{
  titulo: string;
  subtitulo?: string;
  voltarPara?: string;
  voltarLabel?: string;
}>();

const router = useRouter();

function voltar() {
  if (props.voltarPara) {
    router.push(props.voltarPara);
  }
}
</script>

<template>
  <div class="page-header-ipb">
    <div>
      <a
        v-if="voltarPara"
        href="#"
        class="page-header-voltar"
        @click.prevent="voltar"
      >
        <i class="pi pi-arrow-left" style="font-size: 11px"></i>
        {{ voltarLabel || "Voltar" }}
      </a>
      <h2 class="page-header-titulo">{{ titulo }}</h2>
      <p v-if="subtitulo" class="page-header-subtitulo">{{ subtitulo }}</p>
    </div>

    <div class="page-header-acoes">
      <slot name="acoes" />
    </div>
  </div>
</template>

<style scoped>
.page-header-ipb {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.page-header-voltar {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--ipb-cinza-claro, #7a7a7a);
  text-decoration: none;
  margin-bottom: 4px;
  transition: color 0.15s ease;
}

.page-header-voltar:hover {
  color: var(--ipb-verde, #234f32);
}

.page-header-titulo {
  margin: 0;
  font-family: var(--font-display, Georgia);
  font-size: 22px;
  font-weight: 700;
  color: var(--ipb-verde-escuro, #1a3b25);
}

.page-header-subtitulo {
  margin: 4px 0 0;
  font-size: 14px;
  color: var(--ipb-cinza-claro, #7a7a7a);
}

.page-header-acoes {
  display: flex;
  gap: 8px;
  align-items: center;
}
</style>
