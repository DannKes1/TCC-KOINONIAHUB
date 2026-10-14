<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute } from "vue-router";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import TagSituacaoAula from "../../../components/ui/TagSituacaoAula.vue";
import MenuAcoes from "../../../components/ui/MenuAcoes.vue";
import BarraAcoesFixa from "../../../components/ui/BarraAcoesFixa.vue";
import LinhaChamada from "../../../components/chamada/LinhaChamada.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";
import { usarAcaoUnica } from "../../../aplicacao/composables/usarAcaoUnica";
import {
  chamadaAlterada,
  copiarLinhas,
  montarLoteChamada,
  resumirChamada,
  type LinhaChamada as LinhaChamadaVM,
} from "../../../aplicacao/dominio/chamada";

import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";

import Button from "primevue/button";
import InputNumber from "primevue/inputnumber";
import type { MenuItem } from "primevue/menuitem";

import { useConfirm } from "primevue/useconfirm";

import {
  obterAula,
  consolidarAula,
  extrairAlunosSemRegistro,
} from "../../../aplicacao/servicos/aulasServico";
import {
  listarChamadaCompleta,
  registrarChamada,
} from "../../../aplicacao/servicos/chamadasServico";

import type { AulaVM } from "../../../aplicacao/modelos/dtos";

// RF32 / CSU10 (lançar chamada) e RF33 / CSU11 (consolidar). Etapa 6.4-B: a mesma
// tela nos dois layouts — cabeçalho curto com a situação, resumo "Presentes N de M",
// lista de alunos em que tocar na linha marca a presença, observação só quando
// pedida e a barra fixa Salvar · Consolidar. Consolidar salva as alterações
// pendentes antes (decisão 3 da 6.4), e a tela mostra uma mensagem por vez.
const route = useRoute();
const confirm = useConfirm();

const { carregando, erro, run } = useAsync();

// Salvar e Consolidar ignoram um segundo toque enquanto a requisição anda
// (corretiva da 6.4-B: um toque no celular gerava dois "Chamada salva").
const { emAndamento, executar } = usarAcaoUnica();

const aulaId = computed(() => Number(route.params.aulaId));

const aula = ref<AulaVM | null>(null);
const linhas = ref<LinhaChamadaVM[]>([]);
const visitantes = ref(0);

// Último estado devolvido pela API — base para saber se há alteração não salva.
const linhasSalvas = ref<LinhaChamadaVM[]>([]);
const visitantesSalvos = ref(0);

// RNF 32.2 / CSU10 FA1: só aulas Em aberto permitem lançar ou alterar a chamada;
// Consolidada e Não realizada ficam em modo somente leitura.
const somenteLeitura = computed(
  () => aula.value !== null && aula.value.situacao !== "EmAberto",
);

const resumo = computed(() => resumirChamada(linhas.value));

const alterado = computed(() =>
  chamadaAlterada(
    linhas.value,
    linhasSalvas.value,
    visitantes.value,
    visitantesSalvos.value,
  ),
);

// Alunos apontados pela API no 400 da consolidação (RNFs 32.6/33.3, CSU11 FA1).
const idsSemRegistro = ref<Set<number>>(new Set());

function semRegistro(alunoDepartamentoId: number) {
  return idsSemRegistro.value.has(alunoDepartamentoId);
}

// Uma mensagem por vez: erro da API > chamada incompleta > aula fechada.
const mensagem = computed<{ texto: string; tipo: "erro" | "aviso" } | null>(() => {
  if (erro.value) return { texto: erro.value, tipo: "erro" };

  if (idsSemRegistro.value.size > 0) {
    return {
      tipo: "aviso",
      texto: `${idsSemRegistro.value.size} aluno(s) sem registro de presença ou ausência (destacados na lista). Marque presente ou ausente e salve a chamada para poder consolidar.`,
    };
  }

  if (aula.value?.situacao === "Consolidada") {
    return {
      tipo: "aviso",
      texto:
        "Esta aula está consolidada. A chamada está em modo somente leitura; apenas o Administrador pode reabri-la para correção.",
    };
  }

  if (aula.value?.situacao === "NaoRealizada") {
    return {
      tipo: "aviso",
      texto:
        "Esta aula foi marcada como Não realizada: não possui registros de presença e não entra nos cálculos de frequência. Apenas o Administrador pode reabri-la.",
    };
  }

  return null;
});

// Voltar para a Página da Turma (RF20), aba Aulas; o rótulo é o nome da turma.
const voltarPara = computed(() =>
  aula.value?.departamentoId
    ? `/departamentos/${aula.value.departamentoId}/aulas`
    : "/minhas-turmas",
);
const voltarLabel = computed(
  () => aula.value?.nomeDepartamento || "Minhas Turmas",
);

const titulo = computed(() =>
  aula.value ? `Chamada — ${formatarData(aula.value.data)}` : "Chamada",
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
    command: () => void carregarTudo(),
  },
  {
    label: "Limpar presenças",
    icon: "pi pi-times-circle",
    visible: !somenteLeitura.value,
    command: limparPresencas,
  },
]);

