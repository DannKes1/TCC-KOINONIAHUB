<script setup lang="ts">
import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";
import logoIPB2 from "../../assets/LOGOIPB2.png";

const autenticacao = usarAutenticacaoStore();
</script>

<template>
  <header class="header-ipb">
    <div class="header-marca">
      <img
        :src="logoIPB2"
        alt="Igreja Presbiteriana do Brasil"
        class="header-logo"
      />
      <div class="header-textos">
        <h1>KoinoniaHub</h1>
        <span class="header-slogan"
          >Corpo vivo de Cristo vivendo em família</span
        >
      </div>
    </div>

    <div class="header-usuario">
      <div class="header-usuario-nome">
        {{ autenticacao.emailUsuario || "Usuário" }}
      </div>
      <div class="header-usuario-perfil">
        Perfil: {{ autenticacao.perfil || "-" }}
      </div>
    </div>
  </header>
</template>

<style scoped>
.header-ipb {
  height: var(--header-h, 64px);
  background: var(--ipb-verde-escuro, #1a3b25);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  position: relative;
  z-index: 100;
}

.header-marca {
  display: flex;
  align-items: center;
  gap: 14px;
}

.header-logo {
  height: 44px;
  width: auto;
  object-fit: contain;
}

.header-textos h1 {
  font-family: var(--font-display, Georgia);
  font-size: 18px;
  font-weight: 700;
  letter-spacing: 0.3px;
  line-height: 1.2;
}

.header-slogan {
  font-size: 11px;
  opacity: 0.7;
  font-style: italic;
  font-weight: 300;
  letter-spacing: 0.2px;
}

.header-usuario {
  text-align: right;
  font-size: 13px;
}

.header-usuario-nome {
  font-weight: 600;
}

.header-usuario-perfil {
  font-size: 11px;
  opacity: 0.65;
}
</style>
