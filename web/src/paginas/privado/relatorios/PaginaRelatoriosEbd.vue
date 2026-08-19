<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import CardIndicador from "../../../components/ui/CardIndicador.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";

import Button from "primevue/button";
import Tabs from "primevue/tabs";
import TabList from "primevue/tablist";
import Tab from "primevue/tab";
import TabPanels from "primevue/tabpanels";
import TabPanel from "primevue/tabpanel";
import Dropdown from "primevue/dropdown";
import Calendar from "primevue/calendar";
import InputNumber from "primevue/inputnumber";
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Tag from "primevue/tag";
import Chart from "primevue/chart";
import ProgressBar from "primevue/progressbar";

import { listarDepartamentos } from "../../../aplicacao/servicos/departamentosServico";
import {
  obterFrequenciaTurma,
  obterPainelAcompanhamento,
  obterRankingFaltas,
  obterResumoDia,
  type ResumoDiaVM,
} from "../../../aplicacao/servicos/relatoriosServico";

import type { DepartamentoVM } from "../../../aplicacao/modelos/dtos";

type FrequenciaVM = Awaited<ReturnType<typeof obterFrequenciaTurma>>;
type AcompanhamentoVM = Awaited<ReturnType<typeof obterPainelAcompanhamento>>;
type RankingVM = Awaited<ReturnType<typeof obterRankingFaltas>>;

const { carregando, erro, run, clearErrors } = useAsync();

const turmas = ref<DepartamentoVM[]>([]);
const turmaSelecionadaId = ref<number | null>(null);

const dataInicio = ref<Date | null>(null);
const dataFim = ref<Date | null>(null);

const autenticacao = usarAutenticacaoStore();
const isAdministrativo = computed(() => autenticacao.isAdministrativo);

const resumo = ref<ResumoDiaVM | null>(null);
const dataResumo = ref<Date | null>(new Date());

const abaAtiva = ref<string>("frequencia");

const frequencia = ref<FrequenciaVM | null>(null);
const acompanhamento = ref<AcompanhamentoVM | null>(null);
const ranking = ref<RankingVM | null>(null);

const tabelaAlunosRef = ref();
const tabelaAulasRef = ref();
const tabelaAcompanhamentoRef = ref();
const tabelaRankingRef = ref();
const tabelaResumoRef = ref();

const topRanking = ref<number>(10);

const limiarAtencao = ref<number>(75);
const faltasConsecutivasCritico = ref<number>(3);

const CORES = {
  verde: "#234f32",
  verdeBg: "#edf5f0",
  sucesso: "#2e7d4a",
  alerta: "#b45309",
  perigo: "#b83232",
};

function corClassificacao(c: string): "danger" | "warn" {
  return c === "Critico" ? "danger" : "warn";
}

function rotuloClassificacao(c: string): string {
  return c === "Critico" ? "Crítico" : "Atenção";
}

function formatarPercentual(v: number | null | undefined): string {
  const n = Number(v ?? 0);
  return `${n.toLocaleString("pt-BR", { maximumFractionDigits: 1 })}%`;
}

function toYmd(d: Date | null): string | undefined {
  if (!d) return undefined;
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
}

function formatarData(valor?: string | null) {
  if (!valor) return "-";
  const d = new Date(valor);
  return Number.isNaN(d.getTime()) ? "-" : d.toLocaleDateString("pt-BR");
}

function preencherPeriodoPadrao() {
  const hoje = new Date();
  const trintaDiasAtras = new Date();
  trintaDiasAtras.setDate(hoje.getDate() - 30);

  dataInicio.value = trintaDiasAtras;
  dataFim.value = hoje;
}

const nomeTurmaSelecionada = computed(
  () => turmas.value.find((t) => t.id === turmaSelecionadaId.value)?.nome ?? "",
);

const periodoFormatado = computed(() => {
  const inicio = dataInicio.value
    ? dataInicio.value.toLocaleDateString("pt-BR")
    : "-";
  const fim = dataFim.value ? dataFim.value.toLocaleDateString("pt-BR") : "-";
  return `${inicio} a ${fim}`;
});

function nomeArquivo(prefixo: string): string {
  const nome = (nomeTurmaSelecionada.value || "turma")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/\s+/g, "-")
    .toLowerCase();
  return `${prefixo}-${nome}`;
}

