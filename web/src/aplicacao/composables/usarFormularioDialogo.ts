import { computed, reactive, watch, type Ref } from "vue";

import { useAsync } from "./useAsync";
import { firstFieldError, mensagemForaDosCampos } from "../servicos/apiError";

// Corretiva G5: o padrão de erro dos diálogos e formulários (G3/G4) num lugar só, no lugar do
// bloco que se repetia em cada página:
// - instância própria de useAsync para o salvar (`salvando`, `executar`), separada da
//   instância da página (lista, confirmações);
// - validação local no próprio campo (`errosLocais`, chave = nome do campo no DTO);
// - `erroDoCampo`: o erro local tem prioridade; depois o da API, pelo nome Pascal ou camel;
// - `aviso(campos)`: a mensagem geral da API para o topo do diálogo, vazia quando o erro já
//   está junto a um campo exibido (`mensagemForaDosCampos`);
// - `limparAoFechar` e `limparAoAlterar`: os erros somem ao fechar o diálogo (todos os
//   diálogos que compartilham a instância fechados) ou ao alterar qualquer campo.
export function usarFormularioDialogo() {
  const {
    carregando: salvando,
    erro,
    fieldErrors,
    run: executar,
    clearErrors,
  } = useAsync();

  const errosLocais = reactive<Record<string, string>>({});

  function limpar() {
    clearErrors();
    for (const chave of Object.keys(errosLocais)) delete errosLocais[chave];
  }

  function erroDoCampo(campo: string, camel = campo.charAt(0).toLowerCase() + campo.slice(1)) {
    return (
      errosLocais[campo] ||
      firstFieldError(fieldErrors.value, campo) ||
      firstFieldError(fieldErrors.value, camel)
    );
  }

  function aviso(camposExibidos: string[]) {
    return computed(() => mensagemForaDosCampos(erro.value, fieldErrors.value, camposExibidos));
  }

  function semErrosLocais() {
    return Object.keys(errosLocais).length === 0;
  }

  function limparAoFechar(...visiveis: Ref<boolean>[]) {
    watch(visiveis, (abertos) => {
      if (abertos.every((aberto) => !aberto)) limpar();
    });
  }

  function limparAoAlterar(...formularios: object[]) {
    for (const formulario of formularios) watch(formulario, limpar, { deep: true });
  }

  return {
    salvando,
    erro,
    fieldErrors,
    executar,
    errosLocais,
    limpar,
    erroDoCampo,
    aviso,
    semErrosLocais,
    limparAoFechar,
    limparAoAlterar,
  };
}
