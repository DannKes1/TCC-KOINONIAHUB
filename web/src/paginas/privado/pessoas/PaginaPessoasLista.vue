<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";

import DialogImportarPessoas from "./DialogImportarPessoas.vue";

import {
  toastSuccess,
  toastWarn,
} from "../../../aplicacao/servicos/notificacoes";

import {
  firstFieldError,
  mensagemForaDosCampos,
} from "../../../aplicacao/servicos/apiError";

import {
  listarPessoas,
  obterPessoa,
  criarPessoa,
  atualizarPessoa,
} from "../../../aplicacao/servicos/pessoasServico";
import {
  listarParentescosDaPessoa,
  criarParentesco,
  removerParentesco,
} from "../../../aplicacao/servicos/parentescosServico";
import TabelaParentescos from "../../../components/pessoas/TabelaParentescos.vue";
// Corretiva da 7.3 (CSU14): o diálogo do histórico virou componente, compartilhado
// com a aba Alunos da Página da Turma.
import DialogHistoricoPresencas from "../../../components/pessoas/DialogHistoricoPresencas.vue";

import type {
  PessoaVM,
  ParentescoVM,
} from "../../../aplicacao/modelos/dtos";

import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import InputText from "primevue/inputtext";
import Dropdown from "primevue/dropdown";
import Calendar from "primevue/calendar";

import { useConfirm } from "primevue/useconfirm";

const confirm = useConfirm();
const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

// Corretiva G3: o erro do salvar fica no próprio diálogo Nova/Editar pessoa — junto ao
// campo quando a API devolve o campo, senão num aviso no topo — e some ao fechar ou ao
// alterar um campo. A instância da página continua com o erro da lista e dos parentescos.
const {
  carregando: salvando,
  erro: erroDoDialogo,
  fieldErrors: errosDoDialogo,
  run: executarNoDialogo,
  clearErrors: limparErrosDoDialogo,
} = useAsync();

const dialogImportarAberto = ref(false);

const pessoas = ref<PessoaVM[]>([]);
const parentescos = ref<ParentescoVM[]>([]);

const dialogPessoaAberto = ref(false);
const dialogParentescosAberto = ref(false);
const dialogHistoricoAberto = ref(false);

const pessoaSelecionadaParentescos = ref<PessoaVM | null>(null);
const pessoaSelecionadaHistorico = ref<PessoaVM | null>(null);

const editandoId = ref<number | null>(null);
// Corretiva G2: pessoa com conta de acesso tem o e-mail da conta; o campo fica bloqueado
// e só o Administrador o altera, na edição do usuário (RF14).
const editandoPossuiConta = ref(false);

const busca = ref("");
// Entrega G: a lista abre com as pessoas ativas; "Todas" é escolha explícita (limpar).
const filtroSituacao = ref<string | null>("Ativo");

const opcoesSituacao = [
  { label: "Ativo", value: "Ativo" },
  { label: "Inativo", value: "Inativo" },
];

const opcoesSexo = [
  { label: "Masculino", value: "Masculino" },
  { label: "Feminino", value: "Feminino" },
];

const opcoesEstadoCivil = [
  { label: "Solteiro(a)", value: "Solteiro(a)" },
  { label: "Casado(a)", value: "Casado(a)" },
  { label: "Divorciado(a)", value: "Divorciado(a)" },
  { label: "Viúvo(a)", value: "Viúvo(a)" },
];

const opcoesRelacionamento = [
  { label: "Pai", value: "Pai" },
  { label: "Mãe", value: "Mãe" },
  { label: "Filho(a)", value: "Filho(a)" },
  { label: "Irmão(ã)", value: "Irmão(ã)" },
  { label: "Cônjuge", value: "Cônjuge" },
  { label: "Responsável", value: "Responsável" },
  { label: "Outro", value: "Outro" },
];

const formulario = reactive({
  nome: "",
  dataNascimento: null as Date | null,
  sexo: null as string | null,
  estadoCivil: null as string | null,
  celular: "",
  email: "",
  endereco: "",
  bairro: "",
  cidade: "",
  estado: "",
  cep: "",
  situacao: "Ativo",
});

const formularioParentesco = reactive({
  parenteId: null as number | null,
  tipoRelacionamento: "" as string,
});

// Todos os campos do diálogo têm FieldError; um erro da API fora deles vai para o aviso.
const CAMPOS_PESSOA = [
  "Nome",
  "DataNascimento",
  "Sexo",
  "EstadoCivil",
  "Celular",
  "Email",
  "Endereco",
  "Bairro",
  "Cidade",
  "Estado",
  "CEP",
  "Situacao",
];

