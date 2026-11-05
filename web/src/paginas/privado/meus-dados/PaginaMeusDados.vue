<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue";

import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";
import { usarFormularioDialogo } from "../../../aplicacao/composables/usarFormularioDialogo";
// Corretiva G4b: máscara 00000-000 (InputMask) e validação do CEP.
import { MENSAGEM_CEP, cepParaFormulario, cepValido } from "../../../aplicacao/dominio/cep";
import InputMask from "primevue/inputmask";

import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";


import Button from "primevue/button";
import InputText from "primevue/inputtext";

import {
  obterMeusDados,
  atualizarMeusDados,
} from "../../../aplicacao/servicos/meusDadosServico";

import type { MeusDadosVM } from "../../../aplicacao/servicos/meusDadosServico";
import { rotuloMeioAceite } from "../../../aplicacao/servicos/termoServico";

const autenticacao = usarAutenticacaoStore();

// Perfis administrativos (Admin, Pastor e Superintendente), pelo getter da store.
const isAdministrativo = computed(() => autenticacao.isAdministrativo);

const { carregando, erro, run, clearErrors } = useAsync();

// Corretiva G5: erro do salvar no topo do formulário, validação local no campo e limpeza
// ao alterar ficam no composable; a instância da página fica só com a carga dos dados.
const {
  salvando, executar: executarNoFormulario, errosLocais, limpar, erroDoCampo, aviso,
  semErrosLocais,
} = usarFormularioDialogo();

const CAMPOS_DO_FORMULARIO = ["Celular", "Email", "Endereco", "Bairro", "Cidade", "Estado", "CEP"];
// Linha geral da validação local ("Corrija os campos destacados."), no mesmo lugar do
// aviso de regra da API.
const avisoLocal = ref("");
const avisoDaApi = aviso(CAMPOS_DO_FORMULARIO);
const avisoDoFormulario = computed(() => avisoDaApi.value || avisoLocal.value);

const dadosOriginais = ref<MeusDadosVM | null>(null);

const form = reactive({
  celular: "",
  email: "",
  endereco: "",
  bairro: "",
  cidade: "",
  estado: "",
  cep: "",
});

