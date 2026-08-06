<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";
import { registrarAdminApi } from "../../aplicacao/servicos/authServico";

import FieldError from "../../components/ui/FieldError.vue";
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

function validarRapido(): string {
  if (!nomeIgreja.value.trim()) return "Informe o nome da igreja.";
  if (!nomeAdmin.value.trim()) return "Informe o nome do administrador.";
  if (!emailAdmin.value.trim()) return "Informe o e-mail do administrador.";
  if (!senhaAdmin.value) return "Informe a senha do administrador.";
  if (senhaAdmin.value.length < 6)
    return "A senha deve ter pelo menos 6 caracteres.";
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
    });

    if (resposta?.Token || resposta?.token) {
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

      <div style="display: flex; gap: 10px; justify-content: flex-end">
        <RouterLink to="/login" style="align-self: center; opacity: 0.8">
          Já tenho login
        </RouterLink>

        <button
          @click="concluirCadastro"
          :disabled="carregando"
          style="padding: 10px 14px"
        >
          {{ carregando ? "Concluindo..." : "Concluir Cadastro" }}
        </button>
      </div>
    </div>
  </div>
</template>
a 