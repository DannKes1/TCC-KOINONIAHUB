import { chromium } from "@playwright/test";
import type { FullConfig } from "@playwright/test";

import { apiURL, executavelChromium } from "./apoio/ambiente";
import {
  ARQUIVO_ESTADO_ADMIN,
  ARQUIVO_ESTADO_ALUNO,
  ARQUIVO_ESTADO_PROFESSOR,
  criarSemente,
} from "./apoio/semente";

// Roda uma vez antes dos testes, com os servidores já no ar (Plano 7.3):
// 1. semeia uma igreja nova pela API;
// 2. faz login pela tela como Admin, Professor e aluno (Usuario) e guarda o
//    storageState (cookie httpOnly da sessão + localStorage da SPA) em
//    e2e/.auth/*.json, para os fluxos que começam já autenticados.

async function salvarEstadoDeLogin(
  baseURL: string,
  email: string,
  senha: string,
  destino: string,
) {
  const navegador = await chromium.launch({
    executablePath: executavelChromium,
  });
  const contexto = await navegador.newContext({
    baseURL,
    ignoreHTTPSErrors: true,
  });
  const pagina = await contexto.newPage();
  try {
    await pagina.goto("/login");
    await pagina.getByTestId("login-email").fill(email);
    await pagina.locator("#login-senha").fill(senha);
    await pagina.getByTestId("login-entrar").click();
    await pagina.waitForURL((url) => url.pathname === "/", { timeout: 20_000 });
    await contexto.storageState({ path: destino });
  } catch (erro) {
    throw new Error(
      `global-setup: não foi possível entrar como ${email} pela tela de login (${pagina.url()}). ${String(erro)}`,
    );
  } finally {
    await navegador.close();
  }
}

export default async function globalSetup(config: FullConfig) {
  const baseURL = config.projects[0]?.use?.baseURL ?? "http://localhost:5173";

  const semente = await criarSemente(apiURL);

  await salvarEstadoDeLogin(
    baseURL,
    semente.admin.email,
    semente.admin.senha,
    ARQUIVO_ESTADO_ADMIN,
  );
  await salvarEstadoDeLogin(
    baseURL,
    semente.professor.email,
    semente.professor.senha,
    ARQUIVO_ESTADO_PROFESSOR,
  );
  await salvarEstadoDeLogin(
    baseURL,
    semente.aluno.email,
    semente.aluno.senha,
    ARQUIVO_ESTADO_ALUNO,
  );

  console.log(
    `[e2e] semente ${semente.id}: igreja "${semente.igreja.nome}", turma "${semente.turma.nome}".`,
  );
}
