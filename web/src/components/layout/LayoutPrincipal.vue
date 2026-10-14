<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useRoute } from "vue-router";

import Drawer from "primevue/drawer";

import BarraTopo from "./BarraTopo.vue";
import MenuLateral from "./MenuLateral.vue";

// Responsividade (RNFs "a interface deve ser responsiva", Etapa 6.3): até 768 px a
// barra lateral sai do fluxo e o mesmo MenuLateral abre num Drawer pelo botão da
// BarraTopo. A instância de desktop continua no DOM (oculta por CSS), então nada
// muda para quem lê o menu por seletor; o Drawer só renderiza enquanto está aberto.
const route = useRoute();
const menuAberto = ref(false);

// Ajuste da corretiva da 6.4-B: só a tela que tem a barra de ações (meta
// `barraAcoes` na rota) fica sem o overflow do <main> — é o que deixa a barra
// `sticky` grudar na janela. Nas demais, o <main> volta a conter a própria largura,
// como antes da corretiva: conteúdo mais largo que 360 px rola dentro dele
// (telas administrativas, "utilizáveis com rolagem") sem alargar o documento.
const comBarraAcoes = computed(() => Boolean(route.meta.barraAcoes));

// Qualquer navegação fecha o Drawer (o link do menu já levou para a tela).
watch(
  () => route.fullPath,
  () => {
    menuAberto.value = false;
  },
);
</script>

<template>
  <div class="layout">
    <BarraTopo @abrir-menu="menuAberto = true" />

    <div class="corpo">
      <MenuLateral class="sidebar-desktop" />

      <Drawer
        v-model:visible="menuAberto"
        position="left"
        class="menu-drawer"
        :showCloseIcon="true"
        :blockScroll="true"
        header="Menu"
        data-testid="menu-drawer"
      >
        <MenuLateral em-drawer />
      </Drawer>

      <main class="conteudo" :class="{ 'conteudo-com-barra': comBarraAcoes }">
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

/* O <main> contém a própria largura (overflow): uma tabela ou um filtro mais largo
   que a janela rola dentro dele, sem alargar o documento nem deslocar a barra do
   topo (critério a 360 px: largura do documento = largura da janela). Quem rola na
   vertical continua sendo o documento (o layout tem min-height, não height).
   Coluna flex: a página pode ocupar a altura toda do <main> (flex: 1) quando
   precisa encostar algo no fim — a barra de ações da chamada. */
.conteudo {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  padding: 28px 32px;
  background: var(--ipb-cinza-bg, #f7f7f7);
  overflow-x: auto;
}

/* Tela com barra de ações (meta `barraAcoes`, Etapa 6.4-B corretiva): sem overflow,
   senão o <main> vira contêiner de rolagem e `position: sticky` deixa de grudar na
   janela — a barra precisa disso para nunca cobrir o rodapé. A própria tela garante
   que cabe em 360 px (E2E-M1). */
.conteudo-com-barra {
  overflow: visible;
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

:deep(.menu-drawer) {
  width: 288px !important;
  max-width: 86vw;
}

:deep(.menu-drawer .p-drawer-content) {
  padding: 0;
}

@media (max-width: 768px) {
  .sidebar-desktop {
    display: none;
  }

  .conteudo {
    padding: 16px;
  }

  .footer-ipb {
    padding: 12px 16px;
    font-size: 11px;
  }
}
</style>