function popularForm(dados: MeusDadosVM) {
  form.celular = dados.celular ?? "";
  form.email = dados.email ?? "";
  form.endereco = dados.endereco ?? "";
  form.bairro = dados.bairro ?? "";
  form.cidade = dados.cidade ?? "";
  form.estado = dados.estado ?? "";
  form.cep = cepParaFormulario(dados.cep);
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

watch(
  () => form.celular,
  (v) => {
    const limpo = sanitizarTelefone(v ?? "");
    if (limpo !== v) form.celular = limpo;
  },
);

// RNF 4.3: validação prévia com a mensagem junto ao campo; a API aplica as mesmas
// regras (UF com 2 letras) e responde 400 por campo. O celular (8 a 11 dígitos) é
// regra só do front; a API limita a 20 caracteres. O e-mail é somente leitura
// (Corretiva G2): é o e-mail de acesso, definido na criação da conta.
// Corretiva G5: os erros locais usam o nome do campo no DTO (Celular, Estado, CEP), como nas
// outras telas.
function limparErrosDoFormulario() {
  limpar();
  avisoLocal.value = "";
}

// Qualquer alteração limpa os erros (o watch de sanitizar o celular também dispara aqui,
// sem efeito: a validação só roda no Salvar).
watch(form, limparErrosDoFormulario);

function validarLocal(): boolean {
  limpar();

  const celDigitos = contarDigitos(form.celular);
  if (celDigitos > 0 && (celDigitos < 8 || celDigitos > 11)) {
    errosLocais.Celular = "Celular inválido. Informe DDD + número (8 a 11 dígitos).";
  }

  const uf = form.estado.trim();
  if (uf && !/^[A-Za-z]{2}$/.test(uf)) {
    errosLocais.Estado = "Informe a UF com 2 letras (ex.: RO).";
  }

  if (!cepValido(form.cep)) errosLocais.CEP = MENSAGEM_CEP;

  return semErrosLocais();
}

async function salvar() {
  limparErrosDoFormulario();

  if (!validarLocal()) {
    avisoLocal.value = "Corrija os campos destacados.";
    return;
  }

  await executarNoFormulario(async () => {
    await atualizarMeusDados({
      Celular: limparOuNull(form.celular),
      Email: limparOuNull(form.email),
      Endereco: limparOuNull(form.endereco),
      Bairro: limparOuNull(form.bairro),
      Cidade: limparOuNull(form.cidade),
      Estado: limparOuNull(form.estado)?.toUpperCase() ?? null,
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
    limparErrosDoFormulario();
  }
}

// RF43: "Termo de Uso e Sigilo — versão X, aceito em DD/MM/AAAA (meio)".
const resumoAceiteTermo = computed(() => {
  const aceite = dadosOriginais.value?.aceiteTermo;
  if (!aceite) return null;
  const data = new Date(aceite.aceitoEm);
  const quando = Number.isNaN(data.getTime())
    ? ""
    : data.toLocaleString("pt-BR", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      });
  const meio = rotuloMeioAceite(aceite.meio);
  const sufixoMeio = meio ? ` (${meio})` : "";
  return {
    versao: aceite.versao,
    vigente: aceite.vigente,
    texto: `Versão ${aceite.versao}, aceito em ${quando}${sufixoMeio}`,
  };
});

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
          :disabled="salvando || carregando"
          @click="descartarAlteracoes"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="salvando"
          :disabled="carregando"
          @click="salvar"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <LoadingOverlay :loading="carregando" texto="Carregando seus dados...">
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
            Situação
          </div>
          <div>{{ dadosOriginais.situacao || "-" }}</div>
        </div>
        <div data-testid="meus-dados-termo">
          <div
            style="font-size: 11px; opacity: 0.75; text-transform: uppercase"
          >
            Termo de Uso e Sigilo
          </div>
          <div v-if="resumoAceiteTermo">
            {{ resumoAceiteTermo.texto }}
            <span v-if="!resumoAceiteTermo.vigente" style="color: #8a5b00">
              (há uma nova versão a aceitar)
            </span>
          </div>
          <div v-else style="color: #8a5b00">Aceite pendente</div>
          <RouterLink to="/termo" style="font-size: 12px">
            Ver o termo
          </RouterLink>
        </div>
      </div>

      <InlineMessage
        v-if="!isAdministrativo"
        texto="Para alterar nome ou situação, fale com a secretaria/administração da igreja."
        tipo="info"
      />
      <InlineMessage
        v-else
        texto="Para editar nome ou situação (de qualquer pessoa, incluindo o seu), use a tela Pessoas."
        tipo="info"
      />

      <InlineMessage
        :texto="avisoDoFormulario"
        tipo="erro"
        data-testid="meus-dados-formulario-erro"
      />

      <h3 style="margin: 16px 0 6px">Contato</h3>
      <div
        style="
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
          gap: 14px;
        "
      >
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="meus-dados-celular">Celular</label>
          <InputText
            id="meus-dados-celular"
            v-model="form.celular"
            placeholder="(00) 00000-0000"
            maxlength="20"
          />
          <FieldError :texto="erroDoCampo('Celular', 'celular')" />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="meus-dados-email">E-mail</label>
          <InputText
            id="meus-dados-email"
            v-model="form.email"
            maxlength="100"
            disabled
          />
          <small
            data-testid="meus-dados-email-dica"
            style="font-size: 12px; color: var(--ipb-cinza-claro, #7a7a7a)"
          >
            E-mail de acesso; definido na criação da conta.
          </small>
          <FieldError :texto="erroDoCampo('Email', 'email')" />
        </div>
      </div>

      <h3 style="margin: 16px 0 6px">Endereço</h3>
      <div class="grade-endereco">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="meus-dados-endereco">Endereço</label>
          <InputText
            id="meus-dados-endereco"
            v-model="form.endereco"
            placeholder="Rua, número, complemento"
            maxlength="500"
          />
          <FieldError :texto="erroDoCampo('Endereco', 'endereco')" />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="meus-dados-cep">CEP</label>
          <InputMask
            id="meus-dados-cep"
            v-model="form.cep"
            mask="99999-999"
            :autoClear="false"
            placeholder="00000-000"
            inputmode="numeric"
          />
          <FieldError
            data-testid="meus-dados-cep-erro"
            :texto="erroDoCampo('CEP', 'cep')"
          />
        </div>
      </div>

      <div class="grade-endereco grade-endereco-cidade">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="meus-dados-bairro">Bairro</label>
          <InputText id="meus-dados-bairro" v-model="form.bairro" maxlength="100" />
          <FieldError :texto="erroDoCampo('Bairro', 'bairro')" />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="meus-dados-cidade">Cidade</label>
          <InputText id="meus-dados-cidade" v-model="form.cidade" maxlength="100" />
          <FieldError :texto="erroDoCampo('Cidade', 'cidade')" />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="meus-dados-uf">Estado (UF)</label>
          <InputText
            id="meus-dados-uf"
            v-model="form.estado"
            maxlength="2"
            placeholder="SP"
          />
          <FieldError :texto="erroDoCampo('Estado', 'estado')" />
        </div>
      </div>
    </LoadingOverlay>
  </div>
</template>

<style scoped>
/* Proporções do desktop preservadas; no celular (Etapa 6.3) os campos empilham. */
.grade-endereco {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 14px;
}

.grade-endereco-cidade {
  grid-template-columns: 2fr 2fr 1fr;
  margin-top: 14px;
}

@media (max-width: 768px) {
  .grade-endereco,
  .grade-endereco-cidade {
    grid-template-columns: 1fr;
  }
}
</style>
