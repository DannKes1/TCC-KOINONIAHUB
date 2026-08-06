<script setup lang="ts">
// Card de indicador (KPI) padronizado dos relatórios.
// Centraliza o visual que antes era montado com estilos inline em cada tela.
defineProps<{
  rotulo: string;
  valor: string | number;
  icone?: string;
  tom?: "padrao" | "sucesso" | "alerta" | "perigo";
}>();
</script>

<template>
  <div class="card-indicador" :class="`tom-${tom ?? 'padrao'}`">
    <div v-if="icone" class="card-indicador-icone">
      <i :class="icone"></i>
    </div>
    <div class="card-indicador-textos">
      <span class="card-indicador-rotulo">{{ rotulo }}</span>
      <span class="card-indicador-valor">{{ valor }}</span>
    </div>
  </div>
</template>

<style scoped>
.card-indicador {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 180px;
  padding: 14px 16px;
  background: var(--ipb-branco, #fff);
  border: 1px solid var(--ipb-cinza-borda, #e2e2e2);
  border-radius: 12px;
}

.card-indicador-icone {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border-radius: 10px;
  font-size: 18px;
  flex-shrink: 0;
}

.card-indicador-textos {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.card-indicador-rotulo {
  font-size: 13px;
  color: var(--ipb-cinza-claro, #7a7a7a);
}

.card-indicador-valor {
  font-size: 22px;
  font-weight: 600;
  line-height: 1.1;
  color: var(--ipb-cinza, #4d4d4d);
}

/* Tons do ícone e do valor */
.tom-padrao .card-indicador-icone {
  background: var(--ipb-verde-bg, #edf5f0);
  color: var(--ipb-verde, #234f32);
}

.tom-sucesso .card-indicador-icone {
  background: #e7f3ec;
  color: var(--ipb-sucesso, #2e7d4a);
}

.tom-alerta .card-indicador-icone {
  background: #fdf3e3;
  color: #b45309;
}
.tom-alerta .card-indicador-valor {
  color: #b45309;
}

.tom-perigo .card-indicador-icone {
  background: #f9e9e9;
  color: var(--ipb-erro, #b83232);
}
.tom-perigo .card-indicador-valor {
  color: var(--ipb-erro, #b83232);
}
</style>