function formatarData(iso: string) {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString("pt-BR");
}

function aplicarLista(itens: LinhaChamadaVM[], quantidadeVisitantes: number) {
  linhas.value = copiarLinhas(itens).sort((a, b) =>
    a.nomeAluno.localeCompare(b.nomeAluno, "pt-BR"),
  );
  linhasSalvas.value = copiarLinhas(linhas.value);
  visitantes.value = Number(quantidadeVisitantes ?? 0);
  visitantesSalvos.value = visitantes.value;
}

async function carregarTudo() {
  idsSemRegistro.value = new Set();

  await run(async () => {
    if (!aulaId.value) throw new Error("Aula inválida.");

    const [a, itens] = await Promise.all([
      obterAula(aulaId.value),
      listarChamadaCompleta(aulaId.value),
    ]);

    aula.value = a;
    aplicarLista(itens ?? [], a.quantidadeVisitantes);
  }, "Não foi possível carregar a chamada.");
}

function atualizarPresente(alunoDepartamentoId: number, presente: boolean) {
  const linha = linhas.value.find(
    (l) => l.alunoDepartamentoId === alunoDepartamentoId,
  );
  if (linha) linha.presente = presente;
}

function atualizarObservacao(
  alunoDepartamentoId: number,
  observacao: string | null,
) {
  const linha = linhas.value.find(
    (l) => l.alunoDepartamentoId === alunoDepartamentoId,
  );
  if (linha) linha.observacao = observacao;
}

function marcarTodosPresentes() {
  if (somenteLeitura.value) return;
  for (const l of linhas.value) l.presente = true;
}

function limparPresencas() {
  if (somenteLeitura.value) return;
  for (const l of linhas.value) l.presente = false;
}

// RF32: lote (upsert) com todos os alunos da lista e a contagem de visitantes.
async function gravarChamada() {
  await registrarChamada(
    aulaId.value,
    montarLoteChamada(linhas.value, visitantes.value),
  );
  idsSemRegistro.value = new Set();

  const [a, itens] = await Promise.all([
    obterAula(aulaId.value),
    listarChamadaCompleta(aulaId.value),
  ]);
  aula.value = a;
  aplicarLista(itens ?? [], a.quantidadeVisitantes);
}

async function salvar() {
  if (somenteLeitura.value) return;

  await executar(() =>
    run(async () => {
      await gravarChamada();
      toastSuccess("Chamada salva com sucesso.", "Salvo");
    }, "Não foi possível salvar a chamada."),
  );
}

// RF33 / CSU11: consolidar exige aula Em aberto e chamada completa. Com alteração
// não salva, a chamada é salva antes (decisão 3). Quando a API devolve 400 com
// alunosSemRegistro[], as linhas são destacadas para o professor completar (FA1).
function confirmarConsolidar() {
  if (somenteLeitura.value || emAndamento.value) return;

  const salvaAntes = alterado.value;

  confirm.require({
    header: "Consolidar aula",
    message: salvaAntes
      ? "A chamada será salva e consolidada. Depois disso não poderá mais ser alterada. Deseja continuar?"
      : "Ao consolidar, a chamada não poderá mais ser alterada. Deseja continuar?",
    icon: "pi pi-exclamation-triangle",
    acceptLabel: salvaAntes ? "Salvar e consolidar" : "Consolidar",
    rejectLabel: "Cancelar",
    acceptClass: "p-button-danger",
    accept: () =>
      executar(async () => {
        try {
          await run(
            async () => {
              if (salvaAntes) await gravarChamada();
              await consolidarAula(aulaId.value);
              toastSuccess("Aula consolidada com sucesso.", "Consolidada");
              idsSemRegistro.value = new Set();
              aula.value = await obterAula(aulaId.value);
            },
            "Não foi possível consolidar a aula.",
            { throwOnError: true },
          );
        } catch (e) {
          const alunos = extrairAlunosSemRegistro(e);
          if (alunos.length > 0) {
            // Uma mensagem só: a lista destacada substitui o erro genérico da API.
            erro.value = "";
            idsSemRegistro.value = new Set(
              alunos.map((a) => a.alunoDepartamentoId),
            );
          }
        }
      }),
  });
}

onMounted(carregarTudo);
</script>

