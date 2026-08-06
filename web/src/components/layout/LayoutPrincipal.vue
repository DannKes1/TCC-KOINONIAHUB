<script setup lang="ts">
import BarraTopo from "./BarraTopo.vue";
import MenuLateral from "./MenuLateral.vue";
</script>

<template>
  <div class="layout">
    <BarraTopo />

    <div class="corpo">
      <MenuLateral />

      <main class="conteudo">
        <RouterView />
      </main>
    </div>

    <footer class="footer-ipb">
      KoinoniaHub © 2026 — Sistema de Gestão da Escola Bíblica Dominical
      <div class="footer-slogan">"Corpo vivo de Cristo vivendo em família"</div>
    </footer>
  </div>
</template>

<style scoped>
.layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.corpo {
  display: flex;
  flex: 1;
  min-height: 0;
}

.conteudo {
  flex: 1;
  min-width: 0;
  padding: 28px 32px;
  overflow-y: auto;
  background: var(--ipb-cinza-bg, #f7f7f7);
}

.footer-ipb {
  background: var(--ipb-verde-escuro, #1a3b25);
  color: rgba(255, 255, 255, 0.6);
  text-align: center;
  padding: 16px 24px;
  font-size: 12px;
  letter-spacing: 0.3px;
}

.footer-slogan {
  font-family: var(--font-display, Georgia);
  font-style: italic;
  font-size: 13px;
  color: rgba(255, 255, 255, 0.45);
  margin-top: 4px;
}
</style>
