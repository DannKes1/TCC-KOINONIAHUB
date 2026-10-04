<script setup lang="ts">
import { computed } from "vue";

import Checkbox from "primevue/checkbox";
import Button from "primevue/button";

import type {
  TermoIgrejaVM,
  TermoVigenteVM,
} from "../../aplicacao/modelos/dtos";

// Termo de Uso e Sigilo (RF42; RNFs 42.1, 42.5, 42.7). Mostra a versão vigente
// com a identificação da igreja e o canal de contato FORA do texto fixo (o hash
// cobre só o texto), a caixa de aceite e, opcionalmente, o botão de aceitar.
// Usado em três lugares: cadastro inicial e primeiro acesso (embutido, sem
// botão — o aceite vai junto do formulário) e na tela /termo após o login.
const props = withDefaults(
  defineProps<{
    termo: TermoVigenteVM | null;
    // Checkbox "Li e aceito" (v-model). Quem embute o componente usa este valor
    // para habilitar o próprio botão de concluir.
    modelValue: boolean;
    // Substitui a igreja vinda da API — no cadastro inicial ela ainda não
    // existe, então vem do próprio formulário.
    igreja?: TermoIgrejaVM | null;
    carregando?: boolean;
    erro?: string;
    mostrarBotao?: boolean;
    salvando?: boolean;
  }>(),
  {
    igreja: undefined,
    carregando: false,
    erro: "",
    mostrarBotao: true,
    salvando: false,
  },
);

const emit = defineEmits<{
  (e: "update:modelValue", valor: boolean): void;
  (e: "aceitar", versao: string): void;
}>();

const aceito = computed({
  get: () => props.modelValue,
  set: (valor: boolean) => emit("update:modelValue", Boolean(valor)),
});

const igrejaExibida = computed<TermoIgrejaVM | null>(() => {
  if (props.igreja !== undefined) return props.igreja;
  return props.termo?.igreja ?? null;
});

const nomeIgreja = computed(() => igrejaExibida.value?.nome?.trim() || "");

// Canal para assuntos relacionados a dados pessoais (RNF 42.7): e-mail e/ou
// telefone cadastrados; sem nenhum dos dois, fica explícito que a igreja ainda
// não definiu o canal — nunca inventamos um contato.
const canalContato = computed(() => {
  const partes = [
    igrejaExibida.value?.email?.trim(),
    igrejaExibida.value?.telefone?.trim(),
  ].filter((p): p is string => Boolean(p));
  return partes.join(" · ");
});

function formatarData(iso: string): string {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  return d.toLocaleDateString("pt-BR", { timeZone: "UTC" });
}

const vigenciaFormatada = computed(() =>
  formatarData(props.termo?.vigenteDesde ?? ""),
);

const podeAceitar = computed(
  () => Boolean(props.termo) && aceito.value && !props.salvando,
);

function aceitar() {
  if (!props.termo || !podeAceitar.value) return;
  emit("aceitar", props.termo.versao);
}
</script>

<template>
  <section class="termo" data-testid="termo-uso-sigilo">
    <div v-if="carregando" class="termo-estado">
      <i class="pi pi-spin pi-spinner"></i>
      <span>Carregando o Termo de Uso e Sigilo...</span>
    </div>

    <div v-else-if="erro" class="termo-erro" data-testid="termo-erro">
      {{ erro }}
    </div>

    <template v-else-if="termo">
      <div class="termo-cabecalho">
        <div class="termo-titulo">Termo de Uso e Sigilo</div>
        <div class="termo-versao" data-testid="termo-versao">
          Versão {{ termo.versao }}
          <template v-if="vigenciaFormatada">
            · vigente desde {{ vigenciaFormatada }}
          </template>
        </div>
      </div>

      <!-- Dados da igreja fora do texto fixo (RNF 42.7) -->
      <dl class="termo-igreja" data-testid="termo-igreja">
        <div>
          <dt>Instituição</dt>
          <dd data-testid="termo-igreja-nome">
            {{ nomeIgreja || "[igreja em cadastro]" }}
          </dd>
        </div>
        <div>
          <dt>Canal para assuntos relacionados a dados pessoais</dt>
          <dd data-testid="termo-igreja-contato">
            {{ canalContato || "[contato a ser definido pela igreja]" }}
          </dd>
        </div>
      </dl>

      <div class="termo-texto" data-testid="termo-texto" tabindex="0">{{
        termo.texto
      }}</div>

      <div class="termo-aceite">
        <Checkbox
          v-model="aceito"
          binary
          inputId="aceite-termo"
          :disabled="salvando"
        />
        <label for="aceite-termo">
          Li e aceito o Termo de Uso e Sigilo do KoinoniaHub.
        </label>
      </div>

      <Button
        v-if="mostrarBotao"
        data-testid="botao-aceitar-termo"
        label="Aceitar e continuar"
        icon="pi pi-check"
        :disabled="!podeAceitar"
        :loading="salvando"
        @click="aceitar"
      />
    </template>
  </section>
</template>

<style scoped>
.termo {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.termo-estado {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--ipb-cinza-claro, #7a7a7a);
  font-size: 14px;
}

.termo-erro {
  color: var(--ipb-erro, #b83232);
  font-size: 13px;
}

.termo-cabecalho {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  justify-content: space-between;
  gap: 6px;
}

.termo-titulo {
  font-family: var(--font-display, Georgia);
  font-size: 17px;
  font-weight: 700;
  color: var(--ipb-verde-escuro, #1a3b25);
}

.termo-versao {
  font-size: 12px;
  color: var(--ipb-cinza-claro, #7a7a7a);
}

.termo-igreja {
  margin: 0;
  padding: 10px 12px;
  border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  border-radius: 8px;
  background: var(--ipb-verde-bg, #edf5f0);
  display: grid;
  gap: 8px;
}

.termo-igreja dt {
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 0.3px;
  opacity: 0.75;
}

.termo-igreja dd {
  margin: 2px 0 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--ipb-cinza, #4d4d4d);
}

.termo-texto {
  max-height: 320px;
  overflow-y: auto;
  padding: 14px;
  border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  border-radius: 8px;
  background: var(--ipb-branco, #fff);
  font-size: 13.5px;
  line-height: 1.6;
  color: var(--ipb-cinza, #4d4d4d);
  white-space: pre-wrap;
  text-align: left;
}

.termo-aceite {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  font-size: 14px;
  color: var(--ipb-cinza, #4d4d4d);
}

.termo-aceite label {
  cursor: pointer;
  line-height: 1.4;
}
</style>
