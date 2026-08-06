<script setup lang="ts">
import { computed } from "vue";
import { useRouter, useRoute } from "vue-router";
import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";

const autenticacao = usarAutenticacaoStore();
const router = useRouter();
const route = useRoute();

const isAdmin = computed(() => autenticacao.isAdmin);

const isAdministrativo = computed(() => autenticacao.isAdministrativo);

function estaAtivo(caminho: string) {
  return route.path === caminho;
}

function sair() {
  autenticacao.sair();
  router.push("/login");
}
</script>

<template>
  <aside class="sidebar-ipb">
    <div class="sidebar-igreja">
      <div class="sidebar-igreja-nome">Igreja Presbiteriana</div>
      <div class="sidebar-igreja-sub">Painel Administrativo</div>
    </div>

    <div class="sidebar-secao">Gestão</div>

    <RouterLink to="/" class="sidebar-link" :class="{ ativo: estaAtivo('/') }">
      <i class="pi pi-home sidebar-icone"></i>
      Painel
    </RouterLink>

    <RouterLink
      to="/minhas-turmas"
      class="sidebar-link"
      :class="{ ativo: estaAtivo('/minhas-turmas') }"
    >
      <i class="pi pi-users sidebar-icone"></i>
      Minhas Turmas
    </RouterLink>

    <RouterLink
      to="/meus-dados"
      class="sidebar-link"
      :class="{ ativo: estaAtivo('/meus-dados') }"
    >
      <i class="pi pi-user sidebar-icone"></i>
      Meus Dados
    </RouterLink>

    <RouterLink
      v-if="isAdministrativo"
      to="/pessoas"
      class="sidebar-link"
      :class="{ ativo: estaAtivo('/pessoas') }"
    >
      <i class="pi pi-users sidebar-icone"></i>
      Pessoas
    </RouterLink>

    <RouterLink
      to="/relatorios/ebd"
      class="sidebar-link"
      :class="{ ativo: estaAtivo('/relatorios/ebd') }"
    >
      <i class="pi pi-chart-bar sidebar-icone"></i>
      Relatórios EBD
    </RouterLink>

    <RouterLink
      v-if="isAdmin"
      to="/usuarios"
      class="sidebar-link"
      :class="{ ativo: estaAtivo('/usuarios') }"
    >
      <i class="pi pi-user-edit sidebar-icone"></i>
      Usuários
    </RouterLink>

    <div class="sidebar-secao">EBD</div>

    <RouterLink
      to="/departamentos"
      class="sidebar-link"
      :class="{ ativo: estaAtivo('/departamentos') }"
    >
      <i class="pi pi-sitemap sidebar-icone"></i>
      Turmas EBD
    </RouterLink>

    <div class="sidebar-spacer"></div>

    <button class="sidebar-sair" @click="sair">
      <i class="pi pi-sign-out"></i>
      Sair
    </button>
  </aside>
</template>

<style scoped>
.sidebar-ipb {
  width: var(--sidebar-w, 272px);
  background: var(--ipb-branco, #fff);
  border-right: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  display: flex;
  flex-direction: column;
  padding: 20px 14px;
  gap: 4px;
  overflow-y: auto;
}

.sidebar-igreja {
  padding: 14px;
  background: var(--ipb-verde-bg, #edf5f0);
  border-radius: var(--radius-md, 10px);
  border-left: 4px solid var(--ipb-verde, #234f32);
  margin-bottom: 12px;
}

.sidebar-igreja-nome {
  font-family: var(--font-display, Georgia);
  font-weight: 700;
  font-size: 14px;
  color: var(--ipb-verde-escuro, #1a3b25);
}

.sidebar-igreja-sub {
  font-size: 11px;
  color: var(--ipb-cinza-claro, #7a7a7a);
  margin-top: 2px;
}

.sidebar-secao {
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 1.2px;
  color: var(--ipb-cinza-claro, #7a7a7a);
  font-weight: 600;
  padding: 14px 14px 4px;
}

.sidebar-link {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  border-radius: var(--radius-sm, 6px);
  text-decoration: none;
  color: var(--ipb-cinza, #4d4d4d);
  font-size: 14px;
  font-weight: 500;
  transition: all 0.15s ease;
}

.sidebar-link:hover {
  background: var(--ipb-verde-bg, #edf5f0);
  color: var(--ipb-verde, #234f32);
}

.sidebar-link.ativo {
  background: var(--ipb-verde, #234f32);
  color: #fff;
  font-weight: 600;
  box-shadow: var(--shadow-card);
}

.sidebar-icone {
  width: 20px;
  text-align: center;
  font-size: 15px;
}

.sidebar-spacer {
  flex: 1;
}

.sidebar-sair {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  border-radius: var(--radius-sm, 6px);
  border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  background: transparent;
  color: var(--ipb-cinza-claro, #7a7a7a);
  cursor: pointer;
  font-size: 13px;
  font-family: var(--font-body, sans-serif);
  transition: all 0.15s ease;
}

.sidebar-sair:hover {
  border-color: var(--ipb-erro, #b83232);
  color: var(--ipb-erro, #b83232);
}
</style>