const avisoDoDialogo = computed(() =>
  mensagemForaDosCampos(erroDoDialogo.value, errosDoDialogo.value, CAMPOS_PESSOA),
);

watch(dialogPessoaAberto, (aberto) => {
  if (!aberto) limparErrosDoDialogo();
});
watch(formulario, limparErrosDoDialogo);

function limparFormulario() {
  formulario.nome = "";
  formulario.dataNascimento = null;
  formulario.sexo = null;
  formulario.estadoCivil = null;
  formulario.celular = "";
  formulario.email = "";
  formulario.endereco = "";
  formulario.bairro = "";
  formulario.cidade = "";
  formulario.estado = "";
  formulario.cep = "";
  formulario.situacao = "Ativo";
  editandoId.value = null;
  editandoPossuiConta.value = false;
}

function popularFormulario(p: PessoaVM) {
  formulario.nome = p.nome ?? "";
  formulario.dataNascimento = p.dataNascimento
    ? new Date(p.dataNascimento)
    : null;
  formulario.sexo = p.sexo ?? null;
  formulario.estadoCivil = p.estadoCivil ?? null;
  formulario.celular = p.celular ?? "";
  formulario.email = p.email ?? "";
  formulario.endereco = p.endereco ?? "";
  formulario.bairro = p.bairro ?? "";
  formulario.cidade = p.cidade ?? "";
  formulario.estado = p.estado ?? "";
  formulario.cep = p.cep ?? "";
  formulario.situacao = p.situacao ?? "Ativo";
}

function limparFormularioParentesco() {
  formularioParentesco.parenteId = null;
  formularioParentesco.tipoRelacionamento = "";
}

function abrirNovo() {
  clearErrors();
  limparErrosDoDialogo();
  limparFormulario();
  dialogPessoaAberto.value = true;
}

async function abrirEdicao(pessoa: PessoaVM) {
  clearErrors();
  limparErrosDoDialogo();
  limparFormulario();

  await run(async () => {
    const completa = await obterPessoa(pessoa.id);
    editandoId.value = completa.id;
    editandoPossuiConta.value = completa.possuiConta;
    popularFormulario(completa);
    dialogPessoaAberto.value = true;
  }, "Não foi possível carregar os dados desta pessoa.");
}

async function abrirParentescos(pessoa: PessoaVM) {
  clearErrors();
  pessoaSelecionadaParentescos.value = pessoa;
  limparFormularioParentesco();
  dialogParentescosAberto.value = true;
  await carregarParentescos();
}

function abrirHistorico(pessoa: PessoaVM) {
  clearErrors();
  pessoaSelecionadaHistorico.value = pessoa;
  dialogHistoricoAberto.value = true;
}

function textoOuNull(valor?: string | null) {
  const texto = String(valor ?? "").trim();
  return texto || null;
}

function dataParaEnvio(data: Date | null) {
  if (!data) return null;

  const ano = data.getFullYear();
  const mes = data.getMonth();
  const dia = data.getDate();

  return new Date(Date.UTC(ano, mes, dia, 12, 0, 0)).toISOString();
}

function formatarData(valor?: string | null) {
  if (!valor) return "-";

  const data = new Date(valor);
  if (Number.isNaN(data.getTime())) return "-";

  return data.toLocaleDateString("pt-BR");
}

function sanitizarTelefone(valor: string): string {
  return valor.replace(/[^\d()+\-\s]/g, "");
}

function contarDigitos(valor: string): number {
  return (valor.match(/\d/g) ?? []).length;
}

watch(
  () => formulario.celular,
  (v) => {
    const limpo = sanitizarTelefone(v ?? "");
    if (limpo !== v) formulario.celular = limpo;
  },
);

function validarFormulario(): string {
  if (!formulario.nome.trim()) return "Informe o nome da pessoa.";

  const celDigitos = contarDigitos(formulario.celular);
  if (celDigitos > 0 && (celDigitos < 8 || celDigitos > 11))
    return "Celular inválido. Informe DDD + número (8 a 11 dígitos).";

  return "";
}

