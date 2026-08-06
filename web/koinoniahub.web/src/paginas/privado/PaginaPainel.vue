<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";

// Store
import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";

// UI base
import PageHeader from "../../components/ui/PageHeader.vue";
import InlineMessage from "../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../components/ui/LoadingOverplay.vue";

// Composable
import { useAsync } from "../../aplicacao/composables/useAsync";

// PrimeVue
import Button from "primevue/button";
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Tag from "primevue/tag";

// Serviços
import { listarDepartamentos } from "../../aplicacao/servicos/departamentosServico";
import { listarPessoas } from "../../aplicacao/servicos/pessoasServico";
import { listarAulasPorDepartamento } from "../../aplicacao/servicos/aulasServico";
import { listarHistoricoPresencasDaPessoa } from "../../aplicacao/servicos/presencasPessoaServico";
import {
  listarMinhasTurmas,
  type MinhaTurmaVM,
} from "../../aplicacao/servicos/meusDadosServico";

// Tipos
import type {
  DepartamentoVM,
  PessoaVM,
  AulaVM,
  HistoricoPresencaPessoaVM,
} from "../../aplicacao/modelos/dtos";

// ── Tipos locais ──
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

// ── Perfis ──
const perfilLower = computed(() =>
  (autenticacao.perfil || "").trim().toLowerCase(),
);

const isAdministrativo = computed(() =>
  ["admin", "pastor", "superintendente"].includes(perfilLower.value),
);

const isProfessor = computed(() => perfilLower.value === "professor");

// ── Estado compartilhado (admin + professor) ──
const pessoas = ref<PessoaVM[]>([]);
const departamentos = ref<DepartamentoVM[]>([]);
const aulasRecentes = ref<AulaComTurmaVM[]>([]);
const resumoTurmas = ref<ResumoTurmaVM[]>([]);

// ── Estado usuário comum ──
const historicoPresencas = ref<HistoricoPresencaPessoaVM[]>([]);
const minhasTurmas = ref<MinhaTurmaVM[]>([]);

// ── Navegação ──
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

// Roteia conforme o vínculo: quem é só aluno vê a própria frequência (somente leitura);
// quem tem papel docente (Professor, Auxiliar, etc.) vai para as aulas/chamada.
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

// ── Helpers ──
function formatarData(valor?: string | null) {
  if (!valor) return "-";
  const data = new Date(valor);
  if (Number.isNaN(data.getTime())) return "-";
  return data.toLocaleDateString("pt-BR");
}

function severityAula(consolidada: boolean) {
  return consolidada ? "success" : "warning";
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

// ── Computeds painel ADMINISTRATIVO ──
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

// ── Computeds painel PROFESSOR ──
const aulasAbertasDoProfessor = computed(() =>
  [...aulasRecentes.value]
    .filter((a) => !a.consolidada)
    .sort((a, b) => new Date(b.data).getTime() - new Date(a.data).getTime()),
);

const totalMinhasTurmas = computed(() => departamentos.value.length);
const totalMinhasAulas = computed(() => aulasRecentes.value.length);
const totalMinhasAulasAbertas = computed(
  () => aulasRecentes.value.filter((a) => !a.consolidada).length,
);

// ── Computeds usuário comum ──
const presencasOrdenadas = computed(() =>
  [...historicoPresencas.value].sort(
    (a, b) => new Date(b.dataAula).getTime() - new Date(a.dataAula).getTime(),
  ),
);

const totalPresencas = computed(
  () => historicoPresencas.value.filter((p) => p.presente).length,
);

const totalFaltas = computed(
  () => historicoPresencas.value.filter((p) => !p.presente).length,
);

const percentualPresenca = computed(() => {
  const total = historicoPresencas.value.length;
  if (total === 0) return "-";
  const pct = (totalPresencas.value / total) * 100;
  return `${pct.toFixed(0)}%`;
});

// ── Carregamentos ──
async function carregarDadosTurmas() {
  // Backend já filtra /departamentos por atribuição quando é professor
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
      aulasAbertas: aulas.filter((aula) => !aula.consolidada).length,
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
    <!-- ════════════════════════════════════════ -->
    <!-- PAINEL ADMINISTRATIVO (Admin/Pastor/Superintendente) -->
    <!-- ════════════════════════════════════════ -->
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
              <Column header="Status" style="width: 120px">
                <template #body="{ data }">
                  <Tag
                    :value="data.consolidada ? 'Consolidada' : 'Aberta'"
                    :severity="severityAula(data.consolidada)"
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

    <!-- ════════════════════════════════════════ -->
    <!-- PAINEL DO PROFESSOR                       -->
    <!-- ════════════════════════════════════════ -->
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
            grid-template-columns: 1fr 1fr;
            gap: 16px;
            align-items: start;
          "
        >
          <!-- Aulas abertas com atalho direto para chamada -->
          <div class="card-tabela">
            <div class="card-tabela-titulo">
              Aulas em aberto (fazer chamada)
            </div>

            <DataTable
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

          <!-- Minhas turmas -->
          <div class="card-tabela">
            <div class="card-tabela-titulo">Minhas turmas</div>

            <DataTable
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

    <!-- ════════════════════════════════════════ -->
    <!-- PAINEL DO USUÁRIO COMUM                   -->
    <!-- ════════════════════════════════════════ -->
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
        <!-- Card: minhas turmas -->
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

        <!-- Estatísticas de presença -->
        <div class="stats-grid">
          <div class="stat-card">
            <div class="stat-card-label">Total de aulas</div>
            <div class="stat-card-valor">{{ historicoPresencas.length }}</div>
          </div>
          <div class="stat-card">
            <div class="stat-card-label">Presenças</div>
            <div class="stat-card-valor">{{ totalPresencas }}</div>
          </div>
          <div class="stat-card">
            <div class="stat-card-label">Faltas</div>
            <div class="stat-card-valor">{{ totalFaltas }}</div>
          </div>
          <div class="stat-card">
            <div class="stat-card-label">Frequência</div>
            <div class="stat-card-valor">{{ percentualPresenca }}</div>
          </div>
        </div>

        <!-- Histórico de presenças -->
        <div class="card-tabela">
          <div class="card-tabela-titulo">
            Meu histórico de presenças - 3 Meses
          </div>

          <DataTable
            :value="presencasOrdenadas"
            paginator
            :rows="10"
            dataKey="aulaId"
            responsiveLayout="scroll"
            emptyMessage="Nenhum registro de presença encontrado."
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
            <Column field="observacao" header="Observação" />
          </DataTable>
        </div>
      </LoadingOverlay>
    </template>
  </div>
</template>