const filtroValido = computed(() => {
  if (!turmaSelecionadaId.value) return false;
  if (dataInicio.value && dataFim.value)
    return dataFim.value >= dataInicio.value;
  return true;
});

const podeBuscar = computed(() =>
  abaAtiva.value === "resumo" ? !!dataResumo.value : filtroValido.value,
);

const dadosGraficoAulas = computed(() => {
  const aulas = [...(frequencia.value?.aulas ?? [])]
    .filter(
      (a: any) =>
        Number(a.presentes ?? 0) + Number(a.ausentesMarcados ?? 0) > 0,
    )
    .sort(
      (a: any, b: any) =>
        new Date(a.data).getTime() - new Date(b.data).getTime(),
    );
  return {
    labels: aulas.map((a: any) => formatarData(a.data)),
    datasets: [
      {
        label: "% de presença",
        data: aulas.map((a: any) => Number(a.percentualPresenca ?? 0)),
        borderColor: CORES.verde,
        backgroundColor: "rgba(35, 79, 50, 0.12)",
        pointBackgroundColor: CORES.verde,
        pointRadius: 4,
        fill: true,
        tension: 0.3,
      },
    ],
  };
});

const opcoesGraficoAulas: any = {
  responsive: true,
  maintainAspectRatio: false,
  scales: {
    y: {
      min: 0,
      max: 100,
      ticks: { callback: (v: any) => `${v}%` },
      grid: { color: "#eeeeee" },
    },
    x: { grid: { display: false } },
  },
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (ctx: any) => ` ${formatarPercentual(ctx.parsed.y)} de presença`,
      },
    },
  },
};

const dadosGraficoSituacao = computed(() => {
  const painel = acompanhamento.value;
  if (!painel) return null;

  const regulares = Math.max(
    0,
    painel.totalAlunos - painel.totalCritico - painel.totalAtencao,
  );

  return {
    labels: ["Crítico", "Atenção", "Regulares"],
    datasets: [
      {
        data: [painel.totalCritico, painel.totalAtencao, regulares],
        backgroundColor: [CORES.perigo, CORES.alerta, CORES.sucesso],
        borderColor: "#ffffff",
        borderWidth: 2,
      },
    ],
  };
});

const opcoesGraficoSituacao: any = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: "62%",
  plugins: { legend: { position: "bottom" } },
};

const itensRankingOrdenados = computed(() =>
  [...(ranking.value?.itens ?? [])].sort(
    (a: any, b: any) =>
      Number(b.faltasTotais ?? 0) - Number(a.faltasTotais ?? 0),
  ),
);

const dadosGraficoRanking = computed(() => ({
  labels: itensRankingOrdenados.value.map((i: any) => i.nomeAluno),
  datasets: [
    {
      label: "Faltas no período",
      data: itensRankingOrdenados.value.map((i: any) =>
        Number(i.faltasTotais ?? 0),
      ),
      backgroundColor: "rgba(184, 50, 50, 0.75)",
      borderRadius: 4,
    },
  ],
}));

const opcoesGraficoRanking: any = {
  indexAxis: "y",
  responsive: true,
  maintainAspectRatio: false,
  scales: {
    x: { ticks: { precision: 0 }, grid: { color: "#eeeeee" } },
    y: { grid: { display: false } },
  },
  plugins: { legend: { display: false } },
};

const alturaGraficoRanking = computed(() => {
  const quantidade = itensRankingOrdenados.value.length;
  return `${Math.max(180, quantidade * 34 + 60)}px`;
});

async function carregarTurmas() {
  await run(async () => {
    turmas.value = await listarDepartamentos();

    if (!turmaSelecionadaId.value) {
      const primeiraAtiva = turmas.value.find((t) => t.ativo);
      turmaSelecionadaId.value =
        primeiraAtiva?.id ?? turmas.value[0]?.id ?? null;
    }
  }, "Não foi possível carregar as turmas.");
}

async function buscarFrequencia() {
  await run(async () => {
    frequencia.value = await obterFrequenciaTurma({
      departamentoId: turmaSelecionadaId.value as number,
      dataInicio: toYmd(dataInicio.value),
      dataFim: toYmd(dataFim.value),
    });
  }, "Não foi possível carregar a frequência.");
}

