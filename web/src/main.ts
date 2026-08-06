import { createApp } from "vue";
import { createPinia } from "pinia";
import App from "./App.vue";

import "./style.css";

import router from "./aplicacao/rotas";
import { usarAutenticacaoStore } from "./aplicacao/armazenamentos/autenticacaoStore";

// PrimeVue
import PrimeVue from "primevue/config";
import Aura from "@primeuix/themes/aura";
import "primeicons/primeicons.css";

// PrimeVue services
import ToastService from "primevue/toastservice";
import ConfirmationService from "primevue/confirmationservice";
import Tooltip from "primevue/tooltip";

const app = createApp(App);

const pinia = createPinia();
app.use(pinia);

// carrega sessão persistida 1x (antes das rotas começarem a rodar)
usarAutenticacaoStore(pinia).carregarDoStorage();

app.use(router);

app.use(PrimeVue, {
  theme: {
    preset: Aura,
    options: {
      cssLayer: false,
      darkModeSelector: ".force-dark-never",
    },
  },
  pt: {
    global: {
      css: `
        :root {
          --p-primary-50:  #EDF5F0;
          --p-primary-100: #D4E8DB;
          --p-primary-200: #A3CFAE;
          --p-primary-300: #6DB37E;
          --p-primary-400: #3D8A55;
          --p-primary-500: #234F32;
          --p-primary-600: #1E4429;
          --p-primary-700: #1A3B25;
          --p-primary-800: #152E1D;
          --p-primary-900: #0F2115;
          --p-primary-color: #234F32;
          --p-primary-contrast-color: #FFFFFF;
          --p-text-color: #4D4D4D;
          --p-text-muted-color: #7A7A7A;
          --p-surface-border: #E2E2E2;
        }
      `,
    },
  },
  locale: {
    firstDayOfWeek: 0,
    dayNames: [
      "domingo",
      "segunda-feira",
      "terça-feira",
      "quarta-feira",
      "quinta-feira",
      "sexta-feira",
      "sábado",
    ],
    dayNamesShort: ["dom", "seg", "ter", "qua", "qui", "sex", "sáb"],
    dayNamesMin: ["D", "S", "T", "Q", "Q", "S", "S"],
    monthNames: [
      "janeiro",
      "fevereiro",
      "março",
      "abril",
      "maio",
      "junho",
      "julho",
      "agosto",
      "setembro",
      "outubro",
      "novembro",
      "dezembro",
    ],
    monthNamesShort: [
      "jan",
      "fev",
      "mar",
      "abr",
      "mai",
      "jun",
      "jul",
      "ago",
      "set",
      "out",
      "nov",
      "dez",
    ],
    today: "Hoje",
    clear: "Limpar",
    dateFormat: "dd/mm/yy",
    weekHeader: "Sem",
    chooseYear: "Escolher ano",
    chooseMonth: "Escolher mês",
    chooseDate: "Escolher data",
    prevDecade: "Década anterior",
    nextDecade: "Próxima década",
    prevYear: "Ano anterior",
    nextYear: "Próximo ano",
    prevMonth: "Mês anterior",
    nextMonth: "Próximo mês",
    prevHour: "Hora anterior",
    nextHour: "Próxima hora",
    prevMinute: "Minuto anterior",
    nextMinute: "Próximo minuto",
    am: "AM",
    pm: "PM",
  },
});

app.use(ToastService);
app.use(ConfirmationService);
app.directive("tooltip", Tooltip);

app.mount("#app");
