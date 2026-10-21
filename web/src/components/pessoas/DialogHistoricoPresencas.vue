<script setup lang="ts">
import { computed, ref, watch } from "vue";

import Dialog from "primevue/dialog";
import Button from "primevue/button";
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Tag from "primevue/tag";

import InlineMessage from "../ui/InlineMessage.vue";
import LoadingOverlay from "../ui/LoadingOverplay.vue";
import CartaoItem from "../ui/CartaoItem.vue";
import TagSituacaoAula from "../ui/TagSituacaoAula.vue";

import { usarTelaCompacta } from "../../aplicacao/composables/usarTelaCompacta";
import { usarMostrarMais } from "../../aplicacao/composables/usarMostrarMais";
import { listarHistoricoPresencasDaPessoa } from "../../aplicacao/servicos/presencasPessoaServico";
import {
  formatarPercentual,
  resumirPresencas,
} from "../../aplicacao/dominio/indicadoresPresenca";
import type { HistoricoPresencaPessoaVM } from "../../aplicacao/modelos/dtos";

// Histórico de presenças de uma pessoa (RF34 / CSU14). Corretiva da 7.3: o diálogo
// que vivia em Pessoas (gestão) virou componente para servir também à aba Alunos da
// Página da Turma, onde o Professor consulta os alunos das suas turmas (RNF 34.3). Ele
// mesmo carrega o histórico ao abrir. Totais só com aulas Consolidadas (CSU07); a
// lista traz todos os registros com a situação da aula. Abaixo de 768 px a tabela vira
// cartões, sem rolagem horizontal; no desktop, a tabela de sempre.
const props = defineProps<{
  visible: boolean;
  pessoaId: number | null;
  nome: string;
}>();

const emit = defineEmits<{
  (e: "update:visible", valor: boolean): void;
}>();

const compacta = usarTelaCompacta();

const visivel = computed({
  get: () => props.visible,
  set: (valor: boolean) => emit("update:visible", valor),
});

const historico = ref<HistoricoPresencaPessoaVM[]>([]);
const carregando = ref(false);
const erro = ref("");

// Uma resposta que chega depois de o diálogo fechar ou trocar de pessoa é descartada:
// nunca mostrar as presenças de um aluno sob o nome de outro.
let sequencia = 0;

const resumo = computed(() => resumirPresencas(historico.value));
const percentual = computed(() => formatarPercentual(resumo.value.percentual));

// Cartões: do registro mais recente para o mais antigo, em blocos de dez.
const historicoOrdenado = computed(() =>
  [...historico.value].sort(
    (a, b) => new Date(b.dataAula).getTime() - new Date(a.dataAula).getTime(),
  ),
);
const { visiveis, restantes, mostrarMais } = usarMostrarMais(historicoOrdenado, 10);

async function carregar() {
  if (!props.pessoaId) return;
  const minha = ++sequencia;

  carregando.value = true;
  erro.value = "";
  try {
    const lista = await listarHistoricoPresencasDaPessoa(props.pessoaId);
    if (minha !== sequencia) return;
    historico.value = lista;
  } catch (e: any) {
    if (minha !== sequencia) return;
    erro.value =
      e?.response?.data?.mensagem ??
      "Não foi possível carregar o histórico de presenças.";
  } finally {
    if (minha === sequencia) carregando.value = false;
  }
}

watch(
  () => props.visible,
  (aberto) => {
    historico.value = [];
    erro.value = "";
    if (aberto) {
      void carregar();
    } else {
      sequencia++; // invalida uma resposta ainda em voo
      carregando.value = false;
    }
  },
  { immediate: true },
);

function formatarData(valor?: string | null) {
  if (!valor) return "-";
  const data = new Date(valor);
  return Number.isNaN(data.getTime()) ? "-" : data.toLocaleDateString("pt-BR");
}

function severityPresenca(presente: boolean) {
  return presente ? "success" : "danger";
}
</script>

