<script setup lang="ts">
import { computed, ref } from "vue";

import InlineMessage from "../../../components/ui/InlineMessage.vue";

import {
  toastSuccess,
  toastWarn,
} from "../../../aplicacao/servicos/notificacoes";

import { importarPessoas } from "../../../aplicacao/servicos/pessoasServico";
import type {
  ImportacaoPessoasItemVM,
  ImportacaoPessoasResultadoVM,
} from "../../../aplicacao/modelos/dtos";

import Dialog from "primevue/dialog";
import Button from "primevue/button";
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Tag from "primevue/tag";

const props = defineProps<{ visible: boolean }>();
const emit = defineEmits<{
  (e: "update:visible", valor: boolean): void;
  (e: "importado"): void;
}>();

const arquivo = ref<File | null>(null);
const enviando = ref(false);
const erro = ref("");
const resultado = ref<ImportacaoPessoasResultadoVM | null>(null);

const visivel = computed({
  get: () => props.visible,
  set: (valor: boolean) => emit("update:visible", valor),
});

function aoSelecionarArquivo(evento: Event) {
  const alvo = evento.target as HTMLInputElement;
  arquivo.value = alvo.files?.[0] ?? null;
  resultado.value = null;
  erro.value = "";
}

function limparEFechar() {
  arquivo.value = null;
  resultado.value = null;
  erro.value = "";
  visivel.value = false;
}

