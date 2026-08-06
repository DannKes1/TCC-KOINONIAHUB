<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue";

// UI base
import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";

// Composable
import { useAsync } from "../../../aplicacao/composables/useAsync";

// Notificações
import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";

// Helpers de erro por campo
import { firstFieldError } from "../../../aplicacao/servicos/apiError";

// Serviços
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
import { listarHistoricoPresencasDaPessoa } from "../../../aplicacao/servicos/presencasPessoaServico";

// Tipos
import type {
  PessoaVM,
  ParentescoVM,
  HistoricoPresencaPessoaVM,
} from "../../../aplicacao/modelos/dtos";

// PrimeVue
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import InputText from "primevue/inputtext";
import Dropdown from "primevue/dropdown";
import Calendar from "primevue/calendar";
import Textarea from "primevue/textarea";
import Tag from "primevue/tag";

// PrimeVue services
import { useConfirm } from "primevue/useconfirm";

const confirm = useConfirm();
const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

const pessoas = ref<PessoaVM[]>([]);
const parentescos = ref<ParentescoVM[]>([]);
const historicoPresencas = ref<HistoricoPresencaPessoaVM[]>([]);

const dialogPessoaAberto = ref(false);
const dialogParentescosAberto = ref(false);
const dialogHistoricoAberto = ref(false);

const pessoaSelecionadaParentescos = ref<PessoaVM | null>(null);
const pessoaSelecionadaHistorico = ref<PessoaVM | null>(null);

// Edição: id em edição (null = criando novo)
const editandoId = ref<number | null>(null);

const busca = ref("");
const filtroSituacao = ref<string | null>(null);
const filtroCategoria = ref<string | null>(null);

const opcoesSituacao = [
  { label: "Ativo", value: "Ativo" },
  { label: "Inativo", value: "Inativo" },
];

const opcoesCategoria = [
  { label: "Membro", value: "Membro" },
  { label: "Visitante", value: "Visitante" },
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
  cpf: "",
  dataNascimento: null as Date | null,
  sexo: null as string | null,
  estadoCivil: null as string | null,
  telefone: "",
  celular: "",
  email: "",
  endereco: "",
  bairro: "",
  cidade: "",
  estado: "",
  cep: "",
  situacao: "Ativo",
  categoria: "Membro",
  dataBatismo: null as Date | null,
  dataMembresia: null as Date | null,
  fotoUrl: "",
  observacoes: "",
});

const formularioParentesco = reactive({
  parenteId: null as number | null,
  tipoRelacionamento: "" as string,
});

function limparFormulario() {
  formulario.nome = "";
  formulario.cpf = "";
  formulario.dataNascimento = null;
  formulario.sexo = null;
  formulario.estadoCivil = null;
  formulario.telefone = "";
  formulario.celular = "";
  formulario.email = "";
  formulario.endereco = "";
  formulario.bairro = "";
  formulario.cidade = "";
  formulario.estado = "";
  formulario.cep = "";
  formulario.situacao = "Ativo";
  formulario.categoria = "Membro";
  formulario.dataBatismo = null;
  formulario.dataMembresia = null;
  formulario.fotoUrl = "";
  formulario.observacoes = "";
  editandoId.value = null;
}

function popularFormulario(p: PessoaVM) {
  formulario.nome = p.nome ?? "";
  formulario.cpf = p.cpf ?? "";
  formulario.dataNascimento = p.dataNascimento
    ? new Date(p.dataNascimento)
    : null;
  formulario.sexo = p.sexo ?? null;
  formulario.estadoCivil = p.estadoCivil ?? null;
  formulario.telefone = p.telefone ?? "";
  formulario.celular = p.celular ?? "";
  formulario.email = p.email ?? "";
  formulario.endereco = p.endereco ?? "";
  formulario.bairro = p.bairro ?? "";
  formulario.cidade = p.cidade ?? "";
  formulario.estado = p.estado ?? "";
  formulario.cep = p.cep ?? "";
  formulario.situacao = p.situacao ?? "Ativo";
  formulario.categoria = p.categoria ?? "Membro";
  formulario.dataBatismo = p.dataBatismo ? new Date(p.dataBatismo) : null;
  formulario.dataMembresia = p.dataMembresia ? new Date(p.dataMembresia) : null;
  formulario.fotoUrl = p.fotoUrl ?? "";
  formulario.observacoes = p.observacoes ?? "";
}

