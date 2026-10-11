<script setup lang="ts">
// Etapa 6.4 — item de lista no celular: título, chips de situação, uma linha de
// contexto e a linha de ações (botão principal com nome + ⋮ quando houver mais).
// Substitui a linha da DataTable abaixo de 768 px; no desktop a tabela continua.
defineProps<{
  titulo?: string;
  destaque?: boolean;
}>();
</script>

<template>
  <article
    class="cartao-item"
    :class="{ 'cartao-item-destaque': destaque }"
    data-testid="cartao-item"
  >
    <div class="cartao-item-cabecalho">
      <div class="cartao-item-titulo">
        <slot name="titulo">{{ titulo }}</slot>
      </div>
      <div v-if="$slots.chips" class="cartao-item-chips">
        <slot name="chips" />
      </div>
    </div>

    <div v-if="$slots.meta" class="cartao-item-meta">
      <slot name="meta" />
    </div>

    <div v-if="$slots.acoes" class="cartao-item-acoes">
      <slot name="acoes" />
    </div>
  </article>
</template>

<style scoped>
.cartao-item {
  background: var(--ipb-branco, #fff);
  border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  border-radius: 12px;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.cartao-item-destaque {
  border-color: var(--ipb-verde, #234f32);
  box-shadow: 0 0 0 1px var(--ipb-verde, #234f32) inset;
}

.cartao-item-cabecalho {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 8px;
  flex-wrap: wrap;
}

.cartao-item-titulo {
  font-size: 15px;
  font-weight: 700;
  color: #222;
  line-height: 1.3;
  min-width: 0;
  overflow-wrap: anywhere;
}

.cartao-item-chips {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  align-items: center;
}

.cartao-item-meta {
  font-size: 13px;
  line-height: 1.45;
  color: var(--ipb-cinza-claro, #7a7a7a);
  overflow-wrap: anywhere;
}

.cartao-item-acoes {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
  margin-top: 2px;
}

/* Botões da linha de ações: alvo de toque de 44 px; o principal ocupa o espaço. */
.cartao-item-acoes :deep(.p-button) {
  min-height: 44px;
}

.cartao-item-acoes :deep(.acao-principal) {
  flex: 1 1 auto;
  justify-content: center;
}
</style>
