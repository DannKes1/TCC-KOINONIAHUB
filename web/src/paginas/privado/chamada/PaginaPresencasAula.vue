<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import TagSituacaoAula from "../../../components/ui/TagSituacaoAula.vue";
import CartaoItem from "../../../components/ui/CartaoItem.vue";
import CardIndicador from "../../../components/ui/CardIndicador.vue";
import MenuAcoes from "../../../components/ui/MenuAcoes.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";

import { obterAula } from "../../../aplicacao/servicos/aulasServico";
import { listarPresencasRegistradas } from "../../../aplicacao/servicos/chamadasServico";

import type { AulaVM, PresencaVM } from "../../../aplicacao/modelos/dtos";

import Button from "primevue/button";
import Tag from "primevue/tag";
import type { MenuItem } from "primevue/menuitem";

// RF34 / CSU10 — consulta dos registros já salvos de uma aula. Corretiva da 6.4-B:
// mesmo padrão da Chamada nos dois layouts — cabeçalho com a situação, "Ver
// chamada" e ⋮ (Recarregar); indicadores com o CardIndicador do projeto; um cartão
// por aluno, em grade no desktop e em uma coluna no celular.
const route = useRoute();
const router = useRouter();

const { carregando, erro, run } = useAsync();

const aulaId = Number(route.params.aulaId ?? 0);

const aula = ref<AulaVM | null>(null);
const presencas = ref<PresencaVM[]>([]);

const presencasOrdenadas = computed(() =>
  [...presencas.value].sort((a, b) =>
    a.nomeAluno.localeCompare(b.nomeAluno, "pt-BR"),
  ),
);

// Voltar para a Página da Turma (RF20), aba Aulas, com o nome da turma no rótulo.
const voltarPara = computed(() =>
  aula.value?.departamentoId
    ? `/departamentos/${aula.value.departamentoId}/aulas`
    : "/minhas-turmas",
);
const voltarLabel = computed(
  () => aula.value?.nomeDepartamento || "Minhas Turmas",
);

const titulo = computed(() =>
  aula.value ? `Presenças — ${formatarData(aula.value.data)}` : "Presenças",
);
const subtitulo = computed(() =>
  aula.value
    ? [aula.value.nomeMateria, aula.value.tema].filter(Boolean).join(" · ")
    : "",
);

const itensMenu = computed<MenuItem[]>(() => [
  {
    label: "Recarregar",
    icon: "pi pi-refresh",
    command: () => void carregarTela(),
  },
]);

function formatarData(valor?: string | null) {
  if (!valor) return "-";

  const data = new Date(valor);
  if (Number.isNaN(data.getTime())) return "-";

  return data.toLocaleDateString("pt-BR");
}

// "dd/mm/aaaa às hh:mm" — curto o bastante para caber numa linha do cartão.
function formatarDataHora(valor?: string | null) {
  if (!valor) return "-";

  const data = new Date(valor);
  if (Number.isNaN(data.getTime())) return "-";

  const dia = data.toLocaleDateString("pt-BR");
  const hora = data.toLocaleTimeString("pt-BR", {
    hour: "2-digit",
    minute: "2-digit",
  });
  return `${dia} às ${hora}`;
}

function severityPresenca(presente: boolean) {
  return presente ? "success" : "danger";
}

function abrirChamada() {
  router.push(`/aulas/${aulaId}/chamada`);
}

const totalRegistros = computed(() => presencas.value.length);

const totalPresentes = computed(
  () => presencas.value.filter((item) => item.presente).length,
);

const totalAusentes = computed(
  () => presencas.value.filter((item) => !item.presente).length,
);

async function carregarTela() {
  await run(async () => {
    const [aulaCarregada, presencasCarregadas] = await Promise.all([
      obterAula(aulaId),
      listarPresencasRegistradas(aulaId),
    ]);

    aula.value = aulaCarregada;
    presencas.value = presencasCarregadas;
  }, "Não foi possível carregar as presenças registradas da aula.");
}

onMounted(carregarTela);
</script>

<template>
  <div class="page-container">
    <PageHeader
      :titulo="titulo"
      :subtitulo="subtitulo"
      :voltarPara="voltarPara"
      :voltarLabel="voltarLabel"
    >
      <template #acoes>
        <TagSituacaoAula
          v-if="aula"
          :situacao="aula.situacao"
          :pendenteFechamento="aula.pendenteFechamento"
        />
        <Button
          :label="aula && aula.situacao !== 'EmAberto' ? 'Ver chamada' : 'Abrir chamada'"
          icon="pi pi-check-square"
          severity="secondary"
          data-testid="presencas-abrir-chamada"
          @click="abrirChamada"
        />
        <MenuAcoes
          :itens="itensMenu"
          rotulo="Mais ações das presenças"
          :desabilitado="carregando"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <div class="presencas-indicadores" data-testid="presencas-indicadores">
      <CardIndicador
        rotulo="Registros"
        :valor="totalRegistros"
        icone="pi pi-list-check"
      />
      <CardIndicador
        rotulo="Presentes"
        :valor="totalPresentes"
        icone="pi pi-check-circle"
        tom="sucesso"
      />
      <CardIndicador
        rotulo="Ausentes"
        :valor="totalAusentes"
        icone="pi pi-times-circle"
        :tom="totalAusentes > 0 ? 'perigo' : 'padrao'"
      />
    </div>

    <LoadingOverlay
      :loading="carregando"
      texto="Carregando presenças registradas..."
    >
      <div class="lista-presencas" data-testid="presencas-cartoes">
        <CartaoItem
          v-for="item in presencasOrdenadas"
          :key="item.id"
          :titulo="item.nomeAluno"
          data-testid="presenca-cartao"
        >
          <template #chips>
            <Tag
              :value="item.presente ? 'Presente' : 'Ausente'"
              :severity="severityPresenca(item.presente)"
            />
          </template>
          <template #meta>
            <span v-if="item.observacao" class="presenca-observacao">
              Obs.: {{ item.observacao }}
            </span>
            <span class="presenca-registro">
              Registrado em {{ formatarDataHora(item.criadoEm) }}
            </span>
          </template>
        </CartaoItem>

        <p v-if="!carregando && presencas.length === 0" class="lista-vazia">
          Nenhuma presença registrada para esta aula.
        </p>
      </div>
    </LoadingOverlay>
  </div>
</template>

<style scoped>
.presencas-indicadores {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
}

.lista-presencas {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 10px;
  align-items: start;
}

.lista-vazia {
  grid-column: 1 / -1;
  margin: 4px 0;
  padding: 14px;
  text-align: center;
  color: var(--ipb-cinza-claro, #7a7a7a);
  font-size: 14px;
}

.presenca-observacao {
  display: block;
  color: var(--ipb-cinza, #4d4d4d);
}

.presenca-registro {
  display: block;
  white-space: nowrap;
}

@media (max-width: 768px) {
  .presencas-indicadores {
    grid-template-columns: 1fr 1fr;
    gap: 10px;
  }

  .lista-presencas {
    grid-template-columns: 1fr;
  }
}
</style>