function limparFormularioParentesco() {
  formularioParentesco.parenteId = null;
  formularioParentesco.tipoRelacionamento = "";
}

function abrirNovo() {
  clearErrors();
  limparFormulario();
  dialogPessoaAberto.value = true;
}

async function abrirEdicao(pessoa: PessoaVM) {
  clearErrors();
  limparFormulario();

  await run(async () => {
    const completa = await obterPessoa(pessoa.id);
    editandoId.value = completa.id;
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

async function abrirHistorico(pessoa: PessoaVM) {
  clearErrors();
  pessoaSelecionadaHistorico.value = pessoa;
  historicoPresencas.value = [];
  dialogHistoricoAberto.value = true;
  await carregarHistorico();
}

function textoOuNull(valor?: string | null) {
  const texto = String(valor ?? "").trim();
  return texto ? texto : null;
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
  // Bloqueia letras e símbolos; mantém dígitos e formatação comum de telefone.
  return valor.replace(/[^\d()+\-\s]/g, "");
}

function contarDigitos(valor: string): number {
  return (valor.match(/\d/g) ?? []).length;
}

// Impede letras nos campos de telefone/celular durante a digitação (e na colagem).
watch(
  () => formulario.telefone,
  (v) => {
    const limpo = sanitizarTelefone(v ?? "");
    if (limpo !== v) formulario.telefone = limpo;
  },
);
watch(
  () => formulario.celular,
  (v) => {
    const limpo = sanitizarTelefone(v ?? "");
    if (limpo !== v) formulario.celular = limpo;
  },
);

function validarFormulario(): string {
  if (!formulario.nome.trim()) return "Informe o nome da pessoa.";

  const telDigitos = contarDigitos(formulario.telefone);
  if (telDigitos > 0 && (telDigitos < 8 || telDigitos > 11))
    return "Telefone inválido. Informe DDD + número (8 a 11 dígitos).";

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

function severityPresenca(presente: boolean) {
  return presente ? "success" : "danger";
}

const pessoasFiltradas = computed(() => {
  const termo = busca.value.trim().toLowerCase();
  const situacao = filtroSituacao.value?.trim().toLowerCase() || "";
  const categoria = filtroCategoria.value?.trim().toLowerCase() || "";

  return pessoas.value.filter((pessoa) => {
    const bateBusca =
      !termo ||
      pessoa.nome.toLowerCase().includes(termo) ||
      String(pessoa.email ?? "")
        .toLowerCase()
        .includes(termo) ||
      String(pessoa.cpf ?? "")
        .toLowerCase()
        .includes(termo) ||
      String(pessoa.celular ?? "")
        .toLowerCase()
        .includes(termo);

    const bateSituacao =
      !situacao || String(pessoa.situacao ?? "").toLowerCase() === situacao;

    const bateCategoria =
      !categoria || String(pessoa.categoria ?? "").toLowerCase() === categoria;

    return bateBusca && bateSituacao && bateCategoria;
  });
});

const pessoasDisponiveisParaParentesco = computed(() => {
  const pessoaBase = pessoaSelecionadaParentescos.value;
  if (!pessoaBase) return [];

  const idsJaRelacionados = new Set(
    parentescos.value.map((item) => item.parenteId),
  );

  return [...pessoas.value]
    .filter((pessoa) => pessoa.id !== pessoaBase.id)
    .filter((pessoa) => !idsJaRelacionados.has(pessoa.id))
    .sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR"));
});

const totalHistorico = computed(() => historicoPresencas.value.length);

const totalHistoricoPresentes = computed(
  () => historicoPresencas.value.filter((item) => item.presente).length,
);

const totalHistoricoAusentes = computed(
  () => historicoPresencas.value.filter((item) => !item.presente).length,
);

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

async function carregarHistorico() {
  const pessoa = pessoaSelecionadaHistorico.value;
  if (!pessoa) return;

  await run(async () => {
    historicoPresencas.value = await listarHistoricoPresencasDaPessoa(
      pessoa.id,
    );
  }, "Não foi possível carregar o histórico de presenças.");
}

async function salvarPessoa() {
  const msg = validarFormulario();
  if (msg) {
    erro.value = msg;
    return;
  }

  const payload = {
    Nome: formulario.nome.trim(),
    CPF: textoOuNull(formulario.cpf),
    DataNascimento: dataParaEnvio(formulario.dataNascimento),
    Sexo: textoOuNull(formulario.sexo),
    EstadoCivil: textoOuNull(formulario.estadoCivil),
    Telefone: textoOuNull(formulario.telefone),
    Celular: textoOuNull(formulario.celular),
    Email: textoOuNull(formulario.email),
    Endereco: textoOuNull(formulario.endereco),
    Bairro: textoOuNull(formulario.bairro),
    Cidade: textoOuNull(formulario.cidade),
    Estado: textoOuNull(formulario.estado)?.toUpperCase(),
    CEP: textoOuNull(formulario.cep),
    Situacao: textoOuNull(formulario.situacao) ?? "Ativo",
    Categoria: textoOuNull(formulario.categoria) ?? "Membro",
    DataBatismo: dataParaEnvio(formulario.dataBatismo),
    DataMembresia: dataParaEnvio(formulario.dataMembresia),
    FotoUrl: textoOuNull(formulario.fotoUrl),
    Observacoes: textoOuNull(formulario.observacoes),
  };

  await run(async () => {
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
    erro.value = msg;
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
    acceptClass: "p-button-danger",
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
        <label>Buscar</label>
        <InputText
          v-model="busca"
          placeholder="Pesquisar por nome, e-mail, CPF ou celular..."
        />
      </div>

      <div style="display: flex; flex-direction: column; gap: 6px">
        <label>Situação</label>
        <Dropdown
          v-model="filtroSituacao"
          :options="opcoesSituacao"
          optionLabel="label"
          optionValue="value"
          showClear
          placeholder="Todas"
        />
      </div>

      <div style="display: flex; flex-direction: column; gap: 6px">
        <label>Categoria</label>
        <Dropdown
          v-model="filtroCategoria"
          :options="opcoesCategoria"
          optionLabel="label"
          optionValue="value"
          showClear
          placeholder="Todas"
        />
      </div>
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
        <Column field="categoria" header="Categoria" sortable />

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
      :closable="!carregando"
      :dismissableMask="!carregando"
      :header="editandoId === null ? 'Nova pessoa' : 'Editar pessoa'"
      style="width: 920px; max-width: 96vw"
    >
      <div style="display: flex; flex-direction: column; gap: 16px">
        <div
          style="display: grid; grid-template-columns: 1.6fr 1fr 1fr; gap: 12px"
        >
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Nome *</label>
            <InputText
              v-model="formulario.nome"
              placeholder="Nome completo da pessoa"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'Nome') ||
                firstFieldError(fieldErrors, 'nome')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>CPF</label>
            <InputText v-model="formulario.cpf" placeholder="000.000.000-00" />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'CPF') ||
                firstFieldError(fieldErrors, 'cpf')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Situação</label>
            <Dropdown
              v-model="formulario.situacao"
              :options="opcoesSituacao"
              optionLabel="label"
              optionValue="value"
              placeholder="Selecione a situação"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'Situacao') ||
                firstFieldError(fieldErrors, 'situacao')
              "
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Categoria</label>
            <Dropdown
              v-model="formulario.categoria"
              :options="opcoesCategoria"
              optionLabel="label"
              optionValue="value"
              placeholder="Selecione a categoria"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'Categoria') ||
                firstFieldError(fieldErrors, 'categoria')
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
            <label>Data de nascimento</label>
            <Calendar
              v-model="formulario.dataNascimento"
              dateFormat="dd/mm/yy"
              showIcon
              iconDisplay="input"
              showButtonBar
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Sexo</label>
            <Dropdown
              v-model="formulario.sexo"
              :options="opcoesSexo"
              optionLabel="label"
              optionValue="value"
              showClear
              placeholder="Selecione"
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Estado civil</label>
            <Dropdown
              v-model="formulario.estadoCivil"
              :options="opcoesEstadoCivil"
              optionLabel="label"
              optionValue="value"
              showClear
              placeholder="Selecione"
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>CEP</label>
            <InputText v-model="formulario.cep" placeholder="00000-000" />
          </div>
        </div>

        <div
          style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 12px"
        >
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Telefone</label>
            <InputText
              v-model="formulario.telefone"
              placeholder="Telefone fixo"
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Celular</label>
            <InputText
              v-model="formulario.celular"
              placeholder="Celular / WhatsApp"
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>E-mail</label>
            <InputText
              v-model="formulario.email"
              placeholder="email@dominio.com"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'Email') ||
                firstFieldError(fieldErrors, 'email')
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
            <label>Endereço</label>
            <InputText
              v-model="formulario.endereco"
              placeholder="Rua, número, complemento"
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Bairro</label>
            <InputText v-model="formulario.bairro" placeholder="Bairro" />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Cidade</label>
            <InputText v-model="formulario.cidade" placeholder="Cidade" />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>UF</label>
            <InputText
              v-model="formulario.estado"
              placeholder="UF"
              maxlength="2"
            />
          </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px">
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Data de batismo</label>
            <Calendar
              v-model="formulario.dataBatismo"
              dateFormat="dd/mm/yy"
              showIcon
              iconDisplay="input"
              showButtonBar
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Data de membresia</label>
            <Calendar
              v-model="formulario.dataMembresia"
              dateFormat="dd/mm/yy"
              showIcon
              iconDisplay="input"
              showButtonBar
            />
          </div>
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Foto URL</label>
          <InputText v-model="formulario.fotoUrl" placeholder="https://..." />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Observações</label>
          <Textarea
            v-model="formulario.observacoes"
            rows="4"
            autoResize
            placeholder="Observações gerais sobre a pessoa"
          />
        </div>
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="carregando"
          @click="dialogPessoaAberto = false"
        />
        <Button
          :label="editandoId === null ? 'Salvar' : 'Atualizar'"
          icon="pi pi-check"
          :loading="carregando"
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
            <label>Parente *</label>
            <Dropdown
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
            <label>Relacionamento *</label>
            <Dropdown
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
          <DataTable
            :value="parentescos"
            dataKey="id"
            responsiveLayout="scroll"
            emptyMessage="Nenhum parentesco cadastrado."
          >
            <Column field="parenteNome" header="Parente" />
            <Column field="tipoRelacionamento" header="Relacionamento" />

            <Column header="Ações" style="width: 110px">
              <template #body="{ data }">
                <Button
                  icon="pi pi-trash"
                  severity="danger"
                  :disabled="carregando"
                  @click="confirmarRemocaoParentesco(data)"
                />
              </template>
            </Column>
          </DataTable>
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

    <Dialog
      v-model:visible="dialogHistoricoAberto"
      modal
      :closable="!carregando"
      :dismissableMask="!carregando"
      :header="`Histórico de Presenças - ${pessoaSelecionadaHistorico?.nome ?? ''}`"
      style="width: 980px; max-width: 96vw"
    >
      <div style="display: flex; flex-direction: column; gap: 14px">
        <InlineMessage :texto="erro" tipo="erro" />

        <div
          style="
            display: grid;
            grid-template-columns: repeat(3, minmax(180px, 1fr));
            gap: 12px;
          "
        >
          <div
            style="
              border: 1px solid rgba(0, 0, 0, 0.08);
              border-radius: 12px;
              padding: 12px;
            "
          >
            <div style="font-size: 12px; opacity: 0.7">Total de registros</div>
            <div style="font-size: 24px; font-weight: 700">
              {{ totalHistorico }}
            </div>
          </div>

          <div
            style="
              border: 1px solid rgba(0, 0, 0, 0.08);
              border-radius: 12px;
              padding: 12px;
            "
          >
            <div style="font-size: 12px; opacity: 0.7">Presentes</div>
            <div style="font-size: 24px; font-weight: 700">
              {{ totalHistoricoPresentes }}
            </div>
          </div>

          <div
            style="
              border: 1px solid rgba(0, 0, 0, 0.08);
              border-radius: 12px;
              padding: 12px;
            "
          >
            <div style="font-size: 12px; opacity: 0.7">Ausentes</div>
            <div style="font-size: 24px; font-weight: 700">
              {{ totalHistoricoAusentes }}
            </div>
          </div>
        </div>

        <LoadingOverlay
          :loading="carregando"
          texto="Carregando histórico de presenças..."
        >
          <DataTable
            :value="historicoPresencas"
            paginator
            :rows="10"
            rowHover
            sortField="dataAula"
            :sortOrder="-1"
            dataKey="aulaId"
            responsiveLayout="scroll"
            emptyMessage="Nenhum histórico de presença encontrado para esta pessoa."
          >
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

            <Column field="observacao" header="Observação" />
          </DataTable>
        </LoadingOverlay>
      </div>

      <template #footer>
        <Button
          label="Fechar"
          severity="secondary"
          :disabled="carregando"
          @click="dialogHistoricoAberto = false"
        />
      </template>
    </Dialog>
  </div>
</template>
