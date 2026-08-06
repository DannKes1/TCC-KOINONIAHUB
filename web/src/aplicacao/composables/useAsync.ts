import { ref } from "vue";
import { parseApiError } from "../servicos/apiError";

export function useAsync() {
  const carregando = ref(false);
  const erro = ref("");
  const fieldErrors = ref<Record<string, string[]>>({});

  function clearErrors() {
    erro.value = "";
    fieldErrors.value = {};
  }

  async function run<T>(
    acao: () => Promise<T>,
    msgErroPadrao: string,
    opts?: { throwOnError?: boolean },
  ): Promise<T | undefined> {
    clearErrors();
    carregando.value = true;

    try {
      return await acao();
    } catch (e: any) {
      const parsed = parseApiError(e, msgErroPadrao);
      erro.value = parsed.message;
      fieldErrors.value = parsed.fieldErrors;

      if (opts?.throwOnError) throw e;
      return undefined;
    } finally {
      carregando.value = false;
    }
  }

  return { carregando, erro, fieldErrors, run, clearErrors };
}
