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