// Gera um CSV modelo no navegador (com BOM para o Excel abrir os acentos).
function baixarModelo() {
  const conteudo =
    "\uFEFF" +
    "Nome;Sexo;DataNascimento;EstadoCivil;Email;Celular;Endereco;Bairro;Cidade;Estado;CEP\n" +
    "Maria da Silva;Feminino;05/03/1990;Casado(a);maria@email.com;69 99999-0000;Rua das Flores, 100;Centro;Ji-Paraná;RO;76900-000\n" +
    "João Pereira;Masculino;20/11/1985;Solteiro(a);;69 98888-0000;;;;;\n" +
    "Ana Souza;;;;ana@email.com;;;;;;\n";

  const blob = new Blob([conteudo], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);

  const link = document.createElement("a");
  link.href = url;
  link.download = "modelo-importacao-pessoas.csv";
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

async function enviar() {
  if (!arquivo.value) {
    toastWarn("Selecione um arquivo CSV para importar.");
    return;
  }

  enviando.value = true;
  erro.value = "";

  try {
    const dados = await importarPessoas(arquivo.value);
    resultado.value = dados;

    if (dados.criados > 0) {
      toastSuccess(
        `${dados.criados} pessoa(s) importada(s) com sucesso.`,
        "Importação concluída",
      );
      emit("importado");
    } else {
      toastWarn("Nenhuma pessoa nova foi criada. Confira o relatório abaixo.");
    }

    // RNF 41.3: linhas ignoradas por nome repetido sem e-mail pedem conferência.
    if (dados.paraConferencia > 0) {
      toastWarn(
        `${dados.paraConferencia} linha(s) ignorada(s) por nome já cadastrado, sem e-mail para confirmar. Confira na lista: se for outra pessoa, cadastre manualmente.`,
        "Linhas para conferência",
      );
    }
  } catch (e: any) {
    erro.value =
      e?.response?.data?.mensagem ??
      "Não foi possível importar o arquivo. Confira o formato e tente novamente.";
  } finally {
    enviando.value = false;
  }
}

function severityItem(status: string) {
  if (status === "Criado") return "success";
  if (status === "Ignorado") return "warning";
  return "danger";
}

// Linhas sinalizadas (41.3) ficam destacadas na tabela e ordenadas primeiro.
function classeLinha(item: ImportacaoPessoasItemVM) {
  return item.paraConferencia ? "linha-conferencia" : "";
}

const itensOrdenados = computed(() => {
  const itens = resultado.value?.itens ?? [];
  return [...itens].sort((a, b) => {
    if (a.paraConferencia !== b.paraConferencia) return a.paraConferencia ? -1 : 1;
    return a.linha - b.linha;
  });
});
</script>

<template>
  <Dialog
    v-model:visible="visivel"
    modal
    header="Importar pessoas (CSV)"
    :closable="!enviando"
    :dismissableMask="!enviando"
    style="width: 760px; max-width: 96vw"
  >
    <div class="page-container">
      <InlineMessage
        texto="Importe de uma só vez o rol de membros da igreja. O arquivo precisa da coluna 'Nome'; as demais colunas do modelo (Sexo, DataNascimento, EstadoCivil, Email, Celular, Endereco, Bairro, Cidade, Estado, CEP) são opcionais e colunas fora do modelo são ignoradas. Linhas com e-mail já cadastrado são ignoradas; linhas sem e-mail cujo nome já existe são ignoradas e sinalizadas para conferência."
        tipo="info"
      />

      <div
        style="display: flex; gap: 10px; align-items: center; flex-wrap: wrap"
      >
        <input
          type="file"
          accept=".csv,.txt"
          :disabled="enviando"
          @change="aoSelecionarArquivo"
        />
        <Button
          label="Baixar modelo CSV"
          icon="pi pi-download"
          severity="secondary"
          size="small"
          @click="baixarModelo"
        />
      </div>

      <InlineMessage :texto="erro" tipo="erro" />

      <div
        v-if="resultado"
        style="display: flex; flex-direction: column; gap: 10px"
      >
        <div style="display: flex; gap: 8px; flex-wrap: wrap">
          <Tag
            severity="info"
            :value="`Linhas lidas: ${resultado.totalLinhas}`"
          />
          <Tag severity="success" :value="`Criadas: ${resultado.criados}`" />
          <Tag
            severity="warning"
            :value="`Ignoradas: ${resultado.ignorados}`"
          />
          <Tag severity="danger" :value="`Erros: ${resultado.erros}`" />
          <Tag
            v-if="resultado.paraConferencia > 0"
            severity="warning"
            icon="pi pi-exclamation-triangle"
            :value="`Para conferência: ${resultado.paraConferencia}`"
            data-testid="importacao-para-conferencia"
          />
        </div>

        <InlineMessage
          v-if="resultado.paraConferencia > 0"
          texto="Linhas destacadas: já existe uma pessoa com o mesmo nome e a linha não trazia e-mail para confirmar. Confira se é a mesma pessoa; se for outra, cadastre manualmente."
          tipo="aviso"
        />

        <DataTable
          :value="itensOrdenados"
          paginator
          :rows="8"
          dataKey="linha"
          responsiveLayout="scroll"
          :rowClass="classeLinha"
        >
          <Column field="linha" header="Linha" style="width: 80px" sortable />
          <Column field="nome" header="Nome" sortable />
          <Column header="Status" style="width: 170px">
            <template #body="{ data }">
              <span style="display: inline-flex; gap: 6px; flex-wrap: wrap">
                <Tag :value="data.status" :severity="severityItem(data.status)" />
                <Tag
                  v-if="data.paraConferencia"
                  value="Conferir"
                  severity="warning"
                  icon="pi pi-exclamation-triangle"
                  v-tooltip.top="
                    'Nome já cadastrado e linha sem e-mail: confira se é a mesma pessoa.'
                  "
                />
              </span>
            </template>
          </Column>
          <Column field="mensagem" header="Observação" />
        </DataTable>
      </div>
    </div>

    <template #footer>
      <Button
        label="Fechar"
        severity="secondary"
        :disabled="enviando"
        @click="limparEFechar"
      />
      <Button
        label="Importar"
        icon="pi pi-upload"
        :loading="enviando"
        :disabled="!arquivo"
        @click="enviar"
      />
    </template>
  </Dialog>
</template>

<style scoped>
:deep(tr.linha-conferencia > td) {
  background: #fff4e5;
}
</style>
