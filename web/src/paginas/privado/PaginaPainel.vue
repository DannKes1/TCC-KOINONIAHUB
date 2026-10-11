<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";

import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";

import PageHeader from "../../components/ui/PageHeader.vue";
import InlineMessage from "../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../components/ui/LoadingOverplay.vue";

import { useAsync } from "../../aplicacao/composables/useAsync";

import Button from "primevue/button";
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Tag from "primevue/tag";
import TagSituacaoAula from "../../components/ui/TagSituacaoAula.vue";
import CartaoItem from "../../components/ui/CartaoItem.vue";
import { usarTelaCompacta } from "../../aplicacao/composables/usarTelaCompacta";

import { listarDepartamentos } from "../../aplicacao/servicos/departamentosServico";
import { listarPessoas } from "../../aplicacao/servicos/pessoasServico";
import { listarAulasPorDepartamento } from "../../aplicacao/servicos/aulasServico";
import { listarHistoricoPresencasDaPessoa } from "../../aplicacao/servicos/presencasPessoaServico";
import {
  listarMinhasTurmas,
  type MinhaTurmaVM,
} from "../../aplicacao/servicos/meusDadosServico";

import type {
  DepartamentoVM,
  PessoaVM,
  AulaVM,
  HistoricoPresencaPessoaVM,
} from "../../aplicacao/modelos/dtos";
import {
  JANELA_PADRAO_DIAS,
  filtrarJanela,
  formatarPercentual,
  resumirPresencas,
} from "../../aplicacao/dominio/indicadoresPresenca";

type AulaComTurmaVM = AulaVM & {
  departamentoId: number;
  nomeDepartamento: string;
};

type ResumoTurmaVM = {
  departamentoId: number;
  nomeDepartamento: string;
  totalAulas: number;
  aulasAbertas: number;
  ultimaAulaData: string | null;
};

const autenticacao = usarAutenticacaoStore();
const router = useRouter();
const { carregando, erro, run } = useAsync();

// Etapa 6.4: no celular, as listas do painel do professor viram cartões com a
// ação principal nomeada (Fazer chamada / Abrir turma).
const compacta = usarTelaCompacta();

const perfilLower = computed(() =>
  (autenticacao.perfil || "").trim().toLowerCase(),
);

const isAdministrativo = computed(() =>
  ["admin", "pastor", "superintendente"].includes(perfilLower.value),
);

const isProfessor = computed(() => perfilLower.value === "professor");

const pessoas = ref<PessoaVM[]>([]);
const departamentos = ref<DepartamentoVM[]>([]);
const aulasRecentes = ref<AulaComTurmaVM[]>([]);
const resumoTurmas = ref<ResumoTurmaVM[]>([]);

const historicoPresencas = ref<HistoricoPresencaPessoaVM[]>([]);
const minhasTurmas = ref<MinhaTurmaVM[]>([]);

function abrirPessoas() {
  router.push("/pessoas");
}

function abrirTurmas() {
  router.push("/departamentos");
}

function abrirRelatorios() {
  router.push("/relatorios/ebd");
}

function abrirAulasDaTurma(departamentoId: number) {
  router.push(`/departamentos/${departamentoId}/aulas`);
}

function abrirTurma(turma: MinhaTurmaVM) {
  if ((turma.vinculo || "").toLowerCase() === "aluno") {
    router.push(`/departamentos/${turma.departamentoId}/minha-frequencia`);
    return;
  }
  abrirAulasDaTurma(turma.departamentoId);
}

function abrirChamada(aulaId: number) {
  router.push(`/aulas/${aulaId}/chamada`);
}

function formatarData(valor?: string | null) {
  if (!valor) return "-";
  const data = new Date(valor);
  if (Number.isNaN(data.getTime())) return "-";
  return data.toLocaleDateString("pt-BR");
}

function severityPresenca(presente: boolean) {
  return presente ? "success" : "danger";
}

function severityVinculo(vinculo: string) {
  const v = (vinculo || "").toLowerCase();
  if (v === "aluno") return "info";
  if (v === "professor") return "success";
  if (v === "auxiliar") return "help";
  if (v === "lider") return "warning";
  return "secondary";
}

const turmasEbdAtivas = computed(() =>
  departamentos.value.filter(
    (dep) =>
      String(dep.tipo ?? "").toLowerCase() === "ebd" && Boolean(dep.ativo),
  ),
);

