<script setup lang="ts">
import { computed, ref } from "vue";

import InlineMessage from "../../../components/ui/InlineMessage.vue";

import {
  toastSuccess,
  toastWarn,
} from "../../../aplicacao/servicos/notificacoes";

import { importarPessoas } from "../../../aplicacao/servicos/pessoasServico";
import type { ImportacaoPessoasResultadoVM } from "../../../aplicacao/modelos/dtos";

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
    "Nome;Email;Celular;DataNascimento;Sexo;Categoria\n" +
    "Maria da Silva;maria@email.com;69 99999-0000;05/03/1990;Feminino;Membro\n" +
    "João Pereira;;69 98888-0000;20/11/1985;Masculino;Membro\n" +
    "Ana Souza (visitante);ana@email.com;;;;Visitante\n";

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
        texto="Importe de uma só vez o rol de membros da igreja. O arquivo precisa da coluna 'Nome'; as demais (Email, Celular, DataNascimento, Sexo, Categoria...) são opcionais. Linhas repetidas são ignoradas, então é seguro reenviar o mesmo arquivo."
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
        </div>

        <DataTable
          :value="resultado.itens"
          paginator
          :rows="8"
          dataKey="linha"
          responsiveLayout="scroll"
        >
          <Column field="linha" header="Linha" style="width: 80px" sortable />
          <Column field="nome" header="Nome" sortable />
          <Column header="Status" style="width: 120px">
            <template #body="{ data }">
              <Tag :value="data.status" :severity="severityItem(data.status)" />
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
