<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

import Button from "primevue/button";

import logoIPB2 from "../../assets/LOGOIPB2.png";

import TermoUsoSigilo from "../../components/termo/TermoUsoSigilo.vue";
import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";
import {
  aceitarTermo,
  obterTermoVigente,
} from "../../aplicacao/servicos/termoServico";
import type { TermoVigenteVM } from "../../aplicacao/modelos/dtos";
import { toastSuccess } from "../../aplicacao/servicos/notificacoes";

// RNF 2.5 / 13.4 (CSU02, fluxo alternativo): conta autenticada sem aceite da
// versão vigente só pode aceitar o termo ou sair. A guarda de rotas e o filtro
// global da API garantem que nada mais funcione antes disso.
const route = useRoute();
const router = useRouter();
const autenticacao = usarAutenticacaoStore();

const termo = ref<TermoVigenteVM | null>(null);
const carregando = ref(true);
const salvando = ref(false);
const erroCarregamento = ref("");
const erroAceite = ref("");
const aceito = ref(false);

const jaAceito = computed(() => !autenticacao.termoPendente);

function obterRedirectSeguro(): string {
  const q = route.query.redirecionar;
  const destino = typeof q === "string" ? q : "/";
  if (!destino.startsWith("/")) return "/";
  if (destino.startsWith("//")) return "/";
  if (destino.toLowerCase().includes("http")) return "/";
  if (destino.startsWith("/termo")) return "/";
  return destino;
}

onMounted(async () => {
  try {
    termo.value = await obterTermoVigente();
  } catch (e: any) {
    erroCarregamento.value =
      e?.response?.data?.mensagem ??
      "Não foi possível carregar o Termo de Uso e Sigilo. Tente novamente.";
  } finally {
    carregando.value = false;
  }
});

async function aceitar(versao: string) {
  erroAceite.value = "";
  salvando.value = true;
  try {
    await aceitarTermo(versao);
    autenticacao.marcarTermoPendente(false);
    toastSuccess("Termo de Uso e Sigilo aceito.", "Obrigado");
    await router.push(obterRedirectSeguro());
  } catch (e: any) {
    erroAceite.value =
      e?.response?.data?.mensagem ??
      "Não foi possível registrar o aceite. Tente novamente.";
  } finally {
    salvando.value = false;
  }
}

async function sair() {
  await autenticacao.sair();
  await router.push("/login");
}

function voltar() {
  router.push(obterRedirectSeguro());
}
</script>

<template>
  <div class="termo-pagina">
    <div class="termo-card">
      <div class="termo-marca">
        <img :src="logoIPB2" alt="IPB" class="termo-logo" />
        <div>
          <div class="termo-marca-titulo">KoinoniaHub</div>
          <div class="termo-marca-sub">
            Sistema de Gestão da Escola Bíblica Dominical
          </div>
        </div>
      </div>

      <p v-if="!jaAceito" class="termo-aviso">
        Para continuar usando o KoinoniaHub é necessário ler e aceitar a versão
        vigente do Termo de Uso e Sigilo.
      </p>
      <p v-else class="termo-aviso termo-aviso-ok">
        Você já aceitou a versão vigente deste termo. O texto fica disponível
        aqui para consulta.
      </p>

      <TermoUsoSigilo
        v-model="aceito"
        :termo="termo"
        :carregando="carregando"
        :erro="erroCarregamento"
        :salvando="salvando"
        :mostrar-botao="!jaAceito"
        @aceitar="aceitar"
      />

      <small v-if="erroAceite" class="termo-erro">{{ erroAceite }}</small>

      <div class="termo-acoes">
        <Button
          v-if="jaAceito"
          label="Voltar"
          icon="pi pi-arrow-left"
          severity="secondary"
          @click="voltar"
        />
        <Button
          label="Sair"
          icon="pi pi-sign-out"
          severity="secondary"
          text
          data-testid="termo-sair"
          :disabled="salvando"
          @click="sair"
        />
      </div>

      <div class="termo-rodape">
        Conectado como <strong>{{ autenticacao.emailUsuario }}</strong>
      </div>
    </div>
  </div>
</template>

<style scoped>
.termo-pagina {
  min-height: 100vh;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 40px 16px;
  background: var(--ipb-cinza-bg, #f7f7f7);
}

.termo-card {
  width: 100%;
  max-width: 760px;
  background: var(--ipb-branco, #fff);
  border-radius: var(--radius-md, 10px);
  padding: 32px;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.06);
  border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.termo-marca {
  display: flex;
  align-items: center;
  gap: 14px;
}

.termo-logo {
  width: 52px;
  height: auto;
}

.termo-marca-titulo {
  font-family: var(--font-display, Georgia);
  font-size: 22px;
  font-weight: 900;
  color: var(--ipb-verde-escuro, #1a3b25);
}

.termo-marca-sub {
  font-size: 12px;
  color: var(--ipb-cinza-claro, #7a7a7a);
}

.termo-aviso {
  margin: 0;
  padding: 10px 12px;
  border-radius: 8px;
  background: #fff7e6;
  border: 1px solid #f3d9a4;
  color: #8a5b00;
  font-size: 14px;
  line-height: 1.5;
}

.termo-aviso-ok {
  background: var(--ipb-verde-bg, #edf5f0);
  border-color: #cfe3d6;
  color: var(--ipb-verde-escuro, #1a3b25);
}

.termo-erro {
  color: var(--ipb-erro, #b83232);
  font-size: 13px;
}

.termo-acoes {
  display: flex;
  justify-content: space-between;
  gap: 10px;
}

.termo-rodape {
  text-align: center;
  font-size: 12px;
  color: var(--ipb-cinza-claro, #7a7a7a);
}

@media (max-width: 768px) {
  .termo-card {
    padding: 20px;
  }
}
</style>