async function buscarAcompanhamento() {
  await run(async () => {
    acompanhamento.value = await obterPainelAcompanhamento({
      departamentoId: turmaSelecionadaId.value as number,
      dataInicio: toYmd(dataInicio.value),
      dataFim: toYmd(dataFim.value),
      limiarAtencao: limiarAtencao.value,
      faltasConsecutivasCritico: faltasConsecutivasCritico.value,
    });
  }, "Não foi possível carregar o acompanhamento.");
}

async function buscarRanking() {
  await run(async () => {
    ranking.value = await obterRankingFaltas({
      departamentoId: turmaSelecionadaId.value as number,
      dataInicio: toYmd(dataInicio.value),
      dataFim: toYmd(dataFim.value),
      top: topRanking.value,
    });
  }, "Não foi possível carregar o ranking de faltas.");
}

async function buscarResumo() {
  const dia = toYmd(dataResumo.value);
  if (!dia) {
    erro.value = "Informe a data do resumo.";
    return;
  }
  await run(async () => {
    resumo.value = await obterResumoDia(dia);
  }, "Não foi possível carregar o resumo do dia.");
}

function buscar() {
  clearErrors();
  if (abaAtiva.value === "resumo") return buscarResumo();
  if (!filtroValido.value) {
    erro.value = "Selecione uma turma e verifique o período.";
    return;
  }
  if (abaAtiva.value === "acompanhamento") return buscarAcompanhamento();
  if (abaAtiva.value === "ranking") return buscarRanking();
  return buscarFrequencia();
}

function carregarAbaSeVazia() {
  if (abaAtiva.value === "resumo") {
    if (!resumo.value) buscarResumo();
    return;
  }
  if (!filtroValido.value) return;
  if (abaAtiva.value === "frequencia" && !frequencia.value) {
    buscarFrequencia();
  } else if (abaAtiva.value === "acompanhamento" && !acompanhamento.value) {
    buscarAcompanhamento();
  } else if (abaAtiva.value === "ranking" && !ranking.value) {
    buscarRanking();
  }
}

watch(abaAtiva, () => {
  clearErrors();
  carregarAbaSeVazia();
});

watch(turmaSelecionadaId, () => {
  frequencia.value = null;
  acompanhamento.value = null;
  ranking.value = null;
  clearErrors();
  carregarAbaSeVazia();
});

function exportarCsv(tabela: { exportCSV?: () => void } | undefined) {
  tabela?.exportCSV?.();
}

function imprimir() {
  window.print();
}

onMounted(async () => {
  preencherPeriodoPadrao();
  await carregarTurmas();
});
</script>

