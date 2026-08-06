import axios from "axios";
import { usarAutenticacaoStore } from "../armazenamentos/autenticacaoStore";
import { toastError, toastWarn } from "./notificacoes";

export const clienteHttp = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 20000,
  // Envia e recebe o cookie httpOnly de autenticação (kh_token) em toda requisição.
  withCredentials: true,
});

// A autenticação viaja no cookie httpOnly, gravado pelo servidor no login.
// Não há mais injeção do header Authorization: Bearer no cliente.

clienteHttp.interceptors.response.use(
  (resposta) => resposta,
  (erro) => {
    const status = erro?.response?.status;

    const urlAtual =
      window.location.pathname + window.location.search + window.location.hash;

    // 0) Erro de rede (sem response)
    if (!erro?.response) {
      toastError(
        "Não foi possível conectar ao servidor. Verifique se a API está rodando e se a URL está correta.",
        "Servidor indisponível",
      );
      erro.message =
        "Não foi possível conectar ao servidor. Verifique se a API está rodando e a URL está correta.";
      return Promise.reject(erro);
    }

    // 1) 401: sessão inválida/expirada → limpa estado + redirect preservando rota
    if (status === 401) {
      const autenticacao = usarAutenticacaoStore();
      autenticacao.$reset();
      localStorage.removeItem("koinoniahub_sessao");

      const estaEmPublica =
        window.location.pathname.startsWith("/login") ||
        window.location.pathname.startsWith("/cadastro-inicial");

      if (!estaEmPublica) {
        const destino = encodeURIComponent(urlAtual);
        window.location.href = `/login?redirecionar=${destino}`;
      } else {
        window.location.href = "/login";
      }

      return Promise.reject(erro);
    }

    // 2) 403: sem permissão → toast + mensagem padrão
    if (status === 403) {
      const data = erro?.response?.data;

      const msg =
        data?.mensagem ?? "Você não tem permissão para acessar este recurso.";

      erro.response.data = {
        ...(typeof data === "object" ? data : {}),
        mensagem: msg,
      };

      toastWarn(
        `${msg} Se você acredita que isso é um engano, peça a um Admin para ajustar suas permissões/atribuições.`,
        "Sem permissão",
      );

      return Promise.reject(erro);
    }

    // 3) 500+: erro inesperado no servidor
    if (status >= 500) {
      toastError(
        "O servidor retornou um erro inesperado. Tente novamente. Se persistir, verifique os logs da API.",
        "Erro no servidor",
      );

      const data = erro?.response?.data;
      if (data && typeof data === "object" && !data.mensagem) {
        erro.response.data = {
          ...data,
          mensagem: "Erro inesperado no servidor.",
        };
      }

      return Promise.reject(erro);
    }

    return Promise.reject(erro);
  },
);
