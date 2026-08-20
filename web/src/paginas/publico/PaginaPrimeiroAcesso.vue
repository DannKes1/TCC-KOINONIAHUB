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
  <div class="login-container">
    <div class="login-marca">
      <div class="login-marca-conteudo">
        <img :src="logoIPB2" alt="IPB" class="login-logo" />
        <h1 class="login-marca-titulo">KoinoniaHub</h1>
        <p class="login-marca-slogan">
          "Corpo vivo de Cristo vivendo em família"
        </p>
        <div class="login-marca-separador"></div>
        <p class="login-marca-descricao">
          Sistema de Gestão da Escola Bíblica Dominical
        </p>
      </div>
    </div>

    <div class="login-formulario-area">
      <div class="login-formulario-card">
        <div class="login-form-header">
          <h2 class="login-form-titulo">Primeiro acesso</h2>
          <p class="login-form-subtitulo">
            Defina a sua senha para começar a usar o KoinoniaHub.
          </p>
        </div>

        <!-- Validando o convite -->
        <div v-if="validando" class="estado-central">
          <i class="pi pi-spin pi-spinner estado-icone"></i>
          <p class="estado-texto">Validando o convite...</p>
        </div>

        <!-- Convite inválido / expirado -->
        <div v-else-if="!conviteValido && !concluido" class="login-campos">
          <p class="login-erro">{{ erro }}</p>
          <Button
            label="Ir para o login"
            icon="pi pi-sign-in"
            severity="secondary"
            class="login-botao"
            @click="irParaLogin"
          />
        </div>

        <!-- Senha definida com sucesso -->
        <div v-else-if="concluido" class="estado-central">
          <i class="pi pi-check-circle estado-icone estado-icone-sucesso"></i>
          <p class="estado-titulo">Senha definida com sucesso!</p>
          <p class="estado-texto">
            Use o e-mail <strong>{{ email }}</strong> e a senha que você acabou
            de criar para entrar.
          </p>
          <Button
            label="Ir para o login"
            icon="pi pi-sign-in"
            class="login-botao"
            @click="irParaLogin"
          />
        </div>

        <!-- Formulário de definição de senha -->
        <div v-else class="login-campos">
          <p class="saudacao">
            Olá<strong>{{ nomePessoa ? `, ${nomePessoa}` : "" }}</strong
            >! Seu acesso foi criado pelo administrador. Escolha uma senha
            pessoal para concluir.
          </p>

          <div class="login-campo">
            <label class="login-label">E-mail de acesso</label>
            <InputText :modelValue="email" disabled class="login-input" />
          </div>

          <div class="login-campo">
            <label class="login-label">Nova senha *</label>
            <Password
              v-model="novaSenha"
              placeholder="Mínimo de 6 caracteres"
              toggleMask
              :feedback="false"
              :inputStyle="{ width: '100%' }"
              style="width: 100%"
              @keyup.enter="definirSenha"
            />
          </div>

          <div class="login-campo">
            <label class="login-label">Confirmar nova senha *</label>
            <Password
              v-model="confirmarSenha"
              placeholder="Repita a senha"
              toggleMask
              :feedback="false"
              :inputStyle="{ width: '100%' }"
              style="width: 100%"
              @keyup.enter="definirSenha"
            />
          </div>

          <small v-if="erro" class="login-erro">{{ erro }}</small>

          <Button
            label="Definir senha e concluir"
            icon="pi pi-check"
            class="login-botao"
            :loading="salvando"
            @click="definirSenha"
          />

          <small class="login-ajuda">
            A senha deve ter no mínimo 6 caracteres. Ela é pessoal e não fica
            visível para o administrador.
          </small>
        </div>

        <div class="login-rodape">Igreja Presbiteriana do Brasil © 2026</div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.login-container {
  display: flex;
  min-height: 100vh;
}

.login-marca {
  flex: 1;
  background: linear-gradient(
    160deg,
    var(--ipb-verde-escuro, #1a3b25) 0%,
    var(--ipb-verde, #234f32) 100%
  );
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 48px;
  position: relative;
  overflow: hidden;
}

.login-marca::before {
  content: "";
  position: absolute;
  top: -30%;
  right: -20%;
  width: 500px;
  height: 500px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.03);
}

.login-marca::after {
  content: "";
  position: absolute;
  bottom: -20%;
  left: -10%;
  width: 400px;
  height: 400px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.02);
}

