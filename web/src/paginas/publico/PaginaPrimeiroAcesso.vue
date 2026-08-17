<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

import {
  ativarPrimeiroAcessoApi,
  validarPrimeiroAcessoApi,
} from "../../aplicacao/servicos/authServico";

import logoIPB2 from "../../assets/LOGOIPB2.png";

import InputText from "primevue/inputtext";
import Password from "primevue/password";
import Button from "primevue/button";

const route = useRoute();
const router = useRouter();

const token = ref<string>("");

const validando = ref(true);
const conviteValido = ref(false);
const concluido = ref(false);
const salvando = ref(false);

const erro = ref<string>("");

const email = ref<string>("");
const nomePessoa = ref<string | null>(null);

const novaSenha = ref<string>("");
const confirmarSenha = ref<string>("");

function extrairMensagem(e: any, padrao: string): string {
  return e?.response?.data?.mensagem ?? padrao;
}

onMounted(async () => {
  const bruto = route.query.token;
  token.value = typeof bruto === "string" ? bruto.trim() : "";

  if (!token.value) {
    erro.value =
      "Link incompleto. Abra exatamente o link de convite que você recebeu.";
    validando.value = false;
    return;
  }

  try {
    const dados = await validarPrimeiroAcessoApi(token.value);
    email.value = dados.email;
    nomePessoa.value = dados.nomePessoa;
    conviteValido.value = true;
  } catch (e: any) {
    erro.value = extrairMensagem(
      e,
      "Convite inválido ou já utilizado. Solicite um novo link ao administrador.",
    );
  } finally {
    validando.value = false;
  }
});

function validarFormulario(): string {
  if (!novaSenha.value.trim()) return "Informe a nova senha.";
  if (novaSenha.value.trim().length < 6)
    return "A senha deve ter no mínimo 6 caracteres.";
  if (novaSenha.value.trim() !== confirmarSenha.value.trim())
    return "A confirmação da senha não confere.";
  return "";
}

async function definirSenha() {
  erro.value = "";

  const msg = validarFormulario();
  if (msg) {
    erro.value = msg;
    return;
  }

  salvando.value = true;
  try {
    await ativarPrimeiroAcessoApi({
      Token: token.value,
      NovaSenha: novaSenha.value.trim(),
    });
    concluido.value = true;
  } catch (e: any) {
    erro.value = extrairMensagem(
      e,
      "Não foi possível definir a senha. Tente novamente.",
    );
  } finally {
    salvando.value = false;
  }
}

function irParaLogin() {
  router.push("/login");
}
</script>

<template>
  <div style="max-width: 460px; margin: 60px auto; padding: 24px">
    <div style="text-align: center; margin-bottom: 18px">
      <img :src="logoIPB2" alt="KoinoniaHub" style="height: 72px" />
      <h2 style="margin: 12px 0 0">Primeiro acesso</h2>
      <p style="margin-top: 6px; opacity: 0.7">
        Defina a sua senha para começar a usar o KoinoniaHub.
      </p>
    </div>

    <div
      style="
        padding: 20px;
        border: 1px solid rgba(0, 0, 0, 0.08);
        border-radius: 12px;
        background: #fff;
      "
    >
      <!-- Validando o convite -->
      <div v-if="validando" style="text-align: center; padding: 12px 0">
        <i class="pi pi-spin pi-spinner" style="font-size: 1.6rem"></i>
        <p style="margin-top: 10px; opacity: 0.7">Validando o convite...</p>
      </div>

      <!-- Convite inválido / expirado -->
      <div v-else-if="!conviteValido && !concluido">
        <p style="color: #b00020; margin: 0 0 12px">{{ erro }}</p>
        <Button
          label="Ir para o login"
          icon="pi pi-sign-in"
          severity="secondary"
          style="width: 100%"
          @click="irParaLogin"
        />
      </div>

      <!-- Senha definida com sucesso -->
      <div v-else-if="concluido" style="text-align: center">
        <i
          class="pi pi-check-circle"
          style="font-size: 2.2rem; color: #2e7d32"
        ></i>
        <p style="margin: 12px 0 4px; font-weight: 700">
          Senha definida com sucesso!
        </p>
        <p style="margin: 0 0 16px; opacity: 0.75">
          Use o e-mail <strong>{{ email }}</strong> e a senha que você acabou de
          criar para entrar.
        </p>
        <Button
          label="Ir para o login"
          icon="pi pi-sign-in"
          style="width: 100%"
          @click="irParaLogin"
        />
      </div>

      <!-- Formulário de definição de senha -->
      <div v-else style="display: flex; flex-direction: column; gap: 14px">
        <p style="margin: 0">
          Olá<strong>{{ nomePessoa ? `, ${nomePessoa}` : "" }}</strong
          >! Seu acesso foi criado pelo administrador. Escolha uma senha pessoal
          para concluir.
        </p>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>E-mail de acesso</label>
          <InputText :modelValue="email" disabled />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Nova senha *</label>
          <Password
            v-model="novaSenha"
            toggleMask
            :feedback="false"
            :inputStyle="{ width: '100%' }"
            style="width: 100%"
            @keyup.enter="definirSenha"
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Confirmar nova senha *</label>
          <Password
            v-model="confirmarSenha"
            toggleMask
            :feedback="false"
            :inputStyle="{ width: '100%' }"
            style="width: 100%"
            @keyup.enter="definirSenha"
          />
        </div>

        <small v-if="erro" style="color: #b00020">{{ erro }}</small>

        <Button
          label="Definir senha e concluir"
          icon="pi pi-check"
          :loading="salvando"
          style="width: 100%"
          @click="definirSenha"
        />

        <small style="opacity: 0.6; text-align: center">
          A senha deve ter no mínimo 6 caracteres. Ela é pessoal e não fica
          visível para o administrador.
        </small>
      </div>
    </div>
  </div>
</template>
