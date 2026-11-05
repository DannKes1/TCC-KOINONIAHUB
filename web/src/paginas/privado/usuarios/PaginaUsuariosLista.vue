<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";
import { usarFormularioDialogo } from "../../../aplicacao/composables/usarFormularioDialogo";

import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";


import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

import { listarPessoas } from "../../../aplicacao/servicos/pessoasServico";
import {
  listarUsuarios,
  criarUsuario,
  atualizarUsuario,
  gerarConviteUsuario,
} from "../../../aplicacao/servicos/usuariosServico";
import { resumirTermoUsuario } from "../../../aplicacao/dominio/termoUsuario";

import type { PessoaVM, UsuarioVM } from "../../../aplicacao/modelos/dtos";

import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import Dropdown from "primevue/dropdown";
import InputText from "primevue/inputtext";
import Password from "primevue/password";
import Tag from "primevue/tag";
import Checkbox from "primevue/checkbox";

const autenticacao = usarAutenticacaoStore();
const { carregando, erro, run, clearErrors } = useAsync();

// Corretiva G5: erro do salvar dentro do diálogo, validação local no campo e limpeza ao
// fechar ou alterar ficam no composable (antes o bloco se repetia em cada página).
const {
  salvando, executar: executarNoDialogo, errosLocais, limpar: limparErrosDoDialogo,
  erroDoCampo, aviso, semErrosLocais, limparAoFechar, limparAoAlterar,
} = usarFormularioDialogo();

const usuarios = ref<UsuarioVM[]>([]);
const pessoas = ref<PessoaVM[]>([]);

const busca = ref("");
const filtroPerfil = ref<string | null>(null);
// Entrega G: a lista abre com as contas ativas; "Todos" é escolha explícita (limpar).
const filtroAtivo = ref<string | null>("true");

const dialogCriacaoAberto = ref(false);
const dialogEdicaoAberto = ref(false);
const dialogConviteAberto = ref(false);

const editandoUsuario = ref<UsuarioVM | null>(null);

// Dados do convite exibidos após a criação/geração (o token só aparece agora).
const conviteAtual = ref<{
  nomePessoa: string | null;
  email: string;
  link: string;
  expiraEm: string | null;
} | null>(null);

const opcoesPerfil = [
  { label: "Admin", value: "Admin" },
  { label: "Pastor", value: "Pastor" },
  { label: "Superintendente", value: "Superintendente" },
  { label: "Professor", value: "Professor" },
  { label: "Usuario", value: "Usuario" },
];

const opcoesStatus = [
  { label: "Ativo", value: "true" },
  { label: "Inativo", value: "false" },
];

const formularioCriacao = reactive({
  pessoaId: null as number | null,
  email: "",
  senha: "",
  perfil: "Usuario",
  // false = gera convite (a própria pessoa define a senha pelo link);
  // true  = fluxo antigo (o admin digita a senha inicial).
  definirSenhaManual: false,
});

const formularioEdicao = reactive({
  email: "",
  perfil: "Usuario",
  ativo: true,
});

// Campos com FieldError em cada diálogo: um erro da API fora deles vai para o aviso.
const CAMPOS_CRIACAO = ["PessoaId", "Email", "Perfil", "Senha"];
const CAMPOS_EDICAO = ["Email", "Perfil", "Ativo"];

const avisoCriacao = aviso(CAMPOS_CRIACAO);
const avisoEdicao = aviso(CAMPOS_EDICAO);

limparAoFechar(dialogCriacaoAberto, dialogEdicaoAberto);
limparAoAlterar(formularioCriacao, formularioEdicao);

// Corretiva G2 (RNF 13.5): ao escolher a pessoa, o e-mail dela vem preenchido (editável);
// ao salvar, o cadastro da pessoa fica com o e-mail da conta.
watch(
  () => formularioCriacao.pessoaId,
  (id) => {
    const pessoa = pessoas.value.find((p) => p.id === id);
    formularioCriacao.email = pessoa?.email ?? "";
  },
);

