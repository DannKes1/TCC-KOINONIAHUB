<script setup lang="ts">
import { onMounted, reactive, ref, watch } from "vue";

import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";
import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";
import { firstFieldError } from "../../../aplicacao/servicos/apiError";

import Button from "primevue/button";
import InputText from "primevue/inputtext";

import {
  atualizarIgreja,
  obterIgreja,
} from "../../../aplicacao/servicos/igrejasServico";
import type { IgrejaVM } from "../../../aplicacao/modelos/dtos";

// RF44 / CSU24 — Editar Dados da Igreja. Mesmo layout de Meus Dados: cabeçalho com
// Descartar/Salvar, aviso sobre o canal de contato do termo (RNF 44.5 / 42.7) e os
// cinco campos do requisito. A rota exige Admin (RNF 44.1) e a API confere que o id
// é o da própria igreja (RNF 44.2).
const autenticacao = usarAutenticacaoStore();
const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

const dadosOriginais = ref<IgrejaVM | null>(null);

const form = reactive({
  nome: "",
  cidade: "",
  estado: "",
  email: "",
  telefone: "",
});

function popularForm(dados: IgrejaVM) {
  form.nome = dados.nome ?? "";
  form.cidade = dados.cidade ?? "";
  form.estado = dados.estado ?? "";
  form.email = dados.email ?? "";
  form.telefone = dados.telefone ?? "";
}

function limparOuNull(valor: string): string | null {
  const t = valor.trim();
  return t === "" ? null : t;
}

// Mesma higienização do celular em Meus Dados: dígitos e formatação comum.
function sanitizarTelefone(valor: string): string {
  return valor.replace(/[^\d()+\-\s]/g, "");
}

function contarDigitos(valor: string): number {
  return (valor.match(/\d/g) ?? []).length;
}

watch(
  () => form.telefone,
  (v) => {
    const limpo = sanitizarTelefone(v ?? "");
    if (limpo !== v) form.telefone = limpo;
  },
);

watch(
  () => form.estado,
  (v) => {
    const uf = (v ?? "").replace(/[^a-zA-Z]/g, "").toUpperCase().slice(0, 2);
    if (uf !== v) form.estado = uf;
  },
);

async function carregar() {
  const igrejaId = autenticacao.igrejaId;
  if (!igrejaId) {
    erro.value = "Sessão sem igreja vinculada. Entre novamente.";
    return;
  }

  await run(async () => {
    const dados = await obterIgreja(igrejaId);
    dadosOriginais.value = dados;
    popularForm(dados);
  }, "Não foi possível carregar os dados da igreja.");
}

async function salvar() {
  clearErrors();

  if (!form.nome.trim()) {
    erro.value = "Informe o nome da igreja.";
    return;
  }

  const telDigitos = contarDigitos(form.telefone);
  if (telDigitos > 0 && (telDigitos < 8 || telDigitos > 11)) {
    erro.value = "Telefone inválido. Informe DDD + número (8 a 11 dígitos).";
    return;
  }

  const igrejaId = autenticacao.igrejaId;
  if (!igrejaId) return;

  await run(async () => {
    const atualizada = await atualizarIgreja(igrejaId, {
      Nome: form.nome.trim(),
      Cidade: limparOuNull(form.cidade),
      Estado: limparOuNull(form.estado),
      Email: limparOuNull(form.email),
      Telefone: limparOuNull(form.telefone),
    });

    dadosOriginais.value = atualizada;
    popularForm(atualizada);
    toastSuccess(
      "Os dados da igreja foram atualizados. O Termo de Uso e Sigilo passa a exibir o novo contato.",
      "Salvo",
    );
  }, "Não foi possível salvar os dados da igreja.");
}

function descartarAlteracoes() {
  if (dadosOriginais.value) {
    popularForm(dadosOriginais.value);
    clearErrors();
  }
}

onMounted(carregar);
</script>

<template>
  <div class="page-container">
    <PageHeader
      titulo="Igreja"
      subtitulo="Dados cadastrais da igreja e canal de contato do Termo de Uso e Sigilo"
    >
      <template #acoes>
        <Button
          label="Descartar"
          icon="pi pi-undo"
          severity="secondary"
          :disabled="carregando || !dadosOriginais"
          @click="descartarAlteracoes"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          data-testid="igreja-salvar"
          :loading="carregando"
          :disabled="!dadosOriginais"
          @click="salvar"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <InlineMessage
      texto="O e-mail e o telefone abaixo são exibidos no Termo de Uso e Sigilo como canal para assuntos relacionados a dados pessoais. Os aceites já registrados não mudam: o hash do termo cobre apenas o texto fixo da versão."
      tipo="info"
    />

    <LoadingOverlay :loading="carregando" texto="Carregando dados da igreja...">
      <h3 style="margin: 16px 0 6px">Dados da igreja</h3>
      <div
        style="
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
          gap: 14px;
        "
      >
        <div
          style="display: flex; flex-direction: column; gap: 6px; grid-column: 1 / -1"
        >
          <label>Nome *</label>
          <InputText
            v-model="form.nome"
            data-testid="igreja-nome"
            maxlength="200"
            placeholder="Nome da igreja"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Nome') ||
              firstFieldError(fieldErrors, 'nome')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Cidade</label>
          <InputText
            v-model="form.cidade"
            data-testid="igreja-cidade"
            maxlength="100"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Cidade') ||
              firstFieldError(fieldErrors, 'cidade')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Estado (UF)</label>
          <InputText
            v-model="form.estado"
            data-testid="igreja-estado"
            maxlength="2"
            placeholder="RO"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Estado') ||
              firstFieldError(fieldErrors, 'estado')
            "
          />
        </div>
      </div>

      <h3 style="margin: 16px 0 6px">Contato</h3>
      <div
        style="
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
          gap: 14px;
        "
      >
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>E-mail</label>
          <InputText
            v-model="form.email"
            type="email"
            data-testid="igreja-email"
            maxlength="100"
            placeholder="contato@igreja.org"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Email') ||
              firstFieldError(fieldErrors, 'email')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Telefone</label>
          <InputText
            v-model="form.telefone"
            data-testid="igreja-telefone"
            maxlength="20"
            placeholder="(00) 00000-0000"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Telefone') ||
              firstFieldError(fieldErrors, 'telefone')
            "
          />
        </div>
      </div>

      <p
        v-if="dadosOriginais?.atualizadoEm"
        style="margin-top: 14px; font-size: 12px; opacity: 0.7"
      >
        Última atualização:
        {{ new Date(dadosOriginais.atualizadoEm).toLocaleString("pt-BR") }}
      </p>
    </LoadingOverlay>
  </div>
</template>
