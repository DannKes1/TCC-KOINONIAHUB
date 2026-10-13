<script setup lang="ts">
// Etapa 6.4 — barra de ações fixa no rodapé da janela (Salvar · Consolidar na
// chamada): sempre visível em qualquer ponto da rolagem. No desktop começa depois
// da barra lateral; no celular ocupa a largura toda. Quem a usa reserva o espaço
// no fim da página (classe `espaco-barra-acoes`) para a barra não cobrir o conteúdo.
defineProps<{ rotulo?: string }>();
</script>

<template>
  <div
    class="barra-acoes-fixa"
    role="toolbar"
    :aria-label="rotulo ?? 'Ações'"
    data-testid="barra-acoes"
  >
    <div class="barra-acoes-fixa-conteudo">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.barra-acoes-fixa {
  position: fixed;
  left: var(--sidebar-w, 272px);
  right: 0;
  bottom: 0;
  z-index: 90;
  background: var(--ipb-branco, #fff);
  border-top: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  box-shadow: 0 -4px 16px rgba(0, 0, 0, 0.08);
  padding: 10px 32px calc(10px + env(safe-area-inset-bottom, 0px));
}

.barra-acoes-fixa-conteudo {
  display: flex;
  gap: 10px;
  align-items: center;
  max-width: 820px;
}

.barra-acoes-fixa-conteudo :deep(.p-button) {
  min-height: 44px;
}

.barra-acoes-fixa-conteudo :deep(.acao-principal) {
  flex: 1 1 auto;
  justify-content: center;
}

@media (max-width: 768px) {
  .barra-acoes-fixa {
    left: 0;
    padding-left: 16px;
    padding-right: 16px;
  }
}
</style>
