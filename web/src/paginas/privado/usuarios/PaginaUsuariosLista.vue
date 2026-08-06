<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";


import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";


import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";


import { firstFieldError } from "../../../aplicacao/servicos/apiError";


import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";


import { listarPessoas } from "../../../aplicacao/servicos/pessoasServico";
import {
  listarUsuarios,
  criarUsuario,
  atualizarUsuario,
  resetarSenhaUsuario,
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

const editandoUsuario = ref<UsuarioVM | null>(null);
const resetandoUsuario = ref<UsuarioVM | null>(null);

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

  if (!formularioCriacao.senha.trim()) {
    return "Informe a senha inicial do usuário.";
  }

  if (formularioCriacao.senha.trim().length < 6) {
    return "A senha deve ter no mínimo 6 caracteres.";
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

async function salvarNovo() {
  const msg = validarCriacao();
  if (msg) {
    erro.value = msg;
    return;
  }

  await run(async () => {
    await criarUsuario({
      PessoaId: Number(formularioCriacao.pessoaId),
      Email: textoOuNull(formularioCriacao.email),
      Senha: formularioCriacao.senha.trim(),
      Perfil: formularioCriacao.perfil.trim(),
    });

    toastSuccess("Usuário criado com sucesso.", "Criado");
    dialogCriacaoAberto.value = false;
    await carregarDados();
  }, "Não foi possível criar o usuário.");
}

async function salvarEdicao() {
  const msg = validarEdicao();
  if (msg) {
    erro.value = msg;
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
    erro.value = msg;
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
      texto="A criação de usuário sempre exige vínculo com uma pessoa já cadastrada. Este módulo é restrito a administradores."
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

        <Column header="Status" style="width: 120px">
          <template #body="{ data }">
            <Tag
              :value="rotuloStatus(data.ativo)"
              :severity="severityStatus(data.ativo)"
            />
          </template>
        </Column>

        <Column header="Ações" style="width: 180px">
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

        <div
          style="
            display: grid;
            grid-template-columns: 1fr 220px;
            gap: 12px;
            align-items: start;
          "
        >
          <div style="display: flex; flex-direction: column; gap: 6px">
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
  </div>
</template>