// Mesma regra do [EmailAddress] da API: um único "@", fora das pontas.
function emailValido(valor: string) {
  return /^[^@\s]+@[^@\s]+$/.test(valor.trim());
}

function limparCriacao() {
  formularioCriacao.pessoaId = null;
  formularioCriacao.email = "";
  formularioCriacao.senha = "";
  formularioCriacao.perfil = "Usuario";
  formularioCriacao.definirSenhaManual = false;
}

function abrirNovo() {
  clearErrors();
  limparErrosDoDialogo();
  limparCriacao();
  dialogCriacaoAberto.value = true;
}

function abrirEdicao(usuario: UsuarioVM) {
  clearErrors();
  limparErrosDoDialogo();
  editandoUsuario.value = usuario;
  formularioEdicao.email = usuario.email;
  formularioEdicao.perfil = usuario.perfil || "Usuario";
  formularioEdicao.ativo = Boolean(usuario.ativo);
  dialogEdicaoAberto.value = true;
}

async function carregarDados() {
  await run(async () => {
    const [usuariosCarregados, pessoasCarregadas] = await Promise.all([
      listarUsuarios(),
      listarPessoas(),
    ]);

    usuarios.value = usuariosCarregados;
    pessoas.value = pessoasCarregadas;
  }, "Não foi possível carregar os usuários.");
}

const pessoasDisponiveis = computed(() => {
  const idsJaVinculados = new Set(
    usuarios.value
      .map((u) => u.pessoaId)
      .filter((id): id is number => typeof id === "number" && id > 0),
  );

  return [...pessoas.value]
    .filter((p) => !idsJaVinculados.has(p.id))
    .sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR"));
});

const usuariosFiltrados = computed(() => {
  const termo = busca.value.trim().toLowerCase();
  const perfil = String(filtroPerfil.value ?? "")
    .trim()
    .toLowerCase();
  const ativo = filtroAtivo.value;

  return usuarios.value.filter((usuario) => {
    const bateBusca =
      !termo ||
      usuario.email.toLowerCase().includes(termo) ||
      String(usuario.nomePessoa ?? "")
        .toLowerCase()
        .includes(termo);

    const batePerfil =
      !perfil || String(usuario.perfil ?? "").toLowerCase() === perfil;

    const bateAtivo =
      ativo === null ||
      ativo === "" ||
      String(Boolean(usuario.ativo)) === ativo;

    return bateBusca && batePerfil && bateAtivo;
  });
});

function validarCriacao(): boolean {
  limparErrosDoDialogo();

  if (!formularioCriacao.pessoaId) {
    errosLocais.PessoaId = "Selecione a pessoa que será vinculada ao usuário.";
  }

  if (!emailValido(formularioCriacao.email)) {
    errosLocais.Email = "Informe um e-mail de acesso válido.";
  }

  if (formularioCriacao.definirSenhaManual) {
    if (!formularioCriacao.senha.trim()) {
      errosLocais.Senha = "Informe a senha inicial do usuário.";
    } else if (formularioCriacao.senha.trim().length < 6) {
      errosLocais.Senha = "A senha deve ter no mínimo 6 caracteres.";
    }
  }

  return semErrosLocais();
}

function validarEdicao(): boolean {
  limparErrosDoDialogo();

  if (!formularioEdicao.perfil.trim()) {
    errosLocais.Perfil = "Informe o perfil do usuário.";
  }

  if (!emailValido(formularioEdicao.email)) {
    errosLocais.Email = "Informe um e-mail de acesso válido.";
  }

  if (
    editandoUsuario.value &&
    editandoUsuario.value.id === autenticacao.usuarioId &&
    !formularioEdicao.ativo
  ) {
    errosLocais.Ativo = "Você não pode desativar o seu próprio usuário.";
  }

  return semErrosLocais();
}

