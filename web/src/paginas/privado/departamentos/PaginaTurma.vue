<script setup lang="ts">
import { computed, onMounted, provide, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";

import Tabs from "primevue/tabs";
import TabList from "primevue/tablist";
import Tab from "primevue/tab";
import Tag from "primevue/tag";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";

import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";
import { useAsync } from "../../../aplicacao/composables/useAsync";

import { obterDepartamento } from "../../../aplicacao/servicos/departamentosServico";
import { listarAlunosDaTurma } from "../../../aplicacao/servicos/matriculasServico";
import { listarMaterias } from "../../../aplicacao/servicos/materiasServico";
import { listarAtribuicoesPorDepartamento } from "../../../aplicacao/servicos/atribuicoesServico";
import { listarAulasPorDepartamento } from "../../../aplicacao/servicos/aulasServico";

import type {
  AtribuicaoVM,
  DepartamentoVM,
} from "../../../aplicacao/modelos/dtos";

// RF20 / CSU08, CSU09, CSU12, CSU18 ("o usuário acessa a turma e abre a opção…"):
// a Página da Turma reúne, em /departamentos/:id, o cabeçalho com os detalhes da
// turma (alunos matriculados, professores atribuídos, matérias e aulas) e as abas
// Alunos · Matérias · Aulas · Atribuições como rotas filhas — as URLs de cada aba são
// as mesmas de antes, então nada que apontava para elas quebra. A aba Atribuições
// só aparece para a gestão (CSU18, RNF 21.1); o professor vê a equipe no cabeçalho
// (RF20, RF23).
const route = useRoute();
const router = useRouter();
const autenticacao = usarAutenticacaoStore();
const { carregando, erro, run } = useAsync();

const departamentoId = computed(() => Number(route.params.departamentoId));

const turma = ref<DepartamentoVM | null>(null);
const totalAlunosAtivos = ref(0);
const totalMaterias = ref(0);
const totalAulasEmAberto = ref(0);
const equipe = ref<AtribuicaoVM[]>([]);

type Aba = { valor: string; rotulo: string; icone: string };

const abas = computed<Aba[]>(() => {
  const lista: Aba[] = [
    { valor: "matriculas", rotulo: "Alunos", icone: "pi pi-users" },
    { valor: "materias", rotulo: "Matérias", icone: "pi pi-book" },
    { valor: "aulas", rotulo: "Aulas", icone: "pi pi-calendar" },
  ];
  if (autenticacao.isAdministrativo) {
    lista.push({ valor: "atribuicoes", rotulo: "Atribuições", icone: "pi pi-id-card" });
  }
  return lista;
});

// A aba ativa é o último segmento da rota (as filhas são /departamentos/:id/<aba>).
const abaAtual = computed(() => {
  const segmento = String(route.path.split("/").filter(Boolean).pop() ?? "");
  return abas.value.some((a) => a.valor === segmento) ? segmento : "aulas";
});

function irParaAba(valor: string | number) {
  const destino = `/departamentos/${departamentoId.value}/${String(valor)}`;
  if (route.path !== destino) router.push(destino);
}

// Gestão volta para a lista de turmas; professor/auxiliar, para Minhas Turmas.
const voltarPara = computed(() =>
  autenticacao.isAdministrativo ? "/departamentos" : "/minhas-turmas",
);
const voltarLabel = computed(() =>
  autenticacao.isAdministrativo ? "Turmas EBD" : "Minhas Turmas",
);

const professores = computed(() =>
  equipe.value
    .filter((a) => (a.funcao || "").toLowerCase() === "professor")
    .map((a) => a.pessoaNome),
);
const auxiliares = computed(() =>
  equipe.value
    .filter((a) => (a.funcao || "").toLowerCase() === "auxiliar")
    .map((a) => a.pessoaNome),
);

async function carregarResumo() {
  await run(async () => {
    if (!departamentoId.value) throw new Error("Turma inválida.");

    const [dep, alunos, materias, atribuicoes, aulas] = await Promise.all([
      obterDepartamento(departamentoId.value),
      listarAlunosDaTurma(departamentoId.value),
      listarMaterias(departamentoId.value),
      listarAtribuicoesPorDepartamento(departamentoId.value, { ativo: true }),
      listarAulasPorDepartamento(departamentoId.value),
    ]);

    turma.value = dep;
    totalAlunosAtivos.value = alunos.filter((a) => a.matriculaAtiva).length;
    totalMaterias.value = materias.filter((m) => m.ativo).length;
    equipe.value = atribuicoes.filter((a) => a.ativo);
    totalAulasEmAberto.value = aulas.filter((a) => a.situacao === "EmAberto").length;
  }, "Não foi possível carregar a turma.");
}

// As abas chamam isto depois de criar/alterar algo, para os contadores não ficarem
// defasados em relação à lista que acabou de mudar.
provide("turmaRecarregarResumo", carregarResumo);

onMounted(carregarResumo);
watch(departamentoId, carregarResumo);
</script>

<template>
  <div class="page-container">
    <PageHeader
      :titulo="turma ? turma.nome : 'Turma'"
      :voltarPara="voltarPara"
      :voltarLabel="voltarLabel"
      modo="completo"
    >
      <template #acoes>
        <span v-if="turma" class="turma-chips">
          <Tag :value="turma.tipo || 'EBD'" severity="secondary" />
          <Tag
            :value="turma.ativo ? 'Ativa' : 'Inativa'"
            :severity="turma.ativo ? 'success' : 'danger'"
            data-testid="turma-situacao"
          />
        </span>
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <div class="turma-resumo" data-testid="turma-resumo">
      <div class="stat-card turma-stat">
        <div class="stat-card-label">Alunos ativos</div>
        <div class="stat-card-valor" data-testid="turma-total-alunos">
          {{ carregando && !turma ? "…" : totalAlunosAtivos }}
        </div>
      </div>
      <div class="stat-card turma-stat">
        <div class="stat-card-label">Aulas em aberto</div>
        <div class="stat-card-valor" data-testid="turma-total-aulas-abertas">
          {{ carregando && !turma ? "…" : totalAulasEmAberto }}
        </div>
      </div>
      <div class="stat-card turma-stat turma-stat-desktop">
        <div class="stat-card-label">Matérias</div>
        <div class="stat-card-valor">
          {{ carregando && !turma ? "…" : totalMaterias }}
        </div>
      </div>
      <div class="stat-card turma-stat turma-stat-desktop">
        <div class="stat-card-label">Equipe</div>
        <div class="stat-card-valor">
          {{ carregando && !turma ? "…" : equipe.length }}
        </div>
      </div>
    </div>

    <p class="turma-equipe" data-testid="turma-equipe">
      <template v-if="professores.length > 0">
        <strong>{{ professores.length > 1 ? "Professores:" : "Professor(a):" }}</strong>
        {{ professores.join(", ") }}
      </template>
      <span v-else><strong>Professor(a):</strong> nenhuma atribuição ativa</span>
      <template v-if="auxiliares.length > 0">
        · <strong>{{ auxiliares.length > 1 ? "Auxiliares:" : "Auxiliar:" }}</strong>
        {{ auxiliares.join(", ") }}
      </template>
    </p>

    <Tabs :value="abaAtual" scrollable @update:value="irParaAba">
      <TabList>
        <Tab
          v-for="aba in abas"
          :key="aba.valor"
          :value="aba.valor"
          :data-testid="`turma-aba-${aba.valor}`"
        >
          <i :class="aba.icone" style="margin-right: 6px"></i>{{ aba.rotulo }}
        </Tab>
      </TabList>
    </Tabs>

    <RouterView />
  </div>
</template>

<style scoped>
.turma-chips {
  display: inline-flex;
  gap: 6px;
  align-items: center;
}

.turma-resumo {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 12px;
}

.turma-stat {
  padding: 14px 16px;
}

.turma-equipe {
  margin: -4px 0 0;
  font-size: 13.5px;
  line-height: 1.5;
  color: var(--ipb-cinza, #4d4d4d);
}

:deep(.p-tablist-tab-list) {
  background: transparent;
}

:deep(.p-tab) {
  padding: 10px 14px;
  font-weight: 600;
  white-space: nowrap;
}

@media (max-width: 768px) {
  .turma-resumo {
    grid-template-columns: 1fr 1fr;
    gap: 10px;
  }

  /* No celular ficam os dois contadores que orientam a ação (alunos e aulas). */
  .turma-stat-desktop {
    display: none;
  }

  :deep(.p-tab) {
    min-height: 44px;
  }
}
</style>
