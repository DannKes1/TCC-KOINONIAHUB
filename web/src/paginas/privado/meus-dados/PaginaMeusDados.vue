<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue";

// Store
import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

// UI base
import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";

// Composable
import { useAsync } from "../../../aplicacao/composables/useAsync";

// Notificações padronizadas
import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";

// Helpers de erro por campo
import { firstFieldError } from "../../../aplicacao/servicos/apiError";

// PrimeVue
import Button from "primevue/button";
import InputText from "primevue/inputtext";

// Serviços
import {
  obterMeusDados,
  atualizarMeusDados,
} from "../../../aplicacao/servicos/meusDadosServico";

import type { MeusDadosVM } from "../../../aplicacao/servicos/meusDadosServico";

const autenticacao = usarAutenticacaoStore();

const isAdministrativo = computed(() => {
  const p = (autenticacao.perfil || "").trim().toLowerCase();
  return ["admin", "pastor", "lider"].includes(p);
});

const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

const dadosOriginais = ref<MeusDadosVM | null>(null);

const form = reactive({
  telefone: "",
  celular: "",
  email: "",
  endereco: "",
  bairro: "",
  cidade: "",
  estado: "",
  cep: "",
});

function popularForm(dados: MeusDadosVM) {
  form.telefone = dados.telefone ?? "";
  form.celular = dados.celular ?? "";
  form.email = dados.email ?? "";
  form.endereco = dados.endereco ?? "";
  form.bairro = dados.bairro ?? "";
  form.cidade = dados.cidade ?? "";
  form.estado = dados.estado ?? "";
  form.cep = dados.cep ?? "";
}

async function carregar() {
  await run(async () => {
    const dados = await obterMeusDados();
    dadosOriginais.value = dados;
    popularForm(dados);
  }, "Não foi possível carregar seus dados.");
}

function limparOuNull(valor: string): string | null {
  const t = valor.trim();
  return t === "" ? null : t;
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
  () => form.telefone,
  (v) => {
    const limpo = sanitizarTelefone(v ?? "");
    if (limpo !== v) form.telefone = limpo;
  },
);
watch(
  () => form.celular,
  (v) => {
    const limpo = sanitizarTelefone(v ?? "");
    if (limpo !== v) form.celular = limpo;
  },
);

async function salvar() {
  clearErrors();

  const telDigitos = contarDigitos(form.telefone);
  if (telDigitos > 0 && (telDigitos < 8 || telDigitos > 11)) {
    erro.value = "Telefone inválido. Informe DDD + número (8 a 11 dígitos).";
    return;
  }

  const celDigitos = contarDigitos(form.celular);
  if (celDigitos > 0 && (celDigitos < 8 || celDigitos > 11)) {
    erro.value = "Celular inválido. Informe DDD + número (8 a 11 dígitos).";
    return;
  }

  await run(async () => {
    await atualizarMeusDados({
      Telefone: limparOuNull(form.telefone),
      Celular: limparOuNull(form.celular),
      Email: limparOuNull(form.email),
      Endereco: limparOuNull(form.endereco),
      Bairro: limparOuNull(form.bairro),
      Cidade: limparOuNull(form.cidade),
      Estado: limparOuNull(form.estado),
      CEP: limparOuNull(form.cep),
    });

    toastSuccess("Seus dados foram atualizados com sucesso.", "Salvo");
    await carregar();
  }, "Não foi possível salvar seus dados.");
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
      titulo="Meus Dados"
      subtitulo="Atualize seus dados de contato e endereço"
    >
      <template #acoes>
        <Button
          label="Descartar"
          icon="pi pi-undo"
          severity="secondary"
          :disabled="carregando"
          @click="descartarAlteracoes"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="carregando"
          @click="salvar"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <LoadingOverlay :loading="carregando" texto="Carregando seus dados...">
      <!-- Identidade (somente leitura) -->
      <div
        v-if="dadosOriginais"
        style="
          padding: 14px;
          border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
          border-radius: 10px;
          background: var(--ipb-verde-bg, #edf5f0);
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
          gap: 14px;
        "
      >
        <div>
          <div
            style="font-size: 11px; opacity: 0.75; text-transform: uppercase"
          >
            Nome
          </div>
          <div style="font-weight: 600">{{ dadosOriginais.nome }}</div>
        </div>
        <div>
          <div
            style="font-size: 11px; opacity: 0.75; text-transform: uppercase"
          >
            CPF
          </div>
          <div>{{ dadosOriginais.cpf || "—" }}</div>
        </div>
        <div>
          <div
            style="font-size: 11px; opacity: 0.75; text-transform: uppercase"
          >
            Situação
          </div>
          <div>{{ dadosOriginais.situacao || "—" }}</div>
        </div>
        <div>
          <div
            style="font-size: 11px; opacity: 0.75; text-transform: uppercase"
          >
            Categoria
          </div>
          <div>{{ dadosOriginais.categoria || "—" }}</div>
        </div>
      </div>

      <InlineMessage
        v-if="!isAdministrativo"
        texto="Para alterar nome, CPF, situação ou categoria, fale com a secretaria/administração da igreja."
        tipo="info"
      />
      <InlineMessage
        v-else
        texto="Para editar nome, CPF, situação ou categoria (de qualquer pessoa, incluindo o seu), use a tela Pessoas."
        tipo="info"
      />

      <!-- Contato -->
      <h3 style="margin: 16px 0 6px">Contato</h3>
      <div
        style="
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
          gap: 14px;
        "
      >
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Telefone</label>
          <InputText v-model="form.telefone" placeholder="(00) 0000-0000" />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Telefone') ||
              firstFieldError(fieldErrors, 'telefone')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Celular</label>
          <InputText v-model="form.celular" placeholder="(00) 00000-0000" />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Celular') ||
              firstFieldError(fieldErrors, 'celular')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>E-mail</label>
          <InputText
            v-model="form.email"
            type="email"
            placeholder="seu@email.com"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Email') ||
              firstFieldError(fieldErrors, 'email')
            "
          />
        </div>
      </div>

      <!-- Endereço -->
      <h3 style="margin: 16px 0 6px">Endereço</h3>
      <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 14px">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Endereço</label>
          <InputText
            v-model="form.endereco"
            placeholder="Rua, número, complemento"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Endereco') ||
              firstFieldError(fieldErrors, 'endereco')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>CEP</label>
          <InputText v-model="form.cep" placeholder="00000-000" />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'CEP') ||
              firstFieldError(fieldErrors, 'cep')
            "
          />
        </div>
      </div>

      <div
        style="
          display: grid;
          grid-template-columns: 2fr 2fr 1fr;
          gap: 14px;
          margin-top: 14px;
        "
      >
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Bairro</label>
          <InputText v-model="form.bairro" />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Bairro') ||
              firstFieldError(fieldErrors, 'bairro')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Cidade</label>
          <InputText v-model="form.cidade" />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Cidade') ||
              firstFieldError(fieldErrors, 'cidade')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Estado (UF)</label>
          <InputText v-model="form.estado" maxlength="2" placeholder="SP" />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Estado') ||
              firstFieldError(fieldErrors, 'estado')
            "
          />
        </div>
      </div>
    </LoadingOverlay>
  </div>
</template>
