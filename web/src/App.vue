<script setup lang="ts">
import Toast from "primevue/toast";
import ConfirmDialog from "primevue/confirmdialog";
import ToastBridge from "./components/ui/ToastBridge.vue";

import type { OpcoesConfirmacao } from "./aplicacao/servicos/notificacoes";

// Etapa 6.4-C — diálogos de confirmação e avisos num único lugar, no tema do
// projeto (classes `kh-confirm` e `kh-toast`, estilos em style.css): título na fonte
// de destaque, ícone no padrão dos cartões, botão de confirmação no verde do projeto
// e, no celular, largura dentro da tela com os botões empilhados. O corpo aceita,
// além do texto, uma lista de itens com rolagem própria (`itens`) e um texto final
// (`rodape`) — ver OpcoesConfirmacao.
const BREAKPOINTS_CONFIRM = { "640px": "calc(100vw - 24px)" };
const BREAKPOINTS_TOAST = {
  "640px": { width: "calc(100vw - 24px)", left: "12px", right: "12px" },
};

function opcoes(message: unknown): OpcoesConfirmacao {
  return (message ?? {}) as OpcoesConfirmacao;
}
</script>

<template>
  <Toast class="kh-toast" :breakpoints="BREAKPOINTS_TOAST" />
  <ConfirmDialog
    class="kh-confirm"
    :breakpoints="BREAKPOINTS_CONFIRM"
    :style="{ width: '440px' }"
    :draggable="false"
  >
    <template #message="{ message }">
      <div class="kh-confirm-corpo" data-testid="confirm-corpo">
        <span class="kh-confirm-icone" aria-hidden="true">
          <i :class="opcoes(message).icon || 'pi pi-question-circle'"></i>
        </span>
        <div class="kh-confirm-texto">
          <p class="kh-confirm-mensagem">{{ opcoes(message).message }}</p>
          <ul
            v-if="opcoes(message).itens?.length"
            class="kh-confirm-lista"
            data-testid="confirm-lista"
          >
            <li v-for="(item, indice) in opcoes(message).itens" :key="indice">
              {{ item }}
            </li>
          </ul>
          <p v-if="opcoes(message).rodape" class="kh-confirm-rodape">
            {{ opcoes(message).rodape }}
          </p>
        </div>
      </div>
    </template>
  </ConfirmDialog>
  <ToastBridge />
  <RouterView />
</template>