<template>
  <Dialog
    v-model:visible="visivel"
    modal
    :closable="!carregando"
    :dismissableMask="!carregando"
    :closeOnEscape="!carregando"
    :header="`Histórico de Presenças - ${nome}`"
    style="width: 980px; max-width: 96vw"
    data-testid="historico-dialogo"
  >
    <div style="display: flex; flex-direction: column; gap: 14px">
      <InlineMessage :texto="erro" tipo="erro" />

      <div class="historico-indicadores" data-testid="historico-indicadores">
        <div class="historico-indicador">
          <div class="historico-indicador-rotulo">Aulas consolidadas</div>
          <div class="historico-indicador-valor" data-testid="historico-consolidadas">
            {{ resumo.aulasConsolidadas }}
          </div>
        </div>
        <div class="historico-indicador">
          <div class="historico-indicador-rotulo">Presenças</div>
          <div class="historico-indicador-valor" data-testid="historico-presencas">
            {{ resumo.presencas }}
          </div>
        </div>
        <div class="historico-indicador">
          <div class="historico-indicador-rotulo">Faltas</div>
          <div class="historico-indicador-valor" data-testid="historico-faltas">
            {{ resumo.faltas }}
          </div>
        </div>
        <div class="historico-indicador">
          <div class="historico-indicador-rotulo">Frequência</div>
          <div class="historico-indicador-valor" data-testid="historico-percentual">
            {{ percentual }}
          </div>
        </div>
        <div class="historico-indicador">
          <div class="historico-indicador-rotulo">Registros no histórico</div>
          <div class="historico-indicador-valor" data-testid="historico-total">
            {{ historico.length }}
          </div>
        </div>
      </div>

      <InlineMessage
        texto="Os totais consideram somente aulas Consolidadas (CSU07). Registros de aulas Em aberto ou Não realizadas aparecem na lista, mas não entram na frequência."
        tipo="info"
      />

      <LoadingOverlay :loading="carregando" texto="Carregando histórico de presenças...">
        <!-- Celular: cartões, um por registro. -->
        <div v-if="compacta" class="lista-cartoes" data-testid="historico-cartoes">
          <CartaoItem
            v-for="item in visiveis"
            :key="item.aulaId"
            data-testid="historico-cartao"
          >
            <template #titulo>
              {{ formatarData(item.dataAula) }} · {{ item.departamentoNome }}
            </template>
            <template #chips>
              <Tag
                :value="item.presente ? 'Presente' : 'Ausente'"
                :severity="severityPresenca(item.presente)"
              />
              <TagSituacaoAula :situacao="item.situacaoAula" />
            </template>
            <template #meta>
              {{ item.materiaNome }}
              <template v-if="item.observacao"><br />{{ item.observacao }}</template>
            </template>
          </CartaoItem>

          <p v-if="!carregando && visiveis.length === 0" class="lista-vazia">
            Nenhum histórico de presença encontrado para esta pessoa.
          </p>

          <Button
            v-if="restantes > 0"
            :label="`Mostrar mais (${restantes} restantes)`"
            severity="secondary"
            text
            class="mostrar-mais"
            data-testid="historico-mostrar-mais"
            @click="mostrarMais"
          />
        </div>

        <!-- Desktop: tabela de sempre. -->
        <DataTable
          v-else
          :value="historico"
          paginator
          :rows="10"
          rowHover
          sortField="dataAula"
          :sortOrder="-1"
          dataKey="aulaId"
          responsiveLayout="scroll"
          data-testid="historico-tabela"
        >
          <template #empty>
            <div style="padding: 14px; opacity: 0.7">
              Nenhum histórico de presença encontrado para esta pessoa.
            </div>
          </template>
          <Column header="Data" style="width: 120px">
            <template #body="{ data }">
              {{ formatarData(data.dataAula) }}
            </template>
          </Column>

          <Column field="departamentoNome" header="Turma" sortable />
          <Column field="materiaNome" header="Matéria" sortable />

          <Column header="Presença" style="width: 120px">
            <template #body="{ data }">
              <Tag
                :value="data.presente ? 'Presente' : 'Ausente'"
                :severity="severityPresenca(data.presente)"
              />
            </template>
          </Column>

          <Column header="Aula" style="width: 170px">
            <template #body="{ data }">
              <TagSituacaoAula :situacao="data.situacaoAula" />
            </template>
          </Column>

          <Column field="observacao" header="Observação" />
        </DataTable>
      </LoadingOverlay>
    </div>

    <template #footer>
      <Button
        label="Fechar"
        severity="secondary"
        :disabled="carregando"
        data-testid="historico-fechar"
        @click="visivel = false"
      />
    </template>
  </Dialog>
</template>

<style scoped>
.historico-indicadores {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 12px;
}

.historico-indicador {
  border: 1px solid rgba(0, 0, 0, 0.08);
  border-radius: 12px;
  padding: 12px;
  min-width: 0;
}

.historico-indicador-rotulo {
  font-size: 12px;
  opacity: 0.7;
}

.historico-indicador-valor {
  font-size: 24px;
  font-weight: 700;
}
</style>
