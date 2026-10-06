<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";
import { registrarAdminApi } from "../../aplicacao/servicos/authServico";
import { obterTermoVigente } from "../../aplicacao/servicos/termoServico";
import type {
  TermoIgrejaVM,
  TermoVigenteVM,
} from "../../aplicacao/modelos/dtos";

import FieldError from "../../components/ui/FieldError.vue";
import TermoUsoSigilo from "../../components/termo/TermoUsoSigilo.vue";
import { firstFieldError } from "../../aplicacao/servicos/apiError";
import { useAsync } from "../../aplicacao/composables/useAsync";

const router = useRouter();
const autenticacao = usarAutenticacaoStore();

const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

const nomeIgreja = ref<string>("");
const cidade = ref<string>("");
const estado = ref<string>("");
const emailIgreja = ref<string>("");

const nomeAdmin = ref<string>("");
const emailAdmin = ref<string>("");
const senhaAdmin = ref<string>("");

// RNF 42.1: o Termo de Uso e Sigilo é aceito aqui mesmo, antes de criar a igreja
// e o Admin. A igreja ainda não existe, então o cabeçalho do termo (RNF 42.7)
// usa o que está sendo digitado no formulário.
const termo = ref<TermoVigenteVM | null>(null);
const termoCarregando = ref(true);
const termoErro = ref("");
const aceiteTermo = ref(false);

const igrejaDoFormulario = computed<TermoIgrejaVM>(() => ({
  nome: nomeIgreja.value.trim(),
  email: emailIgreja.value.trim() || null,
  telefone: null,
}));

const podeConcluir = computed(
  () => Boolean(termo.value) && aceiteTermo.value && !carregando.value,
);

onMounted(async () => {
  try {
    termo.value = await obterTermoVigente();
  } catch (e: any) {
    termoErro.value =
      e?.response?.data?.mensagem ??
      "Não foi possível carregar o Termo de Uso e Sigilo. Recarregue a página.";
  } finally {
    termoCarregando.value = false;
  }
});

function validarRapido(): string {
  if (!nomeIgreja.value.trim()) return "Informe o nome da igreja.";
  if (!nomeAdmin.value.trim()) return "Informe o nome do administrador.";
  if (!emailAdmin.value.trim()) return "Informe o e-mail do administrador.";
  if (!senhaAdmin.value) return "Informe a senha do administrador.";
  if (senhaAdmin.value.length < 6)
    return "A senha deve ter pelo menos 6 caracteres.";
  if (!termo.value || !aceiteTermo.value)
    return "É necessário aceitar o Termo de Uso e Sigilo para concluir.";
  return "";
}

async function concluirCadastro(): Promise<void> {
  clearErrors();

  const msg = validarRapido();
  if (msg) {
    erro.value = msg;
    return;
  }

  await run(async () => {
    const resposta = await registrarAdminApi({
      Igreja: {
        Nome: nomeIgreja.value.trim(),
        Cidade: cidade.value.trim() || null,
        Estado: estado.value.trim() || null,
        Email: emailIgreja.value.trim() || null,
      },
      EmailAdmin: emailAdmin.value.trim(),
      SenhaAdmin: senhaAdmin.value,
      NomeAdmin: nomeAdmin.value.trim(),
      AceiteTermoVersao: termo.value!.versao,
    });

    // O cookie de sessão já foi gravado pela API; o corpo traz os dados do
    // usuário criado (sem o token, monografia 4.8).
    if (resposta?.UsuarioId || resposta?.usuarioId) {
      autenticacao.entrar(resposta);
      await router.push("/");
      return;
    }

    await router.push("/login");
  }, "Não foi possível concluir o cadastro inicial.");
}
</script>