function validarFormularioParentesco(): string {
  if (!pessoaSelecionadaParentescos.value) {
    return "Nenhuma pessoa foi selecionada.";
  }

  if (!formularioParentesco.parenteId) {
    return "Selecione o parente.";
  }

  if (!formularioParentesco.tipoRelacionamento.trim()) {
    return "Informe o tipo de relacionamento.";
  }

  if (
    formularioParentesco.parenteId === pessoaSelecionadaParentescos.value.id
  ) {
    return "A pessoa não pode ser parente dela mesma.";
  }

  return "";
}

const pessoasFiltradas = computed(() => {
  const termo = busca.value.trim().toLowerCase();
  const situacao = filtroSituacao.value?.trim().toLowerCase() || "";

  return pessoas.value.filter((pessoa) => {
    const bateBusca =
      !termo ||
      pessoa.nome.toLowerCase().includes(termo) ||
      String(pessoa.email ?? "")
        .toLowerCase()
        .includes(termo) ||
      String(pessoa.celular ?? "")
        .toLowerCase()
        .includes(termo);

    const bateSituacao =
      !situacao || String(pessoa.situacao ?? "").toLowerCase() === situacao;

    return bateBusca && bateSituacao;
  });
});

const pessoasDisponiveisParaParentesco = computed(() => {
  const pessoaBase = pessoaSelecionadaParentescos.value;
  if (!pessoaBase) return [];

  // Só os vínculos registrados aqui excluem a pessoa do seletor: uma derivada não
  // impede registrar o vínculo explícito (que então passa a valer no lugar dela).
  const idsJaRelacionados = new Set(
    parentescos.value
      .filter((item) => !item.derivado)
      .map((item) => item.parenteId),
  );

  return [...pessoas.value]
    .filter((pessoa) => pessoa.id !== pessoaBase.id)
    .filter((pessoa) => !idsJaRelacionados.has(pessoa.id))
    .sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR"));
});

async function carregarLista() {
  await run(async () => {
    pessoas.value = await listarPessoas();
  }, "Não foi possível carregar as pessoas.");
}

async function carregarParentescos() {
  const pessoa = pessoaSelecionadaParentescos.value;
  if (!pessoa) return;

  await run(async () => {
    parentescos.value = await listarParentescosDaPessoa(pessoa.id);
  }, "Não foi possível carregar os parentescos.");
}

async function salvarPessoa() {
  const msg = validarFormulario();
  if (msg) {
    toastWarn(msg);
    return;
  }

  const payload = {
    Nome: formulario.nome.trim(),
    DataNascimento: dataParaEnvio(formulario.dataNascimento),
    Sexo: textoOuNull(formulario.sexo),
    EstadoCivil: textoOuNull(formulario.estadoCivil),
    Celular: textoOuNull(formulario.celular),
    Email: textoOuNull(formulario.email),
    Endereco: textoOuNull(formulario.endereco),
    Bairro: textoOuNull(formulario.bairro),
    Cidade: textoOuNull(formulario.cidade),
    Estado: textoOuNull(formulario.estado)?.toUpperCase(),
    CEP: textoOuNull(formulario.cep),
    Situacao: textoOuNull(formulario.situacao) ?? "Ativo",
  };

  await executarNoDialogo(async () => {
    if (editandoId.value === null) {
      await criarPessoa(payload);
      toastSuccess("Pessoa cadastrada com sucesso.", "Criado");
    } else {
      await atualizarPessoa(editandoId.value, payload);
      toastSuccess("Pessoa atualizada com sucesso.", "Salvo");
    }

    dialogPessoaAberto.value = false;
    await carregarLista();
  }, "Não foi possível salvar a pessoa.");
}

async function salvarParentesco() {
  const msg = validarFormularioParentesco();
  if (msg) {
    toastWarn(msg);
    return;
  }

  const pessoa = pessoaSelecionadaParentescos.value;
  if (!pessoa) return;

  await run(async () => {
    await criarParentesco(pessoa.id, {
      ParenteId: Number(formularioParentesco.parenteId),
      TipoRelacionamento: formularioParentesco.tipoRelacionamento.trim(),
    });

    toastSuccess("Parentesco cadastrado com sucesso.", "Criado");
    limparFormularioParentesco();
    await carregarParentescos();
  }, "Não foi possível salvar o parentesco.");
}

function confirmarRemocaoParentesco(item: ParentescoVM) {
  const pessoa = pessoaSelecionadaParentescos.value;
  if (!pessoa) return;

  confirm.require({
    header: "Remover parentesco",
    message: `Deseja remover o vínculo "${item.tipoRelacionamento}" com "${item.parenteNome}"?`,
    icon: "pi pi-exclamation-triangle",
    acceptLabel: "Remover",
    rejectLabel: "Cancelar",
    accept: async () => {
      await run(async () => {
        await removerParentesco(pessoa.id, item.id);
        toastSuccess("Parentesco removido com sucesso.", "Concluído");
        await carregarParentescos();
      }, "Não foi possível remover o parentesco.");
    },
  });
}

