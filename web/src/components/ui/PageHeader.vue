<script setup lang="ts">
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";

const props = defineProps<{
  titulo: string;
  subtitulo?: string;
  voltarPara?: string;
  voltarLabel?: string;
  // Etapa 6.4 — dentro da Página da Turma (rotas filhas com meta `abaDaTurma`) o
  // cabeçalho da turma já mostra o nome e o link de voltar; o cabeçalho da aba só
  // mostra as ações (Nova aula, Matricular, Recarregar…), como uma barra de
  // ferramentas. "auto" (padrão) segue a meta da rota; a própria Página da Turma
  // passa "completo", porque a meta da filha também chega a ela (a meta é mesclada
  // ao longo da cadeia de rotas).
  modo?: "auto" | "completo";
}>();

const router = useRouter();
const route = useRoute();

const somenteAcoes = computed(
  () => props.modo !== "completo" && Boolean(route.meta?.abaDaTurma),
);

function voltar() {
  if (props.voltarPara) {
    void router.push(props.voltarPara);
  }
}
</script>

<template>
  <div
    class="page-header-ipb"
    :class="{ 'page-header-somente-acoes': somenteAcoes }"
  >
    <div v-if="!somenteAcoes">
      <a
        v-if="voltarPara"
        href="#"
        class="page-header-voltar"
        @click.prevent="voltar"
      >
        <i class="pi pi-arrow-left" aria-hidden="true"></i>
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
  flex-wrap: wrap;
}

/* Link de voltar (ajuste A2): maior, em verde e com peso, para ser visto; no
   celular, área de toque de 44 px (bloco do fim do arquivo). */
.page-header-voltar {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 32px;
  font-size: 15px;
  font-weight: 600;
  color: var(--ipb-verde, #234f32);
  text-decoration: none;
  border-radius: var(--radius-sm, 6px);
  margin-bottom: 2px;
  transition: color 0.15s ease;
}

.page-header-voltar .pi {
  font-size: 13px;
}

.page-header-voltar:hover {
  color: var(--ipb-verde-claro, #2e6b42);
}

.page-header-voltar:focus-visible {
  outline: 2px solid var(--ipb-verde, #234f32);
  outline-offset: 2px;
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
  flex-wrap: wrap;
}

.page-header-somente-acoes {
  justify-content: flex-start;
}

@media (max-width: 768px) {
  .page-header-voltar {
    min-height: 44px;
    min-width: 44px;
    margin-bottom: 0;
  }

  .page-header-titulo {
    font-size: 20px;
  }

  .page-header-acoes {
    width: 100%;
  }
}
</style>
