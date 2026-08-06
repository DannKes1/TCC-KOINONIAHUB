import { defineStore } from "pinia";
import { clienteHttp } from "../servicos/clienteHttp";


type EstadoAutenticacao = {
  sessaoAtiva: boolean;
  expiraEm: string;
  usuarioId: number | null;
  emailUsuario: string;
  perfil: string;
  igrejaId: number | null;
  pessoaId: number | null;
};

const CHAVE_STORAGE = "koinoniahub_sessao";

function parseData(valor: string): Date | null {
  if (!valor) return null;
  const d = new Date(valor);
  return Number.isNaN(d.getTime()) ? null : d;
}

export const usarAutenticacaoStore = defineStore("autenticacao", {
  state: (): EstadoAutenticacao => ({
    sessaoAtiva: false,
    expiraEm: "",
    usuarioId: null,
    emailUsuario: "",
    perfil: "",
    igrejaId: null,
    pessoaId: null as number | null,
  }),

  getters: {
    tokenExpirado(): boolean {
      const d = parseData(this.expiraEm);
      return d ? d.getTime() <= Date.now() : false;
    },
    autenticado(): boolean {
      return this.sessaoAtiva && !this.tokenExpirado;
    },
    isAdmin(): boolean {
      return (
        String(this.perfil || "")
          .trim()
          .toLowerCase() === "admin"
      );
    },
    isPastor(): boolean {
      return (
        String(this.perfil || "")
          .trim()
          .toLowerCase() === "pastor"
      );
    },
    isAdminOuPastor(): boolean {
      return this.isAdmin || this.isPastor;
    },
    isGestor(): boolean {
      const p = String(this.perfil || "")
        .trim()
        .toLowerCase();
      return ["admin", "pastor", "superintendente", "professor"].includes(p);
    },
    isAdministrativo(): boolean {
      const p = String(this.perfil || "")
        .trim()
        .toLowerCase();
      return ["admin", "pastor", "superintendente"].includes(p);
    },
    isUsuarioComum(): boolean {
      const p = String(this.perfil || "")
        .trim()
        .toLowerCase();
      return p === "usuario" || p === "";
    },
  },

  actions: {
    carregarDoStorage() {
      const bruto = localStorage.getItem(CHAVE_STORAGE);
      if (!bruto) return;

      const dados = JSON.parse(bruto) as EstadoAutenticacao;
      this.$patch(dados);

      if (this.tokenExpirado) {
        this.sair();
      }
    },

    salvarNoStorage() {
      localStorage.setItem(CHAVE_STORAGE, JSON.stringify(this.$state));
    },

    entrar(dados: any) {
      
      this.sessaoAtiva = true;
      this.expiraEm = String(dados?.ExpiraEm ?? dados?.expiraEm ?? "");
      this.usuarioId =
        Number(dados?.UsuarioId ?? dados?.usuarioId ?? 0) || null;
      this.emailUsuario = String(
        dados?.EmailUsuario ?? dados?.emailUsuario ?? "",
      );
      this.perfil = String(dados?.Perfil ?? dados?.perfil ?? "");
      this.igrejaId = Number(dados?.IgrejaId ?? dados?.igrejaId ?? 0) || null;
      this.pessoaId = Number(dados?.PessoaId ?? dados?.pessoaId ?? 0) || null;
      this.salvarNoStorage();
    },
    
    async sair() {
      this.$reset();
      localStorage.removeItem(CHAVE_STORAGE);

      try {
        await clienteHttp.post("/api/auth/logout");
      } catch {
      }
    },
  },
});
