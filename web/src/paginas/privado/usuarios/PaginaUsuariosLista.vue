<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";

import {
  toastSuccess,
  toastWarn,
} from "../../../aplicacao/servicos/notificacoes";

import { firstFieldError } from "../../../aplicacao/servicos/apiError";

import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

import { listarPessoas } from "../../../aplicacao/servicos/pessoasServico";
import {
  listarUsuarios,
  criarUsuario,
  atualizarUsuario,
  resetarSenhaUsuario,
  gerarConviteUsuario,
} from "../../../aplicacao/servicos/usuariosServico";

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
const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

const usuarios = ref<UsuarioVM[]>([]);
const pessoas = ref<PessoaVM[]>([]);

const busca = ref("");
const filtroPerfil = ref<string | null>(null);
const filtroAtivo = ref<string | null>(null);

const dialogCriacaoAberto = ref(false);
const dialogEdicaoAberto = ref(false);
const dialogResetSenhaAberto = ref(false);
const dialogConviteAberto = ref(false);

const editandoUsuario = ref<UsuarioVM | null>(null);
const resetandoUsuario = ref<UsuarioVM | null>(null);

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
  perfil: "Usuario",
  ativo: true,
});

const formularioResetSenha = reactive({
  novaSenha: "",
  confirmarSenha: "",
});

function limparCriacao() {
  formularioCriacao.pessoaId = null;
  formularioCriacao.email = "";
  formularioCriacao.senha = "";
  formularioCriacao.perfil = "Usuario";
  formularioCriacao.definirSenhaManual = false;
}

function limparResetSenha() {
  formularioResetSenha.novaSenha = "";
  formularioResetSenha.confirmarSenha = "";
}

function abrirNovo() {
  clearErrors();
  limparCriacao();
  dialogCriacaoAberto.value = true;
}

function abrirEdicao(usuario: UsuarioVM) {
  clearErrors();
  editandoUsuario.value = usuario;
  formularioEdicao.perfil = usuario.perfil || "Usuario";
  formularioEdicao.ativo = Boolean(usuario.ativo);
  dialogEdicaoAberto.value = true;
}

