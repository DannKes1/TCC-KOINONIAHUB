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

import logoIPB2 from "../../assets/LOGOIPB2.png";

import InputText from "primevue/inputtext";
import Password from "primevue/password";
import Button from "primevue/button";

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
      <div class="login-formulario-card cadastro-card">
        <div class="login-form-header">
          <h2 class="login-form-titulo">Cadastro Inicial</h2>
          <p class="login-form-subtitulo">
            Crie a igreja e o primeiro usuário administrador.
          </p>
        </div>

        <div class="login-campos">
          <section class="cadastro-secao">
            <h3 class="cadastro-secao-titulo">Dados da Igreja</h3>

            <div class="cadastro-grade">
              <div class="login-campo cadastro-largo">
                <label class="login-label" for="cadastro-nome-igreja">Nome da igreja *</label>
                <InputText
                  id="cadastro-nome-igreja"
                  v-model="nomeIgreja"
                  placeholder="Nome da Igreja"
                  class="login-input"
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

              <div class="login-campo">
                <label class="login-label" for="cadastro-cidade">Cidade</label>
                <InputText
                  id="cadastro-cidade"
                  v-model="cidade"
                  placeholder="Cidade (opcional)"
                  class="login-input"
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

              <div class="login-campo">
                <label class="login-label" for="cadastro-estado">Estado</label>
                <InputText
                  id="cadastro-estado"
                  v-model="estado"
                  placeholder="Estado (opcional)"
                  class="login-input"
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

              <div class="login-campo cadastro-largo">
                <label class="login-label" for="cadastro-email-igreja">E-mail da igreja</label>
                <InputText
                  id="cadastro-email-igreja"
                  v-model="emailIgreja"
                  placeholder="E-mail da igreja (opcional)"
                  class="login-input"
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
          </section>

          <section class="cadastro-secao">
            <h3 class="cadastro-secao-titulo">Administrador</h3>

            <div class="cadastro-grade">
              <div class="login-campo cadastro-largo">
                <label class="login-label" for="cadastro-nome-admin">Nome do administrador *</label>
                <InputText
                  id="cadastro-nome-admin"
                  v-model="nomeAdmin"
                  placeholder="Nome do Administrador"
                  class="login-input"
                  data-testid="cadastro-nome-admin"
                />
                <FieldError
                  :texto="
                    firstFieldError(fieldErrors, 'NomeAdmin') ||
                    firstFieldError(fieldErrors, 'nomeAdmin')
                  "
                />
              </div>

              <div class="login-campo cadastro-largo">
                <label class="login-label" for="cadastro-email-admin">E-mail do administrador *</label>
                <InputText
                  id="cadastro-email-admin"
                  v-model="emailAdmin"
                  placeholder="E-mail do Administrador"
                  class="login-input"
                  data-testid="cadastro-email-admin"
                />
                <FieldError
                  :texto="
                    firstFieldError(fieldErrors, 'EmailAdmin') ||
                    firstFieldError(fieldErrors, 'emailAdmin')
                  "
                />
              </div>

              <div class="login-campo cadastro-largo">
                <label class="login-label" for="cadastro-senha-admin">Senha *</label>
                <Password
                  v-model="senhaAdmin"
                  inputId="cadastro-senha-admin"
                  placeholder="Senha"
                  toggleMask
                  :feedback="false"
                  :inputStyle="{ width: '100%' }"
                  :pt="{ pcInputText: { root: { 'data-testid': 'cadastro-senha-admin' } } }"
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
          </section>

          <section class="cadastro-secao">
            <TermoUsoSigilo
              v-model="aceiteTermo"
              :termo="termo"
              :igreja="igrejaDoFormulario"
              :carregando="termoCarregando"
              :erro="termoErro"
              :mostrar-botao="false"
              aviso-aceite
            />
          </section>

          <small v-if="erro" class="login-erro">{{ erro }}</small>

          <Button
            label="Concluir Cadastro"
            icon="pi pi-check"
            class="login-botao"
            data-testid="cadastro-concluir"
            :loading="carregando"
            :disabled="!podeConcluir"
            @click="concluirCadastro"
          />

          <RouterLink to="/login" class="cadastro-link-login">
            Já tenho login
          </RouterLink>
        </div>

        <div class="login-rodape">Igreja Presbiteriana do Brasil © 2026</div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Mesma moldura das telas públicas (Login e Primeiro Acesso). */
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
  /* O formulário é mais alto que a janela: a marca acompanha a rolagem. */
  position: sticky;
  top: 0;
  align-self: flex-start;
  min-height: 100vh;
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

/* O cadastro tem mais campos que o login: cartão mais largo. */
.cadastro-card {
  max-width: 560px;
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
  min-width: 0;
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

/* ── Seções do cadastro ── */
.cadastro-secao {
  padding: 16px;
  border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  border-radius: var(--radius-md, 10px);
  background: var(--ipb-cinza-bg, #f7f7f7);
}

.cadastro-secao-titulo {
  font-family: var(--font-display, Georgia);
  font-size: 15px;
  font-weight: 700;
  color: var(--ipb-verde-escuro, #1a3b25);
  margin: 0 0 12px;
}

.cadastro-grade {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
}

.cadastro-largo {
  grid-column: 1 / -1;
}

.cadastro-link-login {
  align-self: center;
  font-size: 13px;
  color: var(--ipb-verde, #234f32);
  text-decoration: none;
}

.cadastro-link-login:hover {
  text-decoration: underline;
}

@media (max-width: 768px) {
  .login-container {
    flex-direction: column;
  }

  .login-marca {
    padding: 36px 24px;
    /* relative (não static): mantém o recorte dos círculos decorativos. */
    position: relative;
    align-self: auto;
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

  /* Margens menores que no login: o cartão tem três seções aninhadas. */
  .login-formulario-area {
    padding: 16px;
  }

  .login-formulario-card {
    padding: 24px 16px;
  }

  .cadastro-secao {
    padding: 12px;
  }
}
</style>