<template>
  <div class="page-container pagina-relatorios">
    <PageHeader
      titulo="Relatórios EBD"
      subtitulo="Frequência da turma, acompanhamento, ranking de faltas e resumo do dia"
    >
      <template #acoes>
        <div class="acoes-cabecalho nao-imprimir">
          <Button
            label="Imprimir"
            icon="pi pi-print"
            outlined
            severity="secondary"
            :disabled="carregando"
            @click="imprimir"
          />
          <Button
            label="Buscar"
            icon="pi pi-search"
            :loading="carregando"
            :disabled="!podeBuscar"
            @click="buscar"
          />
        </div>
      </template>
    </PageHeader>

    <div class="apenas-impressao cabecalho-impressao">
      <strong>KoinoniaHub — Relatórios EBD</strong>
      <span v-if="abaAtiva === 'resumo'">
        Resumo do dia ·
        {{ dataResumo ? dataResumo.toLocaleDateString("pt-BR") : "-" }}
      </span>
      <span v-else>
        {{ nomeTurmaSelecionada || "Turma não selecionada" }} ·
        {{ periodoFormatado }}
      </span>
    </div>

    <InlineMessage :texto="erro" tipo="erro" />

    <div class="filtros-relatorios nao-imprimir">
      <template v-if="abaAtiva !== 'resumo'">
        <div class="filtro-campo filtro-turma">
          <label>Turma</label>
          <Dropdown
            v-model="turmaSelecionadaId"
            :options="turmas"
            optionLabel="nome"
            optionValue="id"
            placeholder="Selecione..."
            filter
          />
        </div>

        <div class="filtro-campo">
          <label>Data início</label>
          <Calendar v-model="dataInicio" dateFormat="dd/mm/yy" showIcon />
        </div>

        <div class="filtro-campo">
          <label>Data fim</label>
          <Calendar v-model="dataFim" dateFormat="dd/mm/yy" showIcon />
        </div>
      </template>

      <template v-if="abaAtiva === 'resumo'">
        <div class="filtro-campo">
          <label>Data</label>
          <Calendar v-model="dataResumo" dateFormat="dd/mm/yy" showIcon />
        </div>
      </template>

      <template v-if="abaAtiva === 'acompanhamento'">
        <div class="filtro-campo">
          <label>Mín. frequência (%)</label>
          <InputNumber
            v-model="limiarAtencao"
            :min="0"
            :max="100"
            suffix=" %"
          />
        </div>
        <div class="filtro-campo">
          <label>Faltas seguidas</label>
          <InputNumber v-model="faltasConsecutivasCritico" :min="1" :max="50" />
        </div>
      </template>

      <template v-if="abaAtiva === 'ranking'">
        <div class="filtro-campo">
          <label>Qtd. no ranking (top)</label>
          <InputNumber v-model="topRanking" :min="1" :max="100" />
        </div>
      </template>
    </div>

    <LoadingOverlay :loading="carregando" texto="Gerando relatório...">
      <Tabs v-model:value="abaAtiva">
        <TabList class="nao-imprimir">
          <Tab value="frequencia">Frequência da turma</Tab>
          <Tab value="acompanhamento">Acompanhamento</Tab>
          <Tab value="ranking">Ranking de faltas</Tab>
          <Tab v-if="isAdministrativo" value="resumo">Resumo do dia</Tab>
        </TabList>

        <TabPanels>
          <TabPanel value="frequencia">
            <div v-if="frequencia" class="conteudo-relatorio">
              <div class="linha-indicadores">
                <CardIndicador
                  rotulo="Aulas no período"
                  :valor="frequencia.totalAulas"
                  icone="pi pi-calendar"
                />
                <CardIndicador
                  rotulo="Alunos"
                  :valor="frequencia.totalAlunos"
                  icone="pi pi-users"
                />
                <CardIndicador
                  rotulo="Presenças"
                  :valor="frequencia.totalPresentes"
                  icone="pi pi-check-circle"
                  tom="sucesso"
                />
                <CardIndicador
                  rotulo="% Presença geral"
                  :valor="
                    formatarPercentual(frequencia.percentualPresencaGeral)
                  "
                  icone="pi pi-chart-line"
                />
              </div>

              <div v-if="frequencia.aulas.length" class="card-bloco">
                <h3 class="titulo-secao">Evolução da presença por aula</h3>
                <div class="grafico-area">
                  <Chart
                    type="line"
                    class="grafico"
                    :data="dadosGraficoAulas"
                    :options="opcoesGraficoAulas"
                  />
                </div>
              </div>

              <div class="card-bloco">
                <div class="secao-cabecalho">
                  <h3 class="titulo-secao">Alunos</h3>
                  <Button
                    class="nao-imprimir"
                    label="CSV"
                    icon="pi pi-download"
                    text
                    size="small"
                    @click="exportarCsv(tabelaAlunosRef)"
                  />
                </div>
                <DataTable
                  ref="tabelaAlunosRef"
                  :value="frequencia.alunos"
                  paginator
                  :rows="10"
                  rowHover
                  dataKey="pessoaId"
                  responsiveLayout="scroll"
                  :exportFilename="nomeArquivo('frequencia-alunos')"
                >
                  <Column field="nomeAluno" header="Aluno" sortable />
                  <Column
                    field="presentes"
                    header="Presenças"
                    style="width: 120px"
                    sortable
                  />
                  <Column
                    field="ausentesMarcados"
                    header="Faltas"
                    style="width: 110px"
                    sortable
                  />
                  <Column
                    field="naoRegistrado"
                    header="Não registrado"
                    style="width: 140px"
                    sortable
                  />
                  <Column
                    field="percentualPresenca"
                    header="% Presença"
                    style="width: 220px"
                    sortable
                  >
                    <template #body="{ data }">
                      <div class="celula-presenca">
                        <ProgressBar
                          class="barra-presenca"
                          :value="Number(data.percentualPresenca ?? 0)"
                          :showValue="false"
                        />
                        <span>{{
                          formatarPercentual(data.percentualPresenca)
                        }}</span>
                      </div>
                    </template>
                  </Column>
                </DataTable>
              </div>

              <div class="card-bloco">
                <div class="secao-cabecalho">
                  <h3 class="titulo-secao">Aulas</h3>
                  <Button
                    class="nao-imprimir"
                    label="CSV"
                    icon="pi pi-download"
                    text
                    size="small"
                    @click="exportarCsv(tabelaAulasRef)"
                  />
                </div>
                <DataTable
                  ref="tabelaAulasRef"
                  :value="frequencia.aulas"
                  paginator
                  :rows="10"
                  rowHover
                  dataKey="aulaId"
                  responsiveLayout="scroll"
                  :exportFilename="nomeArquivo('frequencia-aulas')"
                >
                  <Column
                    field="data"
                    header="Data"
                    style="width: 120px"
                    sortable
                  >
                    <template #body="{ data }">
                      {{ formatarData(data.data) }}
                    </template>
                  </Column>
                  <Column field="tema" header="Tema" />
                  <Column
                    field="presentes"
                    header="Presentes"
                    style="width: 120px"
                    sortable
                  />
                  <Column
                    field="ausentesMarcados"
                    header="Ausentes"
                    style="width: 110px"
                    sortable
                  />
                  <Column
                    field="naoRegistrado"
                    header="Não registrado"
                    style="width: 150px"
                    sortable
                  />
                  <Column
                    field="percentualPresenca"
                    header="% Presença"
                    style="width: 130px"
                    sortable
                  >
                    <template #body="{ data }">
                      {{ formatarPercentual(data.percentualPresenca) }}
                    </template>
                  </Column>
                </DataTable>
              </div>
            </div>

            <InlineMessage
              v-else
              texto="Selecione uma turma para gerar o relatório de frequência."
              tipo="info"
            />
          </TabPanel>

          <TabPanel value="acompanhamento">
            <div v-if="acompanhamento" class="conteudo-relatorio">
              <div class="linha-indicadores">
                <CardIndicador
                  rotulo="Críticos"
                  :valor="acompanhamento.totalCritico"
                  icone="pi pi-exclamation-triangle"
                  tom="perigo"
                />
                <CardIndicador
                  rotulo="Em atenção"
                  :valor="acompanhamento.totalAtencao"
                  icone="pi pi-flag"
                  tom="alerta"
                />
                <CardIndicador
                  rotulo="Alunos analisados"
                  :valor="acompanhamento.totalAlunos"
                  icone="pi pi-users"
                />
                <CardIndicador
                  rotulo="Aulas no período"
                  :valor="acompanhamento.totalAulas"
                  icone="pi pi-calendar"
                />
              </div>

              <div
                v-if="dadosGraficoSituacao && acompanhamento.totalAlunos > 0"
                class="card-bloco"
              >
                <h3 class="titulo-secao">Distribuição da turma por situação</h3>
                <div class="grafico-area grafico-rosca">
                  <Chart
                    type="doughnut"
                    class="grafico"
                    :data="dadosGraficoSituacao"
                    :options="opcoesGraficoSituacao"
                  />
                </div>
              </div>

              <InlineMessage
                v-if="acompanhamento.alunos.length === 0"
                texto="Nenhum aluno em situação de atenção no período."
                tipo="sucesso"
              />

              <div v-else class="card-bloco">
                <div class="secao-cabecalho">
                  <h3 class="titulo-secao">Alunos em atenção</h3>
                  <Button
                    class="nao-imprimir"
                    label="CSV"
                    icon="pi pi-download"
                    text
                    size="small"
                    @click="exportarCsv(tabelaAcompanhamentoRef)"
                  />
                </div>
                <DataTable
                  ref="tabelaAcompanhamentoRef"
                  :value="acompanhamento.alunos"
                  paginator
                  :rows="10"
                  rowHover
                  dataKey="matriculaId"
                  responsiveLayout="scroll"
                  :exportFilename="nomeArquivo('acompanhamento')"
                >
                  <Column field="nomeAluno" header="Aluno" sortable />
                  <Column
                    field="classificacao"
                    header="Situação"
                    style="width: 120px"
                    sortable
                  >
                    <template #body="{ data }">
                      <Tag
                        :severity="corClassificacao(data.classificacao)"
                        :value="rotuloClassificacao(data.classificacao)"
                      />
                    </template>
                  </Column>
                  <Column
                    field="percentualPresenca"
                    header="% Presença"
                    style="width: 120px"
                    sortable
                  >
                    <template #body="{ data }">
                      {{ formatarPercentual(data.percentualPresenca) }}
                    </template>
                  </Column>
                  <Column
                    field="faltasConsecutivas"
                    header="Faltas seguidas"
                    style="width: 140px"
                    sortable
                  />
                  <Column
                    field="dataUltimaPresenca"
                    header="Última presença"
                    style="width: 140px"
                    sortable
                  >
                    <template #body="{ data }">
                      {{ formatarData(data.dataUltimaPresenca) }}
                    </template>
                  </Column>
                  <Column field="motivos" header="Motivos">
                    <template #body="{ data }">
                      {{ (data.motivos || []).join("; ") }}
                    </template>
                  </Column>
                </DataTable>
              </div>
            </div>

            <InlineMessage
              v-else
              texto="Selecione uma turma para gerar o painel de acompanhamento."
              tipo="info"
            />
          </TabPanel>

          <TabPanel v-if="isAdministrativo" value="resumo">
            <div v-if="resumo" class="conteudo-relatorio">
              <div class="linha-indicadores">
                <CardIndicador
                  rotulo="Presentes"
                  :valor="resumo.totalPresentes"
                  icone="pi pi-check-circle"
                />
                <CardIndicador
                  rotulo="Ausentes"
                  :valor="resumo.totalAusentes"
                  icone="pi pi-times-circle"
                />
                <CardIndicador
                  rotulo="Visitantes"
                  :valor="resumo.totalVisitantes"
                  icone="pi pi-user-plus"
                />
                <CardIndicador
                  rotulo="Geral do dia"
                  :valor="resumo.totalPresentes + resumo.totalVisitantes"
                  icone="pi pi-users"
                />
              </div>

              <InlineMessage
                v-if="!resumo.turmas.some((t) => t.temChamada)"
                texto="Nenhuma chamada registrada nesta data."
                tipo="info"
              />

              <div class="card-bloco">
                <div class="secao-cabecalho">
                  <h3 class="titulo-secao">Resumo por turma</h3>
                  <Button
                    class="nao-imprimir"
                    label="CSV"
                    icon="pi pi-download"
                    text
                    size="small"
                    @click="exportarCsv(tabelaResumoRef)"
                  />
                </div>
                <DataTable
                  ref="tabelaResumoRef"
                  :value="resumo.turmas"
                  stripedRows
                  exportFilename="resumo-do-dia"
                >
                  <Column field="nome" header="Turma" />
                  <Column header="Chamada" style="width: 120px">
                    <template #body="{ data }">{{
                      data.temChamada ? "Sim" : "Não"
                    }}</template>
                  </Column>
                  <Column
                    field="presentes"
                    header="Presentes"
                    style="width: 120px"
                  />
                  <Column
                    field="ausentes"
                    header="Ausentes"
                    style="width: 120px"
                  />
                  <Column
                    field="visitantes"
                    header="Visitantes"
                    style="width: 120px"
                  />
                </DataTable>
              </div>
            </div>

            <InlineMessage
              v-else
              texto="Selecione a data e clique em Buscar para gerar o resumo do dia."
              tipo="info"
            />
          </TabPanel>

          <TabPanel value="ranking">
            <div v-if="ranking" class="conteudo-relatorio">
              <InlineMessage
                v-if="ranking.itens.length === 0"
                texto="Nenhuma falta registrada no período para esta turma."
                tipo="sucesso"
              />

              <template v-else>
                <div class="card-bloco">
                  <h3 class="titulo-secao">
                    Alunos com mais faltas no período
                  </h3>
                  <div
                    class="grafico-area"
                    :style="{ height: alturaGraficoRanking }"
                  >
                    <Chart
                      type="bar"
                      class="grafico"
                      :data="dadosGraficoRanking"
                      :options="opcoesGraficoRanking"
                    />
                  </div>
                </div>

                <div class="card-bloco">
                  <div class="secao-cabecalho">
                    <h3 class="titulo-secao">Detalhamento</h3>
                    <Button
                      class="nao-imprimir"
                      label="CSV"
                      icon="pi pi-download"
                      text
                      size="small"
                      @click="exportarCsv(tabelaRankingRef)"
                    />
                  </div>
                  <DataTable
                    ref="tabelaRankingRef"
                    :value="itensRankingOrdenados"
                    paginator
                    :rows="10"
                    rowHover
                    dataKey="matriculaId"
                    responsiveLayout="scroll"
                    :exportFilename="nomeArquivo('ranking-faltas')"
                  >
                    <Column header="#" style="width: 70px" :exportable="false">
                      <template #body="{ index }">
                        {{ index + 1 }}
                      </template>
                    </Column>
                    <Column field="nomeAluno" header="Aluno" sortable />
                    <Column
                      field="faltasTotais"
                      header="Faltas"
                      style="width: 110px"
                      sortable
                    />
                    <Column
                      field="presentes"
                      header="Presenças"
                      style="width: 120px"
                      sortable
                    />
                    <Column
                      field="totalAulas"
                      header="Aulas"
                      style="width: 100px"
                      sortable
                    />
                    <Column
                      field="percentualPresenca"
                      header="% Presença"
                      style="width: 220px"
                      sortable
                    >
                      <template #body="{ data }">
                        <div class="celula-presenca">
                          <ProgressBar
                            class="barra-presenca"
                            :value="Number(data.percentualPresenca ?? 0)"
                            :showValue="false"
                          />
                          <span>{{
                            formatarPercentual(data.percentualPresenca)
                          }}</span>
                        </div>
                      </template>
                    </Column>
                  </DataTable>
                </div>
              </template>
            </div>

            <InlineMessage
              v-else
              texto="Selecione uma turma para gerar o ranking de faltas."
              tipo="info"
            />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </LoadingOverlay>
  </div>