.login-marca-conteudo {
  text-align: center;
  color: #fff;
  position: relative;
  z-index: 1;
  max-width: 360px;
}

.login-logo {
  width: 120px;
  height: auto;
  margin-bottom: 28px;
  filter: drop-shadow(0 4px 12px rgba(0, 0, 0, 0.3));
}

.login-marca-titulo {
  font-family: var(--font-display, Georgia);
  font-size: 32px;
  font-weight: 900;
  letter-spacing: 0.5px;
  margin-bottom: 12px;
}

.login-marca-slogan {
  font-family: var(--font-display, Georgia);
  font-style: italic;
  font-size: 15px;
  opacity: 0.8;
  line-height: 1.5;
}

.login-marca-separador {
  width: 60px;
  height: 2px;
  background: rgba(255, 255, 255, 0.3);
  margin: 24px auto;
}

.login-marca-descricao {
  font-size: 13px;
  opacity: 0.55;
  letter-spacing: 0.3px;
}

/* ── Lado direito ── */
.login-formulario-area {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 48px;
  background: var(--ipb-cinza-bg, #f7f7f7);
}

.login-formulario-card {
  width: 100%;
  max-width: 400px;
  background: var(--ipb-branco, #fff);
  border-radius: var(--radius-md, 10px);
  padding: 40px 36px;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.06);
  border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
}

.login-form-header {
  margin-bottom: 32px;
}

.login-form-titulo {
  font-family: var(--font-display, Georgia);
  font-size: 24px;
  font-weight: 700;
  color: var(--ipb-verde-escuro, #1a3b25);
  margin-bottom: 6px;
}

.login-form-subtitulo {
  font-size: 14px;
  color: var(--ipb-cinza-claro, #7a7a7a);
}

.login-campos {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.login-campo {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.login-label {
  font-size: 13px;
  font-weight: 600;
  color: var(--ipb-cinza, #4d4d4d);
}

.login-input {
  width: 100%;
}

.login-botao {
  width: 100%;
  margin-top: 8px;
  padding: 12px;
  font-size: 15px;
  font-weight: 600;
}

.login-erro {
  color: var(--ipb-erro, #b83232);
  font-size: 13px;
  text-align: center;
}

.login-ajuda {
  font-size: 12px;
  color: var(--ipb-cinza-claro, #7a7a7a);
  text-align: center;
  line-height: 1.5;
}

.login-rodape {
  margin-top: 32px;
  text-align: center;
  font-size: 11px;
  color: var(--ipb-cinza-claro, #7a7a7a);
  opacity: 0.7;
}

/* ── Estados: validando / concluído ── */
.saudacao {
  font-size: 14px;
  line-height: 1.55;
  color: var(--ipb-cinza, #4d4d4d);
  margin: 0;
}

.estado-central {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 10px;
  padding: 8px 0;
}

.estado-icone {
  font-size: 2rem;
  color: var(--ipb-verde, #234f32);
}

.estado-icone-sucesso {
  font-size: 2.4rem;
  color: var(--ipb-sucesso, #2e7d4a);
}

.estado-titulo {
  font-family: var(--font-display, Georgia);
  font-size: 17px;
  font-weight: 700;
  color: var(--ipb-verde-escuro, #1a3b25);
  margin: 0;
}

.estado-texto {
  font-size: 14px;
  color: var(--ipb-cinza-claro, #7a7a7a);
  line-height: 1.5;
  margin: 0;
}

@media (max-width: 768px) {
  .login-container {
    flex-direction: column;
  }

  .login-marca {
    padding: 36px 24px;
    min-height: auto;
  }

  .login-logo {
    width: 80px;
    margin-bottom: 16px;
  }

  .login-marca-titulo {
    font-size: 24px;
  }

  .login-marca-separador {
    margin: 16px auto;
  }

  .login-formulario-area {
    padding: 24px;
  }

  .login-formulario-card {
    padding: 28px 24px;
  }
}
</style>
