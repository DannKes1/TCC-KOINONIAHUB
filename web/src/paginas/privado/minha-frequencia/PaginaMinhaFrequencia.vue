<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute } from "vue-router";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import CardIndicador from "../../../components/ui/CardIndicador.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";

import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Tag from "primevue/tag";
import ProgressBar from "primevue/progressbar";

import {
  obterMinhaFrequencia,
  type MinhaFrequenciaTurmaVM,
} from "../../../aplicacao/servicos/meusDadosServico";

const route = useRoute();
const { carregando, erro, run } = useAsync();

const departamentoId = computed(() => Number(route.params.departamentoId));
const dados = ref<MinhaFrequenciaTurmaVM | null>(null);

function formatarData(valor?: string | null) {
  if (!valor) return "-";
  const d = new Date(valor);
  if (Number.isNaN(d.getTime())) return "-";
  return d.toLocaleDateString("pt-BR");
}

function corSituacao(s: string): "success" | "danger" | "secondary" {
  if (s === "Presente") return "success";
  if (s === "Ausente") return "danger";
  return "secondary"; // Não registrado
}

/** Aulas em que a chamada foi lançada (base do percentual). */
const aulasComChamada = computed(() => {
  if (!dados.value) return 0;
  return Number(dados.value.presentes) + Number(dados.value.ausentesMarcados);
});

const percentual = computed(() => Number(dados.value?.percentualPresenca ?? 0));

/** Verde a partir de 75%, alerta entre 50% e 75%, vermelho abaixo disso. */
const tomPresenca = computed<"padrao" | "sucesso" | "alerta" | "perigo">(() => {
  if (aulasComChamada.value === 0) return "padrao";
  if (percentual.value >= 75) return "sucesso";
  if (percentual.value >= 50) return "alerta";
  return "perigo";
});

const tomFaltas = computed<"padrao" | "perigo">(() =>
  Number(dados.value?.ausentesMarcados ?? 0) > 0 ? "perigo" : "padrao",
);

async function carregar() {
  if (!departamentoId.value) {
    erro.value = "Turma inválida.";
    return;
  }

  await run(async () => {
    dados.value = await obterMinhaFrequencia(departamentoId.value);
  }, "Não foi possível carregar sua frequência.");
}

onMounted(carregar);
</script>

<template>
  <div class="pagina-minha-frequencia">
    <PageHeader
      titulo="Minha frequência"
      :subtitulo="dados ? dados.nomeDepartamento : 'Sua presença nesta turma'"
      voltarPara="/"
      voltarLabel="Início"
    />

    <InlineMessage v-if="erro" :texto="erro" tipo="erro" />

    <LoadingOverlay :loading="carregando" texto="Carregando sua frequência...">
      <div v-if="dados" class="conteudo">
        <div class="grade-indicadores">
          <CardIndicador
            rotulo="% Presença"
            :valor="`${percentual.toFixed(2)}%`"
            icone="pi pi-chart-line"
            :tom="tomPresenca"
          />
          <CardIndicador
            rotulo="Presenças"
            :valor="dados.presentes"
            icone="pi pi-check-circle"
            tom="sucesso"
          />
          <CardIndicador
            rotulo="Faltas"
            :valor="dados.ausentesMarcados"
            icone="pi pi-times-circle"
            :tom="tomFaltas"
          />
          <CardIndicador
            rotulo="Não registrado"
            :valor="dados.naoRegistrado"
            icone="pi pi-minus-circle"
          />
          <CardIndicador
            rotulo="Aulas no período"
            :valor="dados.totalAulas"
            icone="pi pi-calendar"
          />
        </div>

        <div v-if="aulasComChamada > 0" class="card-bloco">
          <h3 class="titulo-secao">Frequência na turma</h3>
          <div class="linha-progresso">
            <ProgressBar
              class="barra-frequencia"
              :value="percentual"
              :showValue="false"
            />
            <span class="valor-progresso">{{ percentual.toFixed(2) }}%</span>
          </div>
          <p class="legenda">
            {{ dados.presentes }} presença(s) em {{ aulasComChamada }} aula(s)
            com chamada lançada.
            <template v-if="dados.naoRegistrado > 0">
              Outras {{ dados.naoRegistrado }} aula(s) ainda não tiveram a
              chamada registrada e não entram neste cálculo.
            </template>
          </p>
        </div>

        <div class="card-bloco">
          <h3 class="titulo-secao">Histórico de aulas</h3>

          <InlineMessage
            v-if="dados.aulas.length === 0"
            texto="Nenhuma aula registrada neste período."
            tipo="info"
          />

          <DataTable
            v-else
            :value="dados.aulas"
            paginator
            :rows="10"
            rowHover
            dataKey="aulaId"
            responsiveLayout="scroll"
          >
            <Column header="Data" style="width: 140px">
              <template #body="{ data }">
                {{ formatarData(data.data) }}
              </template>
            </Column>
            <Column field="tema" header="Tema">
              <template #body="{ data }">
                {{ data.tema || "-" }}
              </template>
            </Column>
            <Column header="Situação" style="width: 160px">
              <template #body="{ data }">
                <Tag
                  :severity="corSituacao(data.situacao)"
                  :value="data.situacao"
                />
              </template>
            </Column>
          </DataTable>
        </div>
      </div>
    </LoadingOverlay>
  </div>
</template>

<style scoped>
.conteudo {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.grade-indicadores {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
}

.card-bloco {
  background: var(--ipb-branco, #fff);
  border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  border-radius: 12px;
  padding: 14px 16px;
}

.titulo-secao {
  margin: 0 0 12px;
  font-size: 15px;
  color: var(--ipb-verde-escuro, #1a3b25);
}

.linha-progresso {
  display: flex;
  align-items: center;
  gap: 12px;
}

.barra-frequencia {
  flex: 1;
}

:deep(.barra-frequencia.p-progressbar) {
  height: 10px;
  border-radius: 6px;
  background: var(--ipb-verde-bg, #edf5f0);
}

:deep(.barra-frequencia .p-progressbar-value) {
  background: var(--ipb-verde, #234f32);
}

.valor-progresso {
  min-width: 64px;
  text-align: right;
  font-size: 15px;
  font-weight: 600;
  color: var(--ipb-verde-escuro, #1a3b25);
  font-variant-numeric: tabular-nums;
}

.legenda {
  margin: 10px 0 0;
  font-size: 13px;
  line-height: 1.5;
  color: var(--ipb-cinza-claro, #7a7a7a);
}

@media (max-width: 640px) {
  .grade-indicadores {
    grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  }
}
</style>