</template>

<style scoped>
.acoes-cabecalho {
  display: flex;
  gap: 8px;
}

.filtros-relatorios {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
  align-items: flex-end;
}

.filtro-campo {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 150px;
}

.filtro-turma {
  min-width: 260px;
}

.conteudo-relatorio {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.linha-indicadores {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.card-bloco {
  background: var(--ipb-branco, #fff);
  border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  border-radius: 12px;
  padding: 14px 16px;
}

.titulo-secao {
  margin: 0 0 10px;
  font-size: 15px;
  color: var(--ipb-verde-escuro, #1a3b25);
}

.secao-cabecalho {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.secao-cabecalho .titulo-secao {
  margin: 0;
}

.grafico-area {
  position: relative;
  height: 280px;
}

.grafico-rosca {
  height: 260px;
  max-width: 480px;
  margin: 0 auto;
}

.grafico {
  height: 100%;
}

.celula-presenca {
  display: flex;
  align-items: center;
  gap: 10px;
}

.celula-presenca span {
  min-width: 52px;
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.barra-presenca {
  flex: 1;
}

:deep(.barra-presenca.p-progressbar) {
  height: 8px;
  border-radius: 6px;
  background: var(--ipb-verde-bg, #edf5f0);
}

:deep(.barra-presenca .p-progressbar-value) {
  background: var(--ipb-verde, #234f32);
}

.apenas-impressao {
  display: none;
}
</style>

<style>
@media print {
  .sidebar-ipb,
  .header-ipb,
  .footer-ipb,
  .nao-imprimir,
  .p-tablist,
  .p-paginator,
  .p-toast {
    display: none !important;
  }

  .layout,
  .corpo,
  .conteudo {
    display: block !important;
    padding: 0 !important;
    margin: 0 !important;
  }

  body {
    background: #fff !important;
  }

  .pagina-relatorios .apenas-impressao {
    display: flex !important;
  }

  .pagina-relatorios .cabecalho-impressao {
    justify-content: space-between;
    align-items: baseline;
    gap: 12px;
    padding-bottom: 8px;
    margin-bottom: 10px;
    border-bottom: 1px solid #ccc;
  }

  .pagina-relatorios .card-bloco,
  .pagina-relatorios .card-indicador {
    break-inside: avoid;
    border-color: #ccc;
  }

  .pagina-relatorios canvas {
    max-width: 100% !important;
    height: auto !important;
  }

  .pagina-relatorios .grafico-area {
    height: auto !important;
    overflow: visible !important;
  }
}
</style>