const totalPessoas = computed(() => pessoas.value.length);
const totalTurmasAtivas = computed(() => turmasEbdAtivas.value.length);

const totalAulas = computed(() =>
  resumoTurmas.value.reduce((acc, item) => acc + item.totalAulas, 0),
);

const totalAulasAbertas = computed(() =>
  resumoTurmas.value.reduce((acc, item) => acc + item.aulasAbertas, 0),
);

const ultimasAulasOrdenadas = computed(() =>
  [...aulasRecentes.value]
    .sort((a, b) => new Date(b.data).getTime() - new Date(a.data).getTime())
    .slice(0, 8),
);

const resumoTurmasOrdenado = computed(() =>
  [...resumoTurmas.value].sort((a, b) =>
    a.nomeDepartamento.localeCompare(b.nomeDepartamento, "pt-BR"),
  ),
);

const aulasAbertasDoProfessor = computed(() =>
  [...aulasRecentes.value]
    .filter((a) => a.situacao === "EmAberto")
    .sort((a, b) => new Date(b.data).getTime() - new Date(a.data).getTime()),
);

const totalMinhasTurmas = computed(() => departamentos.value.length);
const totalMinhasAulas = computed(() => aulasRecentes.value.length);
const totalMinhasAulasAbertas = computed(
  () => aulasRecentes.value.filter((a) => a.situacao === "EmAberto").length,
);

// RF3: "indicadores de frequência e o histórico recente de presenças". A janela é a
// mesma de Minha Frequência (RF6: últimos 90 dias) para os números baterem, e só
// registros de aulas Consolidadas contam (CSU07); o histórico mostra todos os
// registros da janela, com a situação da aula em cada linha.
const presencasRecentes = computed(() =>
  filtrarJanela(historicoPresencas.value, JANELA_PADRAO_DIAS),
);

const presencasOrdenadas = computed(() =>
  [...presencasRecentes.value].sort(
    (a, b) => new Date(b.dataAula).getTime() - new Date(a.dataAula).getTime(),
  ),
);

const resumoPresencas = computed(() => resumirPresencas(presencasRecentes.value));

const totalAulasConsolidadas = computed(
  () => resumoPresencas.value.aulasConsolidadas,
);
const totalPresencas = computed(() => resumoPresencas.value.presencas);
const totalFaltas = computed(() => resumoPresencas.value.faltas);
const percentualPresenca = computed(() =>
  formatarPercentual(resumoPresencas.value.percentual),
);

const rotuloJanela = `últimos ${JANELA_PADRAO_DIAS} dias`;

async function carregarDadosTurmas() {
  const listaDepartamentos = await listarDepartamentos();
  departamentos.value = listaDepartamentos;

  const turmasAtivas = listaDepartamentos.filter(
    (dep) =>
      String(dep.tipo ?? "").toLowerCase() === "ebd" && Boolean(dep.ativo),
  );

  const resultadosAulas = await Promise.all(
    turmasAtivas.map(async (dep) => {
      const aulas = await listarAulasPorDepartamento(dep.id);
      return { departamento: dep, aulas };
    }),
  );

  const listaAulasRecentes: AulaComTurmaVM[] = [];
  const listaResumoTurmas: ResumoTurmaVM[] = [];

  for (const item of resultadosAulas) {
    const dep = item.departamento;
    const aulas = item.aulas ?? [];

    for (const aula of aulas) {
      listaAulasRecentes.push({
        ...aula,
        departamentoId: dep.id,
        nomeDepartamento: dep.nome,
      });
    }

    const aulasOrdenadas = [...aulas].sort(
      (a, b) => new Date(b.data).getTime() - new Date(a.data).getTime(),
    );

    listaResumoTurmas.push({
      departamentoId: dep.id,
      nomeDepartamento: dep.nome,
      totalAulas: aulas.length,
      aulasAbertas: aulas.filter((aula) => aula.situacao === "EmAberto").length,
      ultimaAulaData: aulasOrdenadas[0]?.data ?? null,
    });
  }

  aulasRecentes.value = listaAulasRecentes;
  resumoTurmas.value = listaResumoTurmas;
}

async function carregarPainelAdministrativo() {
  await run(async () => {
    pessoas.value = await listarPessoas();
    await carregarDadosTurmas();
  }, "Não foi possível carregar o painel.");
}

