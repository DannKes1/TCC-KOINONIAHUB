<script setup lang="ts">
import { ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";
import { loginApi } from "../../aplicacao/servicos/authServico";

import FieldError from "../../components/ui/FieldError.vue";
import { firstFieldError } from "../../aplicacao/servicos/apiError";
import { useAsync } from "../../aplicacao/composables/useAsync";

import logoIPB2 from "../../assets/LOGOIPB2.png";

import InputText from "primevue/inputtext";
import Password from "primevue/password";
import Button from "primevue/button";

const router = useRouter();
const route = useRoute();
const autenticacao = usarAutenticacaoStore();

const email = ref("");
const senha = ref("");

const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

function obterRedirectSeguro(): string {
  const q = route.query.redirecionar;
  const destino = typeof q === "string" ? q : "/";
  if (!destino.startsWith("/")) return "/";
  if (destino.startsWith("//")) return "/";
  if (destino.toLowerCase().includes("http")) return "/";
  return destino;
}

async function entrar() {
  clearErrors();
  await run(async () => {
    const dados = await loginApi({ Email: email.value, Senha: senha.value });
    autenticacao.entrar(dados);
    const destino = obterRedirectSeguro();
    await router.push(destino);
  }, "Não foi possível entrar.");
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
          <h2 class="login-form-titulo">Bem-vindo</h2>
          <p class="login-form-subtitulo">
            Faça login para acessar o painel administrativo
          </p>
        </div>

        <div class="login-campos">
          <div class="login-campo">
            <label class="login-label">E-mail</label>
            <InputText
              v-model="email"
              placeholder="seu@email.com"
              class="login-input"
              @keyup.enter="entrar"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'Email') ||
                firstFieldError(fieldErrors, 'email')
              "
            />
          </div>

          <div class="login-campo">
            <label class="login-label">Senha</label>
            <Password
              v-model="senha"
              placeholder="Sua senha"
              toggleMask
              :feedback="false"
              :inputStyle="{ width: '100%' }"
              style="width: 100%"
              @keyup.enter="entrar"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'Senha') ||
                firstFieldError(fieldErrors, 'senha')
              "
            />
          </div>

          <Button
            label="Entrar"
            icon="pi pi-sign-in"
            class="login-botao"
            :loading="carregando"
            @click="entrar"
          />

          <small v-if="erro" class="login-erro">{{ erro }}</small>
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

.login-rodape {
  margin-top: 32px;
  text-align: center;
  font-size: 11px;
  color: var(--ipb-cinza-claro, #7a7a7a);
  opacity: 0.7;
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
