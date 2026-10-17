<script setup lang="ts">
import { computed, ref, watch } from "vue";

import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import Tag from "primevue/tag";

import type { ParentescoVM } from "../../aplicacao/modelos/dtos";

// RF12 / RNF 12.3 (CSU13 passo 2 e FA1): a mesma tabela do diálogo de Parentescos.
// A linha derivada (grau inverso de um vínculo registrado no cadastro do parente)
// recebe a tag "derivado" e não tem o botão de remover: a remoção é feita no
// cadastro de origem. No lugar do botão há um ⓘ que mostra e esconde a explicação
// como texto dentro da própria linha — sem tooltip nem Popover (nada montado fora
// da tabela, que sobreviveria ao fechamento do diálogo) e funcionando no toque.
const props = defineProps<{
  itens: ParentescoVM[];
  carregando?: boolean;
}>();

const emit = defineEmits<{
  (e: "remover", item: ParentescoVM): void;
}>();

const possuiDerivados = computed(() => props.itens.some((i) => i.derivado));

// Id da linha derivada com a explicação aberta (uma por vez). Fecha quando a lista
// é recarregada, para não ficar apontando para uma linha que já não existe.
const explicacaoAbertaId = ref<number | null>(null);
watch(
  () => props.itens,
  () => {
    explicacaoAbertaId.value = null;
  },
);

function explicacaoAberta(item: ParentescoVM) {
  return explicacaoAbertaId.value === item.id;
}

function alternarExplicacao(item: ParentescoVM) {
  explicacaoAbertaId.value = explicacaoAberta(item) ? null : item.id;
}

function explicacaoDerivado(item: ParentescoVM) {
  return `Vínculo registrado no cadastro de ${item.parenteNome}. Para removê-lo, abra os parentescos dessa pessoa.`;
}
</script>

<template>
  <div style="display: flex; flex-direction: column; gap: 8px">
    <DataTable :value="itens" dataKey="id" responsiveLayout="scroll">
      <template #empty>
        <div style="padding: 14px; opacity: 0.7">Nenhum parentesco cadastrado.</div>
      </template>
      <Column field="parenteNome" header="Parente" />
      <Column header="Relacionamento">
        <template #body="{ data }">
          <div
            style="display: flex; flex-direction: column; gap: 4px"
            :data-testid="`parentesco-${data.id}`"
          >
            <span style="display: inline-flex; align-items: center; gap: 8px; flex-wrap: wrap">
              {{ data.tipoRelacionamento }}
              <Tag
                v-if="data.derivado"
                value="derivado"
                severity="secondary"
                data-testid="tag-derivado"
              />
            </span>
            <small
              v-if="data.derivado && explicacaoAberta(data)"
              :id="`explicacao-derivado-${data.id}`"
              style="opacity: 0.8; line-height: 1.35"
              data-testid="explicacao-derivado"
            >
              {{ explicacaoDerivado(data) }}
            </small>
          </div>
        </template>
      </Column>

      <Column header="Ações" style="width: 110px">
        <template #body="{ data }">
          <Button
            v-if="!data.derivado"
            icon="pi pi-trash"
            severity="danger"
            :disabled="carregando"
            data-testid="parentesco-remover"
            @click="emit('remover', data)"
          />
          <Button
            v-else
            type="button"
            icon="pi pi-info-circle"
            severity="secondary"
            text
            rounded
            style="min-width: 40px; min-height: 40px"
            :aria-label="`Por que este vínculo não pode ser removido aqui: ${data.parenteNome}`"
            :aria-expanded="explicacaoAberta(data) ? 'true' : 'false'"
            :aria-controls="`explicacao-derivado-${data.id}`"
            data-testid="parentesco-info-derivado"
            @click="alternarExplicacao(data)"
          />
        </template>
      </Column>
    </DataTable>

    <small
      v-if="possuiDerivados"
      style="opacity: 0.75"
      data-testid="nota-derivados"
    >
      Vínculos marcados como <strong>derivado</strong> foram registrados no cadastro
      do parente e só podem ser removidos por lá.
    </small>
  </div>
</template>