async function carregarPainelProfessor() {
  await run(async () => {
    await carregarDadosTurmas();
  }, "Não foi possível carregar o painel.");
}

async function carregarPainelUsuario() {
  await run(async () => {
    const pessoaId = autenticacao.pessoaId;

    if (!pessoaId) {
      throw new Error(
        "Seu usuário não está vinculado a uma pessoa. Procure o administrador.",
      );
    }

    const [historico, turmas] = await Promise.all([
      listarHistoricoPresencasDaPessoa(pessoaId),
      listarMinhasTurmas(),
    ]);

    historicoPresencas.value = historico;
    minhasTurmas.value = turmas;
  }, "Não foi possível carregar suas informações.");
}

async function carregarPainel() {
  if (isAdministrativo.value) {
    await carregarPainelAdministrativo();
  } else if (isProfessor.value) {
    await carregarPainelProfessor();
  } else {
    await carregarPainelUsuario();
  }
}

onMounted(carregarPainel);
</script>

<template>
  <div class="page-container">
    <template v-if="isAdministrativo">
      <PageHeader
        titulo="Painel"
        subtitulo="Visão geral do sistema Koinonia Hub"
      >
        <template #acoes>
          <Button
            label="Recarregar"
            icon="pi pi-refresh"
            severity="secondary"
            :loading="carregando"
            @click="carregarPainel"
          />
        </template>
      </PageHeader>

      <InlineMessage :texto="erro" tipo="erro" />

      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-card-label">Pessoas cadastradas</div>
          <div class="stat-card-valor">{{ totalPessoas }}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card-label">Turmas EBD ativas</div>
          <div class="stat-card-valor">{{ totalTurmasAtivas }}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card-label">Total de aulas</div>
          <div class="stat-card-valor">{{ totalAulas }}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card-label">Aulas em aberto</div>
          <div class="stat-card-valor">{{ totalAulasAbertas }}</div>
        </div>
      </div>

      <div class="acoes-grid">
        <Button
          label="Ir para Pessoas"
          icon="pi pi-users"
          severity="secondary"
          @click="abrirPessoas"
        />
        <Button
          label="Ir para Turmas"
          icon="pi pi-sitemap"
          severity="secondary"
          @click="abrirTurmas"
        />
        <Button
          label="Ir para Relatórios"
          icon="pi pi-chart-bar"
          severity="secondary"
          @click="abrirRelatorios"
        />
      </div>

      <LoadingOverlay :loading="carregando" texto="Carregando painel...">
        <div
          style="
            display: grid;
            grid-template-columns: 1.2fr 1fr;
            gap: 16px;
            align-items: start;
          "
        >
          <div class="card-tabela">
            <div class="card-tabela-titulo">Últimas aulas</div>

            <DataTable
              :value="ultimasAulasOrdenadas"
              :rows="8"
              dataKey="id"
              responsiveLayout="scroll"
              emptyMessage="Nenhuma aula encontrada nas turmas ativas."
            >
              <Column header="Data" style="width: 120px">
                <template #body="{ data }">
                  {{ formatarData(data.data) }}
                </template>
              </Column>
              <Column field="nomeDepartamento" header="Turma" />
              <Column field="nomeMateria" header="Matéria" />
              <Column field="nomeProfessor" header="Professor" />
              <Column header="Situação" style="width: 200px">
                <template #body="{ data }">
                  <TagSituacaoAula
                    :situacao="data.situacao"
                    :pendenteFechamento="data.pendenteFechamento"
                  />
                </template>
              </Column>
            </DataTable>
          </div>

          <div class="card-tabela">
            <div class="card-tabela-titulo">Resumo por turma</div>

            <DataTable
              :value="resumoTurmasOrdenado"
              :rows="10"
              dataKey="departamentoId"
              responsiveLayout="scroll"
              emptyMessage="Nenhuma turma EBD ativa encontrada."
            >
              <Column field="nomeDepartamento" header="Turma" />
              <Column field="totalAulas" header="Aulas" style="width: 90px" />
              <Column
                field="aulasAbertas"
                header="Abertas"
                style="width: 100px"
              />
              <Column header="Última aula" style="width: 130px">
                <template #body="{ data }">
                  {{ formatarData(data.ultimaAulaData) }}
                </template>
              </Column>
            </DataTable>
          </div>
        </div>
      </LoadingOverlay>
    </template>

    <template v-else-if="isProfessor">
      <PageHeader
        titulo="Meu Painel"
        subtitulo="Suas turmas, aulas e chamadas pendentes"
      >
        <template #acoes>
          <Button
            label="Recarregar"
            icon="pi pi-refresh"
            severity="secondary"
            :loading="carregando"
            @click="carregarPainel"
          />
        </template>
      </PageHeader>

      <InlineMessage :texto="erro" tipo="erro" />

      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-card-label">Minhas turmas</div>
          <div class="stat-card-valor">{{ totalMinhasTurmas }}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card-label">Total de aulas</div>
          <div class="stat-card-valor">{{ totalMinhasAulas }}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card-label">Aulas em aberto</div>
          <div class="stat-card-valor">{{ totalMinhasAulasAbertas }}</div>
        </div>
      </div>

      <LoadingOverlay :loading="carregando" texto="Carregando seu painel...">
        <div
          style="
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 16px;
            align-items: start;
          "
        >
          <div class="card-tabela">
            <div class="card-tabela-titulo">
              Aulas em aberto (fazer chamada)
            </div>

            <div
              v-if="compacta"
              class="lista-cartoes"
              data-testid="painel-aulas-abertas-cartoes"
            >
              <CartaoItem
                v-for="aula in aulasAbertasDoProfessor"
                :key="aula.id"
                :titulo="formatarData(aula.data)"
                :destaque="aula.pendenteFechamento"
              >
                <template #chips>
                  <TagSituacaoAula
                    :situacao="aula.situacao"
                    :pendenteFechamento="aula.pendenteFechamento"
                  />
                </template>
                <template #meta>
                  {{ aula.nomeDepartamento }} · {{ aula.nomeMateria }}
                </template>
                <template #acoes>
                  <Button
                    label="Fazer chamada"
                    icon="pi pi-clipboard"
                    class="acao-principal"
                    data-testid="painel-fazer-chamada"
                    @click="abrirChamada(aula.id)"
                  />
                </template>
              </CartaoItem>
              <p v-if="aulasAbertasDoProfessor.length === 0" class="lista-vazia">
                Nenhuma aula em aberto no momento.
              </p>
            </div>

            <DataTable
              v-else
              :value="aulasAbertasDoProfessor"
              :rows="10"
              paginator
              dataKey="id"
              responsiveLayout="scroll"
              emptyMessage="Nenhuma aula em aberto no momento."
            >
              <Column header="Data" style="width: 110px">
                <template #body="{ data }">
                  {{ formatarData(data.data) }}
                </template>
              </Column>
              <Column field="nomeDepartamento" header="Turma" />
              <Column field="nomeMateria" header="Matéria" />
              <Column header="Ação" style="width: 130px">
                <template #body="{ data }">
                  <Button
                    label="Chamada"
                    icon="pi pi-clipboard"
                    size="small"
                    @click="abrirChamada(data.id)"
                  />
                </template>
              </Column>
            </DataTable>
          </div>

          <div class="card-tabela">
            <div class="card-tabela-titulo">Minhas turmas</div>

            <div
              v-if="compacta"
              class="lista-cartoes"
              data-testid="painel-minhas-turmas-cartoes"
            >
              <CartaoItem
                v-for="t in resumoTurmasOrdenado"
                :key="t.departamentoId"
                :titulo="t.nomeDepartamento"
              >
                <template #meta>
                  {{ t.totalAulas }} aula(s) · {{ t.aulasAbertas }} em aberto
                  <template v-if="t.ultimaAulaData">
                    · última em {{ formatarData(t.ultimaAulaData) }}
                  </template>
                </template>
                <template #acoes>
                  <Button
                    label="Abrir turma"
                    icon="pi pi-arrow-right"
                    severity="secondary"
                    outlined
                    class="acao-principal"
                    data-testid="painel-abrir-turma"
                    @click="abrirAulasDaTurma(t.departamentoId)"
                  />
                </template>
              </CartaoItem>
              <p v-if="resumoTurmasOrdenado.length === 0" class="lista-vazia">
                Você ainda não tem turmas atribuídas.
              </p>
            </div>

            <DataTable
              v-else
              :value="resumoTurmasOrdenado"
              :rows="10"
              dataKey="departamentoId"
              responsiveLayout="scroll"
              emptyMessage="Você ainda não tem turmas atribuídas."
            >
              <Column field="nomeDepartamento" header="Turma" />
              <Column field="totalAulas" header="Aulas" style="width: 90px" />
              <Column
                field="aulasAbertas"
                header="Abertas"
                style="width: 100px"
              />
              <Column header="Ação" style="width: 110px">
                <template #body="{ data }">
                  <Button
                    icon="pi pi-arrow-right"
                    size="small"
                    severity="secondary"
                    v-tooltip.top="'Ver aulas'"
                    @click="abrirTurma(data)"
                  />
                </template>
              </Column>
            </DataTable>
          </div>
        </div>
      </LoadingOverlay>
    </template>

    <template v-else>
      <PageHeader
        titulo="Meu Painel"
        :subtitulo="`Bem-vindo(a), ${autenticacao.emailUsuario}`"
      >
        <template #acoes>
          <Button
            label="Recarregar"
            icon="pi pi-refresh"
            severity="secondary"
            :loading="carregando"
            @click="carregarPainel"
          />
        </template>
      </PageHeader>

      <InlineMessage :texto="erro" tipo="erro" />

      <LoadingOverlay
        :loading="carregando"
        texto="Carregando suas informações..."
      >
        <div class="card-tabela">
          <div class="card-tabela-titulo">Minhas turmas</div>

          <DataTable
            :value="minhasTurmas"
            dataKey="departamentoId"
            responsiveLayout="scroll"
            emptyMessage="Você ainda não está matriculado(a) em nenhuma turma. Procure a secretaria/administração da igreja."
          >
            <Column field="nome" header="Turma" sortable />
            <Column field="tipo" header="Tipo" style="width: 110px" sortable />
            <Column header="Vínculo" style="width: 140px">
              <template #body="{ data }">
                <Tag
                  :value="data.vinculo"
                  :severity="severityVinculo(data.vinculo)"
                />
              </template>
            </Column>
            <Column header="Status" style="width: 110px">
              <template #body="{ data }">
                <Tag
                  :value="data.ativo ? 'Ativa' : 'Inativa'"
                  :severity="data.ativo ? 'success' : 'secondary'"
                />
              </template>
            </Column>
            <Column header="Ação" style="width: 110px">
              <template #body="{ data }">
                <Button
                  icon="pi pi-arrow-right"
                  size="small"
                  severity="secondary"
                  v-tooltip.top="'Ver aulas'"
                  :disabled="!data.ativo"
                  @click="abrirTurma(data)"
                />
              </template>
            </Column>
          </DataTable>
        </div>

        <div class="stats-grid" data-testid="painel-aluno-indicadores">
          <div class="stat-card">
            <div class="stat-card-label">Aulas consolidadas ({{ rotuloJanela }})</div>
            <div class="stat-card-valor">{{ totalAulasConsolidadas }}</div>
          </div>
          <div class="stat-card">
            <div class="stat-card-label">Presenças ({{ rotuloJanela }})</div>
            <div class="stat-card-valor">{{ totalPresencas }}</div>
          </div>
          <div class="stat-card">
            <div class="stat-card-label">Faltas ({{ rotuloJanela }})</div>
            <div class="stat-card-valor">{{ totalFaltas }}</div>
          </div>
          <div class="stat-card">
            <div class="stat-card-label">Frequência ({{ rotuloJanela }})</div>
            <div class="stat-card-valor">{{ percentualPresenca }}</div>
          </div>
        </div>

        <div class="card-tabela">
          <div class="card-tabela-titulo">
            Meu histórico de presenças — {{ rotuloJanela }}
          </div>
          <p class="card-tabela-nota">
            Mesma janela e mesma regra de Minha Frequência, somando todas as suas
            turmas: só aulas Consolidadas entram nos números acima; aulas Em aberto
            ou Não realizadas aparecem na lista, mas não contam.
          </p>

          <DataTable
            :value="presencasOrdenadas"
            paginator
            :rows="10"
            dataKey="aulaId"
            responsiveLayout="scroll"
            :emptyMessage="`Nenhum registro de presença nos ${rotuloJanela}.`"
          >
            <Column header="Data" style="width: 120px">
              <template #body="{ data }">
                {{ formatarData(data.dataAula) }}
              </template>
            </Column>
            <Column field="departamentoNome" header="Turma" />
            <Column field="materiaNome" header="Matéria" />
            <Column header="Presença" style="width: 130px">
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
        </div>
      </LoadingOverlay>
    </template>
  </div>
</template>
