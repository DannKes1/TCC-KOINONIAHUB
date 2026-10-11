<script setup lang="ts">
import { computed, ref } from "vue";

import Button from "primevue/button";
import Menu from "primevue/menu";
import type { MenuItem } from "primevue/menuitem";

// Etapa 6.4 — botão ⋮ com as ações secundárias de um item (aula, matrícula…), no
// mesmo Menu popup do PrimeVue que Turmas EBD já usa. Itens com `visible: false`
// são filtrados aqui para o botão sumir quando não sobra nada.
const props = defineProps<{
  itens: MenuItem[];
  rotulo?: string;
  desabilitado?: boolean;
}>();

const menu = ref<InstanceType<typeof Menu> | null>(null);

const itensVisiveis = computed(() =>
  props.itens.filter((item) =>
    typeof item.visible === "function" ? item.visible() : item.visible !== false,
  ),
);

function alternar(evento: Event) {
  menu.value?.toggle(evento);
}
</script>

<template>
  <template v-if="itensVisiveis.length > 0">
    <Button
      type="button"
      icon="pi pi-ellipsis-v"
      severity="secondary"
      outlined
      class="menu-acoes-botao"
      :aria-label="rotulo ?? 'Mais ações'"
      aria-haspopup="true"
      :disabled="desabilitado"
      data-testid="menu-acoes"
      @click="alternar"
    />
    <Menu ref="menu" :model="itensVisiveis" :popup="true" />
  </template>
</template>

<style scoped>
.menu-acoes-botao {
  flex: 0 0 auto;
  min-width: 44px;
}
</style>