function abrirResetSenha(usuario: UsuarioVM) {
  clearErrors();
  resetandoUsuario.value = usuario;
  limparResetSenha();
  dialogResetSenhaAberto.value = true;
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

function validarCriacao(): string {
  if (!formularioCriacao.pessoaId) {
    return "Selecione a pessoa que será vinculada ao usuário.";
  }

  if (formularioCriacao.definirSenhaManual) {
    if (!formularioCriacao.senha.trim()) {
      return "Informe a senha inicial do usuário.";
    }

    if (formularioCriacao.senha.trim().length < 6) {
      return "A senha deve ter no mínimo 6 caracteres.";
    }
  }

  return "";
}

function validarEdicao(): string {
  if (!formularioEdicao.perfil.trim()) {
    return "Informe o perfil do usuário.";
  }

  if (
    editandoUsuario.value &&
    editandoUsuario.value.id === autenticacao.usuarioId &&
    !formularioEdicao.ativo
  ) {
    return "Você não pode desativar o seu próprio usuário.";
  }

  return "";
}

function validarResetSenha(): string {
  if (!formularioResetSenha.novaSenha.trim()) {
    return "Informe a nova senha.";
  }

  if (formularioResetSenha.novaSenha.trim().length < 6) {
    return "A nova senha deve ter no mínimo 6 caracteres.";
  }

  if (
    formularioResetSenha.novaSenha.trim() !==
    formularioResetSenha.confirmarSenha.trim()
  ) {
    return "A confirmação da senha não confere.";
  }

  return "";
}

function textoOuNull(valor?: string | null) {
  const texto = String(valor ?? "").trim();
  return texto ? texto : null;
}

function rotuloStatus(ativo: boolean) {
  return ativo ? "Ativo" : "Inativo";
}

function severityStatus(ativo: boolean) {
  return ativo ? "success" : "danger";
}

function severityPerfil(perfil: string) {
  const valor = String(perfil || "").toLowerCase();

  if (valor === "admin") return "danger";
  if (valor === "pastor") return "warning";
  if (valor === "superintendente") return "info";
  if (valor === "professor") return "help";
  return "secondary";
}

// ---------- Convite de primeiro acesso ----------

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
    document.body.removeChild(campo);
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
  const msg = validarCriacao();
  if (msg) {
    toastWarn(msg);
    return;
  }

  await run(async () => {
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
  const msg = validarEdicao();
  if (msg) {
    toastWarn(msg);
    return;
  }

  const usuario = editandoUsuario.value;
  if (!usuario) return;

  await run(async () => {
    await atualizarUsuario(usuario.id, {
      Perfil: formularioEdicao.perfil.trim(),
      Ativo: Boolean(formularioEdicao.ativo),
    });

    toastSuccess("Usuário atualizado com sucesso.", "Salvo");
    dialogEdicaoAberto.value = false;
    await carregarDados();
  }, "Não foi possível atualizar o usuário.");
}

async function salvarResetSenha() {
  const msg = validarResetSenha();
  if (msg) {
    toastWarn(msg);
    return;
  }

  const usuario = resetandoUsuario.value;
  if (!usuario) return;

  await run(async () => {
    await resetarSenhaUsuario(usuario.id, {
      NovaSenha: formularioResetSenha.novaSenha.trim(),
    });

    toastSuccess("Senha redefinida com sucesso.", "Concluído");
    dialogResetSenhaAberto.value = false;
    limparResetSenha();
  }, "Não foi possível resetar a senha do usuário.");
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
        <Button label="Novo Usuário" icon="pi pi-plus" @click="abrirNovo" />
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
      texto="A criação de usuário sempre exige vínculo com uma pessoa já cadastrada. Por padrão, o sistema gera um link de convite para a própria pessoa definir a senha. Este módulo é restrito a administradores."
      tipo="info"
    />

    <div
      style="
        display: grid;
        grid-template-columns: 1.6fr 220px 180px;
        gap: 10px;
        align-items: end;
      "
    >
      <div style="display: flex; flex-direction: column; gap: 6px">
        <label>Buscar</label>
        <InputText
          v-model="busca"
          placeholder="Pesquisar por nome da pessoa ou e-mail..."
        />
      </div>

      <div style="display: flex; flex-direction: column; gap: 6px">
        <label>Perfil</label>
        <Dropdown
          v-model="filtroPerfil"
          :options="opcoesPerfil"
          optionLabel="label"
          optionValue="value"
          showClear
          placeholder="Todos"
        />
      </div>

      <div style="display: flex; flex-direction: column; gap: 6px">
        <label>Status</label>
        <Dropdown
          v-model="filtroAtivo"
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
                severity="warning"
                v-tooltip.top="
                  'A pessoa ainda não definiu a senha pelo link de convite.'
                "
              />
            </div>
          </template>
        </Column>

        <Column header="Ações" style="width: 220px">
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
                icon="pi pi-send"
                severity="info"
                size="small"
                v-tooltip.top="'Gerar link de convite (primeiro acesso)'"
                :disabled="carregando || !data.ativo"
                @click="gerarConvite(data)"
              />
              <Button
                icon="pi pi-key"
                severity="warning"
                size="small"
                v-tooltip.top="'Resetar Senha'"
                :disabled="carregando"
                @click="abrirResetSenha(data)"
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
      :closable="!carregando"
      :dismissableMask="!carregando"
      style="width: 620px; max-width: 96vw"
    >
      <div class="page-container">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Pessoa *</label>
          <Dropdown
            v-model="formularioCriacao.pessoaId"
            :options="pessoasDisponiveis"
            optionLabel="nome"
            optionValue="id"
            filter
            showClear
            placeholder="Selecione a pessoa"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'PessoaId') ||
              firstFieldError(fieldErrors, 'pessoaId')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>E-mail</label>
          <InputText
            v-model="formularioCriacao.email"
            placeholder="Deixe em branco para usar o e-mail da pessoa"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Email') ||
              firstFieldError(fieldErrors, 'email')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Perfil</label>
          <Dropdown
            v-model="formularioCriacao.perfil"
            :options="opcoesPerfil"
            optionLabel="label"
            optionValue="value"
            placeholder="Selecione o perfil"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Perfil') ||
              firstFieldError(fieldErrors, 'perfil')
            "
          />
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
          <label>Senha inicial *</label>
          <Password
            v-model="formularioCriacao.senha"
            toggleMask
            :feedback="false"
            :inputStyle="{ width: '100%' }"
            style="width: 100%"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Senha') ||
              firstFieldError(fieldErrors, 'senha')
            "
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
          :disabled="carregando"
          @click="dialogCriacaoAberto = false"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="carregando"
          @click="salvarNovo"
        />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="dialogEdicaoAberto"
      modal
      header="Editar usuário"
      :closable="!carregando"
      :dismissableMask="!carregando"
      style="width: 520px; max-width: 96vw"
    >
      <div class="page-container">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Pessoa vinculada</label>
          <InputText
            :modelValue="editandoUsuario?.nomePessoa ?? '-'"
            disabled
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>E-mail</label>
          <InputText :modelValue="editandoUsuario?.email ?? ''" disabled />
        </div>

        <div style="display: grid; grid-template-columns: 1fr 180px; gap: 12px">
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Perfil</label>
            <Dropdown
              v-model="formularioEdicao.perfil"
              :options="opcoesPerfil"
              optionLabel="label"
              optionValue="value"
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Status</label>
            <Dropdown
              v-model="formularioEdicao.ativo"
              :options="[
                { label: 'Ativo', value: true },
                { label: 'Inativo', value: false },
              ]"
              optionLabel="label"
              optionValue="value"
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
          :disabled="carregando"
          @click="dialogEdicaoAberto = false"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="carregando"
          @click="salvarEdicao"
        />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="dialogResetSenhaAberto"
      modal
      header="Resetar senha"
      :closable="!carregando"
      :dismissableMask="!carregando"
      style="width: 520px; max-width: 96vw"
    >
      <div class="page-container">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Usuário</label>
          <InputText :modelValue="resetandoUsuario?.email ?? ''" disabled />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Nova senha *</label>
          <Password
            v-model="formularioResetSenha.novaSenha"
            toggleMask
            :feedback="false"
            :inputStyle="{ width: '100%' }"
            style="width: 100%"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'NovaSenha') ||
              firstFieldError(fieldErrors, 'novaSenha')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Confirmar nova senha *</label>
          <Password
            v-model="formularioResetSenha.confirmarSenha"
            toggleMask
            :feedback="false"
            :inputStyle="{ width: '100%' }"
            style="width: 100%"
          />
        </div>

        <InlineMessage
          texto="Dica: em vez de digitar uma senha para a pessoa, você pode fechar esta janela e usar o botão 'Gerar link de convite' — assim ela mesma define a própria senha."
          tipo="info"
        />
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="carregando"
          @click="dialogResetSenhaAberto = false"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="carregando"
          @click="salvarResetSenha"
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
        <p style="margin: 0">
          Envie o link abaixo para
          <strong>{{ conviteAtual.nomePessoa ?? conviteAtual.email }}</strong
          >. Ao abrir o link, a pessoa define a própria senha e já pode entrar
          com o e-mail <strong>{{ conviteAtual.email }}</strong
          >.
        </p>

        <div style="display: flex; gap: 8px; align-items: center">
          <InputText
            :modelValue="conviteAtual.link"
            readonly
            style="flex: 1"
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
          }. Por segurança, ele é exibido apenas agora — se precisar, gere um novo pelo botão de convite na listagem.`"
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