function textoOuNull(valor?: string | null) {
  const texto = String(valor ?? "").trim();
  return texto || null;
}

function rotuloStatus(ativo: boolean) {
  return ativo ? "Ativo" : "Inativo";
}

function severityStatus(ativo: boolean) {
  return ativo ? "success" : "danger";
}

// RF43 / RNF 43.1: situação do aceite do Termo de Uso e Sigilo de cada conta, com a
// versão na tag e a data e hora visíveis na coluna (o tooltip não existe no celular).
function termoDoUsuario(usuario: UsuarioVM) {
  return resumirTermoUsuario(usuario.aceiteTermo);
}

function severityPerfil(perfil: string) {
  const valor = String(perfil || "").toLowerCase();

  if (valor === "admin") return "danger";
  if (valor === "pastor") return "warn";
  if (valor === "superintendente") return "info";
  if (valor === "professor") return "success";
  return "secondary";
}

// ---------- Convite de primeiro acesso / Redefinir acesso (RF39, RF15) ----------

function montarLinkConvite(token: string) {
  return `${window.location.origin}/primeiro-acesso?token=${token}`;
}

function formatarDataConvite(valor: string | null) {
  if (!valor) return "";
  const data = new Date(valor);
  if (Number.isNaN(data.getTime())) return "";
  return data.toLocaleDateString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

function abrirDialogConvite(dados: {
  nomePessoa: string | null;
  email: string;
  token: string;
  expiraEm: string | null;
}) {
  conviteAtual.value = {
    nomePessoa: dados.nomePessoa,
    email: dados.email,
    link: montarLinkConvite(dados.token),
    expiraEm: dados.expiraEm,
  };
  dialogConviteAberto.value = true;
}

async function copiarLinkConvite() {
  const link = conviteAtual.value?.link ?? "";
  if (!link) return;

  try {
    await navigator.clipboard.writeText(link);
    toastSuccess("Link copiado para a área de transferência.", "Copiado");
  } catch {
    // Fallback para navegadores sem permissão de clipboard.
    const campo = document.createElement("textarea");
    campo.value = link;
    document.body.appendChild(campo);
    campo.select();
    document.execCommand("copy");
    campo.remove();
    toastSuccess("Link copiado para a área de transferência.", "Copiado");
  }
}

function abrirWhatsAppConvite() {
  const convite = conviteAtual.value;
  if (!convite) return;

  const nome = convite.nomePessoa ? `, ${convite.nomePessoa}` : "";
  const validade = formatarDataConvite(convite.expiraEm);

  const mensagem =
    `Olá${nome}! Seu acesso ao KoinoniaHub foi criado. ` +
    `Defina a sua senha pelo link abaixo` +
    (validade ? ` (válido até ${validade})` : "") +
    `:\n${convite.link}`;

  window.open(`https://wa.me/?text=${encodeURIComponent(mensagem)}`, "_blank");
}

// "Redefinir acesso" (CSU20): gera um novo convite de uso único, que invalida o
// anterior; a senha atual continua válida até a pessoa definir a nova pelo link.
// O administrador nunca digita a senha de outro usuário (RNF 15.3).
async function gerarConvite(usuario: UsuarioVM) {
  await run(async () => {
    const convite = await gerarConviteUsuario(usuario.id);

    abrirDialogConvite({
      nomePessoa: convite.nomePessoa ?? usuario.nomePessoa,
      email: convite.email || usuario.email,
      token: convite.token,
      expiraEm: convite.expiraEm,
    });

    await carregarDados();
  }, "Não foi possível gerar o convite.");
}

// ------------------------------------------------

async function salvarNovo() {
  if (!validarCriacao()) return;

  await executarNoDialogo(async () => {
    const criado = await criarUsuario({
      PessoaId: Number(formularioCriacao.pessoaId),
      Email: textoOuNull(formularioCriacao.email),
      Senha: formularioCriacao.definirSenhaManual
        ? formularioCriacao.senha.trim()
        : null,
      Perfil: formularioCriacao.perfil.trim(),
    });

    dialogCriacaoAberto.value = false;

    if (criado.conviteToken) {
      toastSuccess(
        "Usuário criado. Envie o link de convite para a pessoa definir a senha.",
        "Criado",
      );
      abrirDialogConvite({
        nomePessoa: criado.nomePessoa,
        email: criado.email,
        token: criado.conviteToken,
        expiraEm: criado.conviteExpiraEm,
      });
    } else {
      toastSuccess("Usuário criado com sucesso.", "Criado");
    }

    await carregarDados();
  }, "Não foi possível criar o usuário.");
}

async function salvarEdicao() {
  if (!validarEdicao()) return;

  const usuario = editandoUsuario.value;
  if (!usuario) return;

  // Corretiva G2 (RF14): o e-mail só vai quando muda; a API sincroniza o cadastro da
  // pessoa e cancela um convite pendente.
  const emailNovo = formularioEdicao.email.trim().toLowerCase();
  const emailMudou = emailNovo !== usuario.email.trim().toLowerCase();

  await executarNoDialogo(async () => {
    await atualizarUsuario(usuario.id, {
      Perfil: formularioEdicao.perfil.trim(),
      Ativo: Boolean(formularioEdicao.ativo),
      ...(emailMudou ? { Email: emailNovo } : {}),
    });

    toastSuccess("Usuário atualizado com sucesso.", "Salvo");
    dialogEdicaoAberto.value = false;
    await carregarDados();
  }, "Não foi possível atualizar o usuário.");
}

onMounted(carregarDados);
</script>

<template>
  <div class="page-container">
    <PageHeader
      titulo="Usuários"
      subtitulo="Gestão administrativa de acesso ao sistema"
    >
      <template #acoes>
        <Button label="Novo Usuário" icon="pi pi-plus" data-testid="usuario-novo" @click="abrirNovo" />
        <Button
          label="Recarregar"
          icon="pi pi-refresh"
          severity="secondary"
          :loading="carregando"
          @click="carregarDados"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <InlineMessage
      texto="A criação de usuário sempre exige vínculo com uma pessoa já cadastrada. Por padrão, o sistema gera um link de convite para a própria pessoa definir a senha; para redefinir o acesso de uma conta, gere um novo convite. A senha nunca é digitada pelo administrador. Este módulo é restrito a administradores."
      tipo="info"
    />

    <div class="usuarios-filtros">
      <div style="display: flex; flex-direction: column; gap: 6px">
        <label for="usuario-filtro-busca">Buscar</label>
        <InputText
          id="usuario-filtro-busca"
          v-model="busca"
          placeholder="Pesquisar por nome da pessoa ou e-mail..."
        />
      </div>

      <div style="display: flex; flex-direction: column; gap: 6px">
        <label id="usuario-filtro-perfil-rotulo" for="usuario-filtro-perfil">Perfil</label>
        <Dropdown
          v-model="filtroPerfil"
          labelId="usuario-filtro-perfil"
          ariaLabelledby="usuario-filtro-perfil-rotulo"
          :options="opcoesPerfil"
          optionLabel="label"
          optionValue="value"
          showClear
          placeholder="Todos"
        />
      </div>

      <div style="display: flex; flex-direction: column; gap: 6px">
        <label id="usuario-filtro-status-rotulo" for="usuario-filtro-status">Status</label>
        <Dropdown
          v-model="filtroAtivo"
          labelId="usuario-filtro-status"
          ariaLabelledby="usuario-filtro-status-rotulo"
          :options="opcoesStatus"
          optionLabel="label"
          optionValue="value"
          showClear
          placeholder="Todos"
        />
      </div>
    </div>

    <LoadingOverlay :loading="carregando" texto="Carregando usuários...">
      <DataTable
        :value="usuariosFiltrados"
        paginator
        :rows="10"
        rowHover
        sortField="email"
        :sortOrder="1"
        dataKey="id"
        responsiveLayout="scroll"
      >
        <Column field="nomePessoa" header="Pessoa" sortable />
        <Column field="email" header="E-mail" sortable />

        <Column header="Perfil" style="width: 140px">
          <template #body="{ data }">
            <Tag :value="data.perfil" :severity="severityPerfil(data.perfil)" />
          </template>
        </Column>

        <Column header="Status" style="width: 200px">
          <template #body="{ data }">
            <div style="display: flex; gap: 6px; flex-wrap: wrap">
              <Tag
                :value="rotuloStatus(data.ativo)"
                :severity="severityStatus(data.ativo)"
              />
              <Tag
                v-if="data.convitePendente"
                value="Convite pendente"
                severity="warn"
                v-tooltip.top="
                  'A pessoa ainda não definiu a senha pelo link de convite.'
                "
              />
            </div>
          </template>
        </Column>

        <Column header="Termo" style="width: 190px">
          <template #body="{ data }">
            <div style="display: flex; flex-direction: column; gap: 4px; align-items: flex-start">
              <Tag
                :value="termoDoUsuario(data).rotulo"
                :severity="termoDoUsuario(data).severidade"
                v-tooltip.top="termoDoUsuario(data).dica"
              />
              <small
                v-if="termoDoUsuario(data).quando"
                data-testid="usuario-termo-data"
                style="font-size: 12px; opacity: 0.8"
              >
                em {{ termoDoUsuario(data).quando }}
              </small>
            </div>
          </template>
        </Column>

        <Column header="Ações" style="width: 160px">
          <template #body="{ data }">
            <div style="display: flex; gap: 8px">
              <Button
                icon="pi pi-pencil"
                severity="secondary"
                size="small"
                v-tooltip.top="'Editar'"
                :disabled="carregando"
                data-testid="usuario-editar"
                @click="abrirEdicao(data)"
              />
              <Button
                icon="pi pi-send"
                severity="info"
                size="small"
                v-tooltip.top="
                  data.convitePendente
                    ? 'Gerar novo link de convite (primeiro acesso)'
                    : 'Redefinir acesso: gerar link para a pessoa definir uma nova senha'
                "
                :disabled="carregando || !data.ativo"
                data-testid="usuario-convite"
                @click="gerarConvite(data)"
              />
            </div>
          </template>
        </Column>
      </DataTable>
    </LoadingOverlay>

    <Dialog
      v-model:visible="dialogCriacaoAberto"
      modal
      header="Novo usuário"
      :closable="!salvando"
      :dismissableMask="!salvando"
      style="width: 620px; max-width: 96vw"
    >
      <div class="page-container">
        <InlineMessage
          :texto="avisoCriacao"
          tipo="erro"
          data-testid="usuario-criacao-erro"
        />

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label id="usuario-criacao-pessoa-rotulo" for="usuario-criacao-pessoa">Pessoa *</label>
          <Dropdown
            v-model="formularioCriacao.pessoaId"
            labelId="usuario-criacao-pessoa"
            ariaLabelledby="usuario-criacao-pessoa-rotulo"
            :options="pessoasDisponiveis"
            optionLabel="nome"
            optionValue="id"
            filter
            showClear
            placeholder="Selecione a pessoa"
          />
          <FieldError
            data-testid="usuario-criacao-pessoa-erro"
            :texto="erroDoCampo('PessoaId', 'pessoaId')"
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="usuario-criacao-email">E-mail de acesso *</label>
          <InputText
            id="usuario-criacao-email"
            v-model="formularioCriacao.email"
            placeholder="email@dominio.com"
            maxlength="100"
          />
          <small
            data-testid="usuario-criacao-email-dica"
            style="font-size: 12px; color: var(--ipb-cinza-claro, #7a7a7a)"
          >
            O e-mail fica também no cadastro da pessoa.
          </small>
          <FieldError
            data-testid="usuario-criacao-email-erro"
            :texto="erroDoCampo('Email', 'email')"
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label id="usuario-criacao-perfil-rotulo" for="usuario-criacao-perfil">Perfil</label>
          <Dropdown
            v-model="formularioCriacao.perfil"
            labelId="usuario-criacao-perfil"
            ariaLabelledby="usuario-criacao-perfil-rotulo"
            :options="opcoesPerfil"
            optionLabel="label"
            optionValue="value"
            placeholder="Selecione o perfil"
          />
          <FieldError :texto="erroDoCampo('Perfil', 'perfil')" />
        </div>

        <div
          style="
            display: flex;
            align-items: center;
            gap: 8px;
            padding: 10px 12px;
            border: 1px solid rgba(0, 0, 0, 0.08);
            border-radius: 8px;
          "
        >
          <Checkbox
            v-model="formularioCriacao.definirSenhaManual"
            inputId="definirSenhaManual"
            binary
          />
          <label for="definirSenhaManual" style="cursor: pointer">
            Definir a senha manualmente (em vez de gerar link de convite)
          </label>
        </div>

        <InlineMessage
          v-if="!formularioCriacao.definirSenhaManual"
          texto="Será gerado um link de primeiro acesso, válido por 7 dias, para você enviar à pessoa (ex.: WhatsApp). Ela mesma define a senha, que não fica visível para o administrador."
          tipo="info"
        />

        <div
          v-if="formularioCriacao.definirSenhaManual"
          style="display: flex; flex-direction: column; gap: 6px"
        >
          <label for="usuario-criacao-senha">Senha inicial *</label>
          <Password
            v-model="formularioCriacao.senha"
            inputId="usuario-criacao-senha"
            toggleMask
            :feedback="false"
            :inputStyle="{ width: '100%' }"
            style="width: 100%"
          />
          <FieldError
            data-testid="usuario-criacao-senha-erro"
            :texto="erroDoCampo('Senha', 'senha')"
          />
        </div>

        <InlineMessage
          v-if="pessoasDisponiveis.length === 0"
          texto="Todas as pessoas cadastradas já possuem usuário vinculado."
          tipo="aviso"
        />
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="salvando"
          data-testid="usuario-criacao-cancelar"
          @click="dialogCriacaoAberto = false"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="salvando"
          data-testid="usuario-criacao-salvar"
          @click="salvarNovo"
        />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="dialogEdicaoAberto"
      modal
      header="Editar usuário"
      :closable="!salvando"
      :dismissableMask="!salvando"
      style="width: 520px; max-width: 96vw"
    >
      <div class="page-container">
        <InlineMessage
          :texto="avisoEdicao"
          tipo="erro"
          data-testid="usuario-edicao-erro"
        />

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="usuario-edicao-pessoa">Pessoa vinculada</label>
          <InputText
            id="usuario-edicao-pessoa"
            :modelValue="editandoUsuario?.nomePessoa ?? '-'"
            disabled
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="usuario-edicao-email">E-mail de acesso *</label>
          <InputText
            id="usuario-edicao-email"
            v-model="formularioEdicao.email"
            maxlength="100"
            data-testid="usuario-edicao-email"
          />
          <small
            data-testid="usuario-edicao-email-dica"
            style="font-size: 12px; color: var(--ipb-cinza-claro, #7a7a7a)"
          >
            Alterar o e-mail muda o login da pessoa, atualiza o cadastro dela e cancela
            um convite pendente.
          </small>
          <FieldError
            data-testid="usuario-edicao-email-erro"
            :texto="erroDoCampo('Email', 'email')"
          />
        </div>

        <div style="display: grid; grid-template-columns: 1fr 180px; gap: 12px">
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label id="usuario-edicao-perfil-rotulo" for="usuario-edicao-perfil">Perfil</label>
            <Dropdown
              v-model="formularioEdicao.perfil"
              labelId="usuario-edicao-perfil"
              ariaLabelledby="usuario-edicao-perfil-rotulo"
              :options="opcoesPerfil"
              optionLabel="label"
              optionValue="value"
              data-testid="usuario-perfil"
            />
            <FieldError :texto="erroDoCampo('Perfil', 'perfil')" />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label id="usuario-edicao-status-rotulo" for="usuario-edicao-status">Status</label>
            <Dropdown
              v-model="formularioEdicao.ativo"
              labelId="usuario-edicao-status"
              ariaLabelledby="usuario-edicao-status-rotulo"
              :options="[
                { label: 'Ativo', value: true },
                { label: 'Inativo', value: false },
              ]"
              optionLabel="label"
              optionValue="value"
              data-testid="usuario-status"
            />
            <FieldError
              data-testid="usuario-edicao-status-erro"
              :texto="erroDoCampo('Ativo', 'ativo')"
            />
          </div>
        </div>

        <InlineMessage
          v-if="
            editandoUsuario &&
            editandoUsuario.id === autenticacao.usuarioId &&
            !formularioEdicao.ativo
          "
          texto="O próprio usuário logado não pode ser desativado."
          tipo="aviso"
        />
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="salvando"
          data-testid="usuario-edicao-cancelar"
          @click="dialogEdicaoAberto = false"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="salvando"
          data-testid="usuario-salvar"
          @click="salvarEdicao"
        />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="dialogConviteAberto"
      modal
      header="Convite de primeiro acesso"
      style="width: 620px; max-width: 96vw"
    >
      <div class="page-container" v-if="conviteAtual">
        <!-- Corretiva G5: e-mail longo quebra dentro do diálogo a 360 px. -->
        <p style="margin: 0; overflow-wrap: anywhere">
          Envie o link abaixo para
          <strong>{{ conviteAtual.nomePessoa ?? conviteAtual.email }}</strong
          >. Ao abrir o link, a pessoa define a própria senha e já pode entrar
          com o e-mail <strong>{{ conviteAtual.email }}</strong
          >. Se a conta já tinha senha, ela continua válida até o link ser
          usado.
        </p>

        <div style="display: flex; gap: 8px; align-items: center">
          <InputText
            :modelValue="conviteAtual.link"
            readonly
            style="flex: 1; min-width: 0"
            @focus="($event.target as HTMLInputElement).select()"
          />
          <Button
            icon="pi pi-copy"
            severity="secondary"
            v-tooltip.top="'Copiar link'"
            @click="copiarLinkConvite"
          />
        </div>

        <Button
          label="Enviar pelo WhatsApp"
          icon="pi pi-whatsapp"
          severity="success"
          style="width: 100%"
          @click="abrirWhatsAppConvite"
        />

        <InlineMessage
          :texto="`O link é de uso único${
            conviteAtual.expiraEm
              ? ` e vale até ${formatarDataConvite(conviteAtual.expiraEm)}`
              : ''
          }. Por segurança, ele é exibido apenas agora. Se precisar, gere um novo pelo botão de convite na listagem.`"
          tipo="aviso"
        />
      </div>

      <template #footer>
        <Button
          label="Fechar"
          icon="pi pi-check"
          @click="dialogConviteAberto = false"
        />
      </template>
    </Dialog>
  </div>
</template>

<style scoped>
/* Entrega G: filtros (Buscar · Perfil · Status). As colunas fixas estouravam os 360 px
   e alargavam a página; abaixo de 576 px os campos empilham numa coluna. */
.usuarios-filtros {
  display: grid;
  grid-template-columns: minmax(0, 1.6fr) 220px 180px;
  gap: 10px;
  align-items: end;
}

.usuarios-filtros > * {
  min-width: 0;
}

@media (max-width: 575.98px) {
  .usuarios-filtros {
    grid-template-columns: minmax(0, 1fr);
  }

  .usuarios-filtros :deep(.p-inputtext),
  .usuarios-filtros :deep(.p-select) {
    width: 100%;
  }
}
</style>
