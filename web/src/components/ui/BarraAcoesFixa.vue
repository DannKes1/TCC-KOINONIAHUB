<script setup lang="ts">
// Etapa 6.4 — barra de ações da chamada (Salvar · Consolidar), visível em qualquer
// ponto da rolagem. É `sticky` no fim da página, não `fixed` (corretiva da 6.4-B):
// gruda na borda de baixo da janela enquanto há conteúdo abaixo e, no fim da
// rolagem, para logo acima do rodapé — nunca o cobre, em nenhuma largura. Ocupa a
// largura da área de conteúdo (as margens negativas anulam o respiro do <main>), por
// isso também não invade a barra lateral. Deve ser o último elemento da página, e a
// página deve ter `flex: 1 0 auto` para a barra ficar junto ao rodapé quando a lista
// é curta (o <main> é uma coluna flex).
//
// Slot `resumo`: texto à esquerda dos botões, só a partir de 769 px (no celular os
// botões ocupam a largura toda e o resumo já está no topo da tela).
defineProps<{ rotulo?: string }>();
</script>

<template>
  <div
    class="barra-acoes-fixa"
    role="toolbar"
    :aria-label="rotulo ?? 'Ações'"
    data-testid="barra-acoes"
  >
    <div v-if="$slots.resumo" class="barra-acoes-fixa-resumo">
      <slot name="resumo" />
    </div>
    <div class="barra-acoes-fixa-botoes">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.barra-acoes-fixa {
  position: sticky;
  bottom: 0;
  z-index: 20;
  /* Encosta nas bordas da área de conteúdo (padding do <main>: 32 px e 28 px) e,
     com `margin-top: auto`, no fim da página quando a lista é curta. */
  margin: auto -32px -28px;
  padding: 12px 32px calc(12px + env(safe-area-inset-bottom, 0px));
  background: var(--ipb-branco, #fff);
  border-top: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  box-shadow: 0 -4px 16px rgba(0, 0, 0, 0.06);
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 16px;
}

.barra-acoes-fixa-resumo {
  flex: 1 1 auto;
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 14px;
  flex-wrap: wrap;
  font-size: 14px;
  color: var(--ipb-cinza, #4d4d4d);
}

.barra-acoes-fixa-botoes {
  display: flex;
  gap: 10px;
  align-items: center;
}

.barra-acoes-fixa-botoes :deep(.p-button) {
  min-height: 44px;
}

.barra-acoes-fixa-botoes :deep(.acao-principal) {
  min-width: 220px;
  justify-content: center;
}

@media (max-width: 768px) {
  /* Padding do <main> no celular: 16 px (LayoutPrincipal). */
  .barra-acoes-fixa {
    margin: auto -16px -16px;
    padding-left: 16px;
    padding-right: 16px;
  }

  .barra-acoes-fixa-resumo {
    display: none;
  }

  .barra-acoes-fixa-botoes {
    flex: 1 1 auto;
  }

  .barra-acoes-fixa-botoes :deep(.acao-principal) {
    flex: 1 1 auto;
    min-width: 0;
  }
}
</style>
