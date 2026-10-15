import type { ConfirmationOptions } from "primevue/confirmationoptions";

export type ToastPayload = {
  severity: "success" | "info" | "warn" | "error";
  summary: string;
  detail?: string;
  life?: number;
};

type ToastAdd = (payload: ToastPayload) => void;

let toastAdd: ToastAdd | null = null;

export function setToastHandler(fn: ToastAdd) {
  toastAdd = fn;
}

export function toastError(detail: string, summary = "Erro") {
  toastAdd?.({ severity: "error", summary, detail, life: 4500 });
}

export function toastWarn(detail: string, summary = "Atenção") {
  toastAdd?.({ severity: "warn", summary, detail, life: 4500 });
}

export function toastInfo(detail: string, summary = "Info") {
  toastAdd?.({ severity: "info", summary, detail, life: 3000 });
}

export function toastSuccess(detail: string, summary = "Sucesso") {
  toastAdd?.({ severity: "success", summary, detail, life: 2500 });
}

// Etapa 6.4-C — confirmações. O ConfirmDialog global (App.vue) aceita, além do texto,
// uma lista de itens (`itens`) mostrada com rolagem própria — usada pelo Consolidar
// para nomear os alunos que serão gravados como ausentes sem estourar a tela do
// celular — e um texto final (`rodape`). As páginas só montam as opções; a
// aparência é uma só, definida no App.vue e no style.css.
export type OpcoesConfirmacao = ConfirmationOptions & {
  itens?: string[];
  rodape?: string;
};

// Confirmação como promessa: true ao aceitar; false ao recusar ou fechar o diálogo
// (X, Esc, clique fora). Usada onde a resposta decide o fluxo (sair da chamada com
// alterações não salvas — 6.4-C, acréscimo b).
export function confirmarAsync(
  confirm: { require: (opcoes: ConfirmationOptions) => void },
  opcoes: OpcoesConfirmacao,
): Promise<boolean> {
  return new Promise((resolve) => {
    let respondido = false;
    const responder = (valor: boolean) => {
      if (respondido) return;
      respondido = true;
      resolve(valor);
    };
    confirm.require({
      ...opcoes,
      accept: () => {
        opcoes.accept?.();
        responder(true);
      },
      reject: () => {
        opcoes.reject?.();
        responder(false);
      },
      onHide: () => {
        opcoes.onHide?.();
        responder(false);
      },
    });
  });
}
