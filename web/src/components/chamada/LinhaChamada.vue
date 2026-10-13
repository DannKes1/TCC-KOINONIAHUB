<script setup lang="ts">
import { ref, watch } from "vue";

import Checkbox from "primevue/checkbox";
import InputText from "primevue/inputtext";
import Tag from "primevue/tag";

import type { LinhaChamada } from "../../aplicacao/dominio/chamada";

// Linha da chamada (RF32 / CSU10), igual nos dois layouts: tocar em qualquer ponto
// da linha alterna presente/ausente; a observação só aparece quando o professor
// pede ("+ observação") — e é nesse momento que a tela orienta a não registrar
// informações de saúde ou outros dados sensíveis (RNF 32.5).
const props = defineProps<{
  linha: LinhaChamada;
  somenteLeitura?: boolean;
  desabilitado?: boolean;
  semRegistro?: boolean;
}>();

const emit = defineEmits<{
  (e: "update:presente", valor: boolean): void;
  (e: "update:observacao", valor: string | null): void;
}>();

const observacaoAberta = ref(Boolean(props.linha.observacao));

// Recarga da chamada: linha com observação volta aberta; sem, fechada.
watch(
  () => props.linha.observacao,
  (valor) => {
    if (valor) observacaoAberta.value = true;
  },
);

const bloqueada = () => Boolean(props.somenteLeitura || props.desabilitado);

function alternar() {
  if (bloqueada()) return;
  emit("update:presente", !props.linha.presente);
}

function abrirObservacao() {
  if (bloqueada()) return;
  observacaoAberta.value = true;
}

function removerObservacao() {
  emit("update:observacao", null);
  observacaoAberta.value = false;
}
</script>

<template>
  <div
    class="linha-chamada"
    :class="{
      'linha-chamada-presente': linha.presente,
      'linha-chamada-sem-registro': semRegistro,
      'linha-chamada-bloqueada': somenteLeitura || desabilitado,
    }"
    role="button"
    :tabindex="somenteLeitura || desabilitado ? -1 : 0"
    :aria-pressed="linha.presente"
    :aria-label="`${linha.nomeAluno}: ${linha.presente ? 'presente' : 'ausente'}`"
    data-testid="chamada-linha"
    @click="alternar"
    @keydown.enter.prevent="alternar"
    @keydown.space.prevent="alternar"
  >
    <div class="linha-chamada-principal">
      <div class="linha-chamada-texto">
        <span class="linha-chamada-nome" data-testid="chamada-nome">
          {{ linha.nomeAluno }}
          <Tag
            v-if="semRegistro"
            value="Sem registro"
            severity="warning"
            class="linha-chamada-tag"
          />
        </span>

        <!-- Só o botão interrompe o toque; o resto da faixa continua alternando a
             presença (o invólucro estica na coluna e não pode engolir o toque). -->
        <span class="linha-chamada-obs">
          <button
            v-if="!somenteLeitura && !observacaoAberta"
            type="button"
            class="botao-texto"
            :disabled="desabilitado"
            data-testid="chamada-abrir-observacao"
            @click.stop="abrirObservacao"
            @keydown.stop
          >
            + observação
          </button>
          <span
            v-else-if="somenteLeitura && linha.observacao"
            class="linha-chamada-obs-texto"
            data-testid="chamada-observacao-texto"
          >
            Obs.: {{ linha.observacao }}
          </span>
        </span>
      </div>

      <span class="linha-chamada-caixa" @click.stop @keydown.stop>
        <Checkbox
          :modelValue="linha.presente"
          :binary="true"
          :disabled="somenteLeitura || desabilitado"
          :inputId="`presente-${linha.alunoDepartamentoId}`"
          :aria-label="`Presente: ${linha.nomeAluno}`"
          data-testid="chamada-presente"
          @update:modelValue="emit('update:presente', Boolean($event))"
        />
      </span>
    </div>

    <div
      v-if="observacaoAberta && !somenteLeitura"
      class="linha-chamada-observacao"
      @click.stop
      @keydown.stop
    >
      <InputText
        :modelValue="linha.observacao ?? ''"
        placeholder="Justificativa administrativa (opcional)"
        maxlength="500"
        :disabled="desabilitado"
        data-testid="chamada-observacao"
        @update:modelValue="emit('update:observacao', String($event ?? ''))"
      />
      <div class="linha-chamada-observacao-rodape">
        <small class="dica-observacao" data-testid="dica-observacao">
          <i class="pi pi-info-circle" aria-hidden="true"></i>
          Use só para justificativas administrativas (atraso, dispensa, saída
          antecipada). Não registre informações de saúde ou outros dados sensíveis.
        </small>
        <button
          type="button"
          class="botao-texto botao-texto-discreto"
          :disabled="desabilitado"
          data-testid="chamada-remover-observacao"
          @click="removerObservacao"
        >
          Remover
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.linha-chamada {
  border-bottom: 1px solid #eef0ee;
  background: var(--ipb-branco, #fff);
  cursor: pointer;
  user-select: none;
  outline-color: var(--ipb-verde, #234f32);
  transition: background 0.12s ease;
}

.linha-chamada:last-child {
  border-bottom: none;
}

.linha-chamada-presente {
  background: #f1f8f3;
}

.linha-chamada-sem-registro {
  background: #fff4e5;
}

.linha-chamada-bloqueada {
  cursor: default;
}

.linha-chamada:not(.linha-chamada-bloqueada):hover {
  background: #f6faf7;
}

.linha-chamada-presente:not(.linha-chamada-bloqueada):hover {
  background: #e9f4ec;
}

.linha-chamada-principal {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 56px;
  padding: 8px 12px 8px 14px;
}

.linha-chamada-texto {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.linha-chamada-nome {
  font-size: 15px;
  color: #222;
  overflow-wrap: anywhere;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.linha-chamada-tag {
  font-size: 11px;
}

.linha-chamada-obs {
  display: inline-flex;
  align-items: center;
  align-self: flex-start;
  min-height: 24px;
}

.linha-chamada-obs-texto {
  font-size: 12.5px;
  color: var(--ipb-cinza-claro, #7a7a7a);
  overflow-wrap: anywhere;
}

.botao-texto {
  background: none;
  border: none;
  padding: 2px 0;
  min-height: 28px;
  font: inherit;
  font-size: 12.5px;
  font-weight: 600;
  color: var(--ipb-verde, #234f32);
  cursor: pointer;
}

.botao-texto:disabled {
  opacity: 0.5;
  cursor: default;
}

.botao-texto-discreto {
  color: var(--ipb-cinza-claro, #7a7a7a);
}

/* Alvo de toque de 40 px em volta da caixa de 24 px (o input invisível do PrimeVue
   cobre todo o .p-checkbox). */
.linha-chamada-caixa {
  flex: 0 0 auto;
  display: inline-flex;
}

.linha-chamada-caixa :deep(.p-checkbox) {
  width: 2.5rem;
  height: 2.5rem;
  align-items: center;
  justify-content: center;
}

.linha-chamada-caixa :deep(.p-checkbox .p-checkbox-box) {
  width: 1.5rem;
  height: 1.5rem;
}

.linha-chamada-observacao {
  padding: 0 14px 12px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  cursor: default;
}

.linha-chamada-observacao :deep(.p-inputtext) {
  width: 100%;
}

.linha-chamada-observacao-rodape {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;
}

.dica-observacao {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  font-size: 12px;
  line-height: 1.4;
  color: var(--ipb-cinza, #4d4d4d);
}

.dica-observacao .pi {
  margin-top: 2px;
  color: var(--ipb-verde, #234f32);
}
</style>