<template>
  <div style="max-width: 560px; margin: 60px auto; padding: 24px">
    <h2 style="margin: 0">Cadastro Inicial</h2>
    <p style="margin-top: 6px; opacity: 0.7">
      Crie a igreja e o primeiro usuário administrador.
    </p>

    <small v-if="erro" style="color: #b00020">{{ erro }}</small>

    <div
      style="margin-top: 16px; display: flex; flex-direction: column; gap: 14px"
    >
      <div
        style="
          padding: 12px;
          border: 1px solid rgba(0, 0, 0, 0.08);
          border-radius: 12px;
        "
      >
        <div style="font-weight: 700; margin-bottom: 10px">Dados da Igreja</div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px">
          <div style="grid-column: 1 / -1">
            <input
              v-model="nomeIgreja"
              placeholder="Nome da Igreja"
              style="width: 100%"
              data-testid="cadastro-nome-igreja"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'Igreja.Nome') ||
                firstFieldError(fieldErrors, 'igreja.Nome') ||
                firstFieldError(fieldErrors, 'Nome') ||
                firstFieldError(fieldErrors, 'nome')
              "
            />
          </div>

          <div>
            <input
              v-model="cidade"
              placeholder="Cidade (opcional)"
              style="width: 100%"
              data-testid="cadastro-cidade"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'Igreja.Cidade') ||
                firstFieldError(fieldErrors, 'igreja.Cidade') ||
                firstFieldError(fieldErrors, 'Cidade') ||
                firstFieldError(fieldErrors, 'cidade')
              "
            />
          </div>

          <div>
            <input
              v-model="estado"
              placeholder="Estado (opcional)"
              style="width: 100%"
              data-testid="cadastro-estado"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'Igreja.Estado') ||
                firstFieldError(fieldErrors, 'igreja.Estado') ||
                firstFieldError(fieldErrors, 'Estado') ||
                firstFieldError(fieldErrors, 'estado')
              "
            />
          </div>

          <div style="grid-column: 1 / -1">
            <input
              v-model="emailIgreja"
              placeholder="E-mail da igreja (opcional)"
              style="width: 100%"
              data-testid="cadastro-email-igreja"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'Igreja.Email') ||
                firstFieldError(fieldErrors, 'igreja.Email') ||
                firstFieldError(fieldErrors, 'Email') ||
                firstFieldError(fieldErrors, 'email')
              "
            />
          </div>
        </div>
      </div>

      <div
        style="
          padding: 12px;
          border: 1px solid rgba(0, 0, 0, 0.08);
          border-radius: 12px;
        "
      >
        <div style="font-weight: 700; margin-bottom: 10px">Administrador</div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px">
          <div style="grid-column: 1 / -1">
            <input
              v-model="nomeAdmin"
              placeholder="Nome do Administrador"
              style="width: 100%"
              data-testid="cadastro-nome-admin"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'NomeAdmin') ||
                firstFieldError(fieldErrors, 'nomeAdmin')
              "
            />
          </div>

          <div style="grid-column: 1 / -1">
            <input
              v-model="emailAdmin"
              placeholder="E-mail do Administrador"
              style="width: 100%"
              data-testid="cadastro-email-admin"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'EmailAdmin') ||
                firstFieldError(fieldErrors, 'emailAdmin')
              "
            />
          </div>

          <div style="grid-column: 1 / -1">
            <input
              v-model="senhaAdmin"
              type="password"
              placeholder="Senha"
              style="width: 100%"
              data-testid="cadastro-senha-admin"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'SenhaAdmin') ||
                firstFieldError(fieldErrors, 'senhaAdmin')
              "
            />
          </div>
        </div>
      </div>

      <div
        style="
          padding: 12px;
          border: 1px solid rgba(0, 0, 0, 0.08);
          border-radius: 12px;
        "
      >
        <TermoUsoSigilo
          v-model="aceiteTermo"
          :termo="termo"
          :igreja="igrejaDoFormulario"
          :carregando="termoCarregando"
          :erro="termoErro"
          :mostrar-botao="false"
        />
      </div>

      <div style="display: flex; gap: 10px; justify-content: flex-end">
        <RouterLink to="/login" style="align-self: center; opacity: 0.8">
          Já tenho login
        </RouterLink>

        <button
          @click="concluirCadastro"
          :disabled="!podeConcluir"
          style="padding: 10px 14px"
          data-testid="cadastro-concluir"
        >
          {{ carregando ? "Concluindo..." : "Concluir Cadastro" }}
        </button>
      </div>
    </div>
  </div>
</template>