onMounted(carregarLista);
</script>

<template>
  <div class="page-container">
    <PageHeader
      titulo="Pessoas"
      subtitulo="Cadastro, consulta, vínculos familiares e histórico de presença das pessoas da igreja"
    >
      <template #acoes>
        <Button label="Nova Pessoa" icon="pi pi-plus" @click="abrirNovo" />
        <Button
          label="Importar CSV"
          icon="pi pi-upload"
          severity="secondary"
          @click="dialogImportarAberto = true"
        />
        <Button
          label="Recarregar"
          icon="pi pi-refresh"
          severity="secondary"
          :loading="carregando"
          @click="carregarLista"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <div
      style="
        display: grid;
        grid-template-columns: 1.6fr 220px;
        gap: 10px;
        align-items: end;
      "
    >
      <div style="display: flex; flex-direction: column; gap: 6px">
        <label for="pessoas-filtro-busca">Buscar</label>
        <InputText
          id="pessoas-filtro-busca"
          v-model="busca"
          placeholder="Pesquisar por nome, e-mail ou celular..."
        />
      </div>

      <div style="display: flex; flex-direction: column; gap: 6px">
        <label
          id="pessoas-filtro-situacao-rotulo"
          for="pessoas-filtro-situacao"
          >Situação</label
        >
        <Dropdown
          labelId="pessoas-filtro-situacao"
          ariaLabelledby="pessoas-filtro-situacao-rotulo"
          v-model="filtroSituacao"
          :options="opcoesSituacao"
          optionLabel="label"
          optionValue="value"
          showClear
          placeholder="Todas"
        />
      </div>

      <DialogImportarPessoas
        v-model:visible="dialogImportarAberto"
        @importado="carregarLista"
      />
    </div>

    <LoadingOverlay :loading="carregando" texto="Carregando pessoas...">
      <DataTable
        :value="pessoasFiltradas"
        paginator
        :rows="10"
        rowHover
        sortField="nome"
        :sortOrder="1"
        dataKey="id"
        responsiveLayout="scroll"
      >
        <Column field="nome" header="Nome" sortable />
        <Column field="situacao" header="Situação" sortable />

        <Column header="Nascimento" style="width: 130px">
          <template #body="{ data }">
            {{ formatarData(data.dataNascimento) }}
          </template>
        </Column>

        <Column field="celular" header="Celular" />
        <Column field="email" header="E-mail" />

        <Column header="Criado em" style="width: 130px">
          <template #body="{ data }">
            {{ formatarData(data.criadoEm) }}
          </template>
        </Column>

        <Column header="Inativado em" style="width: 140px">
          <template #body="{ data }">
            {{ formatarData(data.dataInativacao) }}
          </template>
        </Column>

        <Column header="Ações" style="width: 240px">
          <template #body="{ data }">
            <div style="display: flex; gap: 8px">
              <Button
                icon="pi pi-pencil"
                severity="secondary"
                size="small"
                v-tooltip.top="'Editar'"
                :disabled="carregando"
                @click="abrirEdicao(data)"
              />
              <Button
                icon="pi pi-share-alt"
                severity="help"
                size="small"
                v-tooltip.top="'Parentescos'"
                :disabled="carregando"
                @click="abrirParentescos(data)"
              />
              <Button
                icon="pi pi-calendar"
                severity="info"
                size="small"
                v-tooltip.top="'Histórico de Presenças'"
                :disabled="carregando"
                @click="abrirHistorico(data)"
              />
            </div>
          </template>
        </Column>
      </DataTable>
    </LoadingOverlay>

    <Dialog
      v-model:visible="dialogPessoaAberto"
      modal
      :closable="!salvando"
      :dismissableMask="!salvando"
      :header="editandoId === null ? 'Nova pessoa' : 'Editar pessoa'"
      style="width: 920px; max-width: 96vw"
    >
      <div style="display: flex; flex-direction: column; gap: 16px">
        <InlineMessage
          :texto="avisoDoDialogo"
          tipo="erro"
          data-testid="pessoa-dialogo-erro"
        />

        <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 12px">
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="pessoa-formulario-nome">Nome *</label>
            <InputText
              id="pessoa-formulario-nome"
              v-model="formulario.nome"
              placeholder="Nome completo da pessoa"
            />
            <FieldError
              :texto="
                firstFieldError(errosDoDialogo, 'Nome') ||
                firstFieldError(errosDoDialogo, 'nome')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label
              id="pessoa-formulario-situacao-rotulo"
              for="pessoa-formulario-situacao"
              >Situação</label
            >
            <Dropdown
              labelId="pessoa-formulario-situacao"
              ariaLabelledby="pessoa-formulario-situacao-rotulo"
              v-model="formulario.situacao"
              :options="opcoesSituacao"
              optionLabel="label"
              optionValue="value"
              placeholder="Selecione a situação"
            />
            <FieldError
              :texto="
                firstFieldError(errosDoDialogo, 'Situacao') ||
                firstFieldError(errosDoDialogo, 'situacao')
              "
            />
          </div>
        </div>

        <div
          style="
            display: grid;
            grid-template-columns: 1fr 1fr 1fr 1fr;
            gap: 12px;
          "
        >
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="pessoa-formulario-nascimento"
              >Data de nascimento</label
            >
            <Calendar
              inputId="pessoa-formulario-nascimento"
              v-model="formulario.dataNascimento"
              dateFormat="dd/mm/yy"
              showIcon
              iconDisplay="input"
              showButtonBar
            />
            <FieldError
              :texto="
                firstFieldError(errosDoDialogo, 'DataNascimento') ||
                firstFieldError(errosDoDialogo, 'dataNascimento')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label
              id="pessoa-formulario-sexo-rotulo"
              for="pessoa-formulario-sexo"
              >Sexo</label
            >
            <Dropdown
              labelId="pessoa-formulario-sexo"
              ariaLabelledby="pessoa-formulario-sexo-rotulo"
              v-model="formulario.sexo"
              :options="opcoesSexo"
              optionLabel="label"
              optionValue="value"
              showClear
              placeholder="Selecione"
            />
            <FieldError
              :texto="
                firstFieldError(errosDoDialogo, 'Sexo') ||
                firstFieldError(errosDoDialogo, 'sexo')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label
              id="pessoa-formulario-estado-civil-rotulo"
              for="pessoa-formulario-estado-civil"
              >Estado civil</label
            >
            <Dropdown
              labelId="pessoa-formulario-estado-civil"
              ariaLabelledby="pessoa-formulario-estado-civil-rotulo"
              v-model="formulario.estadoCivil"
              :options="opcoesEstadoCivil"
              optionLabel="label"
              optionValue="value"
              showClear
              placeholder="Selecione"
            />
            <FieldError
              :texto="
                firstFieldError(errosDoDialogo, 'EstadoCivil') ||
                firstFieldError(errosDoDialogo, 'estadoCivil')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="pessoa-formulario-cep">CEP</label>
            <InputText
              id="pessoa-formulario-cep"
              v-model="formulario.cep"
              placeholder="00000-000"
            />
            <FieldError
              :texto="
                firstFieldError(errosDoDialogo, 'CEP') ||
                firstFieldError(errosDoDialogo, 'cep')
              "
            />
          </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px">
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="pessoa-formulario-celular">Celular</label>
            <InputText
              id="pessoa-formulario-celular"
              v-model="formulario.celular"
              placeholder="Celular / WhatsApp"
            />
            <FieldError
              data-testid="pessoa-celular-erro"
              :texto="
                firstFieldError(errosDoDialogo, 'Celular') ||
                firstFieldError(errosDoDialogo, 'celular')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="pessoa-formulario-email">E-mail</label>
            <InputText
              id="pessoa-formulario-email"
              v-model="formulario.email"
              placeholder="email@dominio.com"
              :disabled="editandoPossuiConta"
            />
            <small
              v-if="editandoPossuiConta"
              data-testid="pessoa-email-dica"
              style="font-size: 12px; color: var(--ipb-cinza-claro, #7a7a7a)"
            >
              E-mail de acesso; definido na criação da conta.
            </small>
            <FieldError
              :texto="
                firstFieldError(errosDoDialogo, 'Email') ||
                firstFieldError(errosDoDialogo, 'email')
              "
            />
          </div>
        </div>

        <div
          style="
            display: grid;
            grid-template-columns: 1.4fr 1fr 1fr 120px;
            gap: 12px;
          "
        >
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="pessoa-formulario-endereco">Endereço</label>
            <InputText
              id="pessoa-formulario-endereco"
              v-model="formulario.endereco"
              placeholder="Rua, número, complemento"
            />
            <FieldError
              :texto="
                firstFieldError(errosDoDialogo, 'Endereco') ||
                firstFieldError(errosDoDialogo, 'endereco')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="pessoa-formulario-bairro">Bairro</label>
            <InputText
              id="pessoa-formulario-bairro"
              v-model="formulario.bairro"
              placeholder="Bairro"
            />
            <FieldError
              :texto="
                firstFieldError(errosDoDialogo, 'Bairro') ||
                firstFieldError(errosDoDialogo, 'bairro')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="pessoa-formulario-cidade">Cidade</label>
            <InputText
              id="pessoa-formulario-cidade"
              v-model="formulario.cidade"
              placeholder="Cidade"
            />
            <FieldError
              :texto="
                firstFieldError(errosDoDialogo, 'Cidade') ||
                firstFieldError(errosDoDialogo, 'cidade')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="pessoa-formulario-uf">UF</label>
            <InputText
              id="pessoa-formulario-uf"
              v-model="formulario.estado"
              placeholder="UF"
              maxlength="2"
            />
            <FieldError
              :texto="
                firstFieldError(errosDoDialogo, 'Estado') ||
                firstFieldError(errosDoDialogo, 'estado')
              "
            />
          </div>
        </div>
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="salvando"
          data-testid="pessoa-dialogo-cancelar"
          @click="dialogPessoaAberto = false"
        />
        <Button
          :label="editandoId === null ? 'Salvar' : 'Atualizar'"
          icon="pi pi-check"
          :loading="salvando"
          data-testid="pessoa-dialogo-salvar"
          @click="salvarPessoa"
        />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="dialogParentescosAberto"
      modal
      :closable="!carregando"
      :dismissableMask="!carregando"
      :header="`Parentescos - ${pessoaSelecionadaParentescos?.nome ?? ''}`"
      style="width: 860px; max-width: 96vw"
    >
      <div style="display: flex; flex-direction: column; gap: 14px">
        <InlineMessage :texto="erro" tipo="erro" />

        <div
          style="
            display: grid;
            grid-template-columns: 1.2fr 1fr 140px;
            gap: 12px;
            align-items: end;
          "
        >
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label
              id="pessoa-parentesco-parente-rotulo"
              for="pessoa-parentesco-parente"
              >Parente *</label
            >
            <Dropdown
              labelId="pessoa-parentesco-parente"
              ariaLabelledby="pessoa-parentesco-parente-rotulo"
              v-model="formularioParentesco.parenteId"
              :options="pessoasDisponiveisParaParentesco"
              optionLabel="nome"
              optionValue="id"
              filter
              showClear
              placeholder="Selecione a pessoa"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'ParenteId') ||
                firstFieldError(fieldErrors, 'parenteId')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="pessoa-parentesco-relacionamento"
              >Relacionamento *</label
            >
            <Dropdown
              labelId="pessoa-parentesco-relacionamento"
              v-model="formularioParentesco.tipoRelacionamento"
              :options="opcoesRelacionamento"
              optionLabel="label"
              optionValue="value"
              editable
              placeholder="Selecione ou digite"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'TipoRelacionamento') ||
                firstFieldError(fieldErrors, 'tipoRelacionamento')
              "
            />
          </div>

          <Button
            label="Adicionar"
            icon="pi pi-plus"
            :loading="carregando"
            @click="salvarParentesco"
          />
        </div>

        <InlineMessage
          v-if="pessoasDisponiveisParaParentesco.length === 0"
          texto="Não há mais pessoas disponíveis para novo vínculo com esta pessoa."
          tipo="aviso"
        />

        <LoadingOverlay :loading="carregando" texto="Carregando parentescos...">
          <TabelaParentescos
            :itens="parentescos"
            :carregando="carregando"
            @remover="confirmarRemocaoParentesco"
          />
        </LoadingOverlay>
      </div>

      <template #footer>
        <Button
          label="Fechar"
          severity="secondary"
          :disabled="carregando"
          @click="dialogParentescosAberto = false"
        />
      </template>
    </Dialog>

    <DialogHistoricoPresencas
      v-model:visible="dialogHistoricoAberto"
      :pessoa-id="pessoaSelecionadaHistorico?.id ?? null"
      :nome="pessoaSelecionadaHistorico?.nome ?? ''"
    />
  </div>
</template>