<template>
  <div class="page-container pagina-chamada">
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
        <MenuAcoes
          :itens="itensMenu"
          rotulo="Mais ações da chamada"
          :desabilitado="carregando"
        />
      </template>
    </PageHeader>

    <InlineMessage
      v-if="mensagem"
      :texto="mensagem.texto"
      :tipo="mensagem.tipo"
      data-testid="chamada-mensagem"
    />

    <LoadingOverlay :loading="carregando" texto="Carregando chamada...">
      <div class="chamada-resumo" data-testid="chamada-resumo">
        <div class="chamada-resumo-presentes">
          <strong data-testid="chamada-contador">
            Presentes: {{ resumo.presentes }} de {{ resumo.total }}
          </strong>
          <Button
            v-if="!somenteLeitura"
            label="Marcar todos presentes"
            icon="pi pi-check-circle"
            text
            size="small"
            class="chamada-marcar-todos"
            :disabled="carregando || linhas.length === 0"
            data-testid="chamada-marcar-todos"
            @click="marcarTodosPresentes"
          />
        </div>

        <label class="chamada-visitantes" for="qtd-visitantes">
          <span title="Pessoas sem matrícula que participaram da aula; a contagem é salva com a chamada.">
            Visitantes
          </span>
          <InputNumber
            v-model="visitantes"
            inputId="qtd-visitantes"
            :min="0"
            :max="999"
            showButtons
            :disabled="carregando || somenteLeitura"
            :inputStyle="{ width: '5.5rem' }"
          />
        </label>
      </div>

      <div class="chamada-lista" data-testid="chamada-lista">
        <LinhaChamada
          v-for="linha in linhas"
          :key="linha.alunoDepartamentoId"
          :linha="linha"
          :somenteLeitura="somenteLeitura"
          :desabilitado="carregando"
          :semRegistro="semRegistro(linha.alunoDepartamentoId)"
          @update:presente="atualizarPresente(linha.alunoDepartamentoId, $event)"
          @update:observacao="atualizarObservacao(linha.alunoDepartamentoId, $event)"
        />

        <p v-if="!carregando && linhas.length === 0" class="chamada-vazia">
          Nenhum aluno com matrícula ativa nesta turma.
        </p>
      </div>
    </LoadingOverlay>

    <BarraAcoesFixa v-if="!somenteLeitura" rotulo="Ações da chamada">
      <template #resumo>
        <strong>Presentes: {{ resumo.presentes }} de {{ resumo.total }}</strong>
        <span
          class="chamada-estado-salvo"
          :class="{ 'chamada-estado-pendente': alterado }"
          data-testid="chamada-estado-salvo"
        >
          <i :class="alterado ? 'pi pi-pencil' : 'pi pi-check'" aria-hidden="true"></i>
          {{ alterado ? "Alterações não salvas" : "Tudo salvo" }}
        </span>
      </template>
      <Button
        label="Salvar chamada"
        icon="pi pi-save"
        class="acao-principal"
        data-testid="chamada-salvar"
        :loading="emAndamento"
        :disabled="carregando || emAndamento"
        @click="salvar"
      />
      <Button
        label="Consolidar"
        icon="pi pi-lock"
        severity="danger"
        outlined
        data-testid="chamada-consolidar"
        :disabled="carregando || emAndamento"
        @click="confirmarConsolidar"
      />
    </BarraAcoesFixa>
  </div>
</template>

<style scoped>
.chamada-resumo {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px 20px;
  flex-wrap: wrap;
  margin-bottom: 12px;
}

.chamada-resumo-presentes {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  font-size: 15px;
  color: #222;
}

.chamada-marcar-todos {
  min-height: 36px;
}

.chamada-visitantes {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 14px;
  color: var(--ipb-cinza, #4d4d4d);
}


.chamada-lista {
  background: var(--ipb-branco, #fff);
  border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  border-radius: 12px;
  overflow: hidden;
}

.chamada-vazia {
  padding: 16px;
  text-align: center;
  color: var(--ipb-cinza-claro, #7a7a7a);
  font-size: 14px;
}

/* Ocupa a altura do <main> para a barra de ações encostar no fim (ver BarraAcoesFixa). */
.pagina-chamada {
  flex: 1 0 auto;
}

.chamada-estado-salvo {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: var(--ipb-verde, #234f32);
}

.chamada-estado-pendente {
  color: #8a5b00;
  font-weight: 600;
}

/* Desktop (corretiva da 6.4-B): a lista aproveita a largura — cada aluno é um
   cartão numa grade; no celular continua uma coluna só, como estava. */
@media (min-width: 769px) {
  .chamada-lista {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 10px;
    align-items: start;
    background: transparent;
    border: none;
    border-radius: 0;
    overflow: visible;
  }

  .chamada-lista :deep(.linha-chamada),
  .chamada-lista :deep(.linha-chamada:last-child) {
    border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
    border-radius: 10px;
  }

  .chamada-lista :deep(.linha-chamada-presente) {
    border-color: #b9dcc4;
  }

  .chamada-lista :deep(.linha-chamada-sem-registro) {
    border-color: #f0c68a;
  }

  .chamada-lista .chamada-vazia {
    grid-column: 1 / -1;
  }
}

@media (max-width: 768px) {
  .chamada-resumo {
    align-items: flex-start;
    flex-direction: column;
    gap: 8px;
  }

  .chamada-visitantes {
    width: 100%;
    justify-content: space-between;
  }
}
</style>
