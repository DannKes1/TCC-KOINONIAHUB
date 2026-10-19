namespace KoinoniaHub.API.Aplicacao.Tempo
{
    // Dia civil da igreja (corretiva da Etapa 7.2, descoberta pelo E2E-04). Os
    // instantes continuam gravados em UTC; o que muda é a leitura de "dia": um período
    // de relatório (RF35–RF38) e o "hoje" da pendência de fechamento (RNF 31.3/35.5)
    // são dias do calendário local, no fuso de Igreja:FusoHorario (padrão
    // America/Porto_Velho, UTC−4). Antes, o dia era o UTC: depois das 20h locais uma
    // matrícula do próprio dia saía do relatório e a aula de hoje virava pendente.
    //
    // O TimeProvider vem da injeção de dependência (TimeProvider.System em produção)
    // para os testes fixarem a hora.
    public sealed class RelogioIgreja
    {
        public const string ChaveConfiguracao = "Igreja:FusoHorario";
        public const string FusoPadrao = "America/Porto_Velho";

        // Mesmo deslocamento (UTC−4, sem horário de verão) no catálogo do Windows, para
        // quando o id IANA não resolver.
        private const string FusoPadraoWindows = "SA Western Standard Time";

        private readonly TimeProvider _tempo;

        public TimeZoneInfo Fuso { get; }

        public RelogioIgreja(IConfiguration configuracao, TimeProvider tempo)
        {
            _tempo = tempo;
            var id = configuracao[ChaveConfiguracao];
            Fuso = ResolverFuso(string.IsNullOrWhiteSpace(id) ? FusoPadrao : id.Trim());
        }

        // Data de hoje no fuso da igreja.
        public DateOnly HojeLocal() => DataLocal(_tempo.GetUtcNow().UtcDateTime);

        // Dia local de um instante gravado (UTC; o SQLite dos testes devolve Unspecified).
        public DateOnly DataLocal(DateTime instante)
        {
            var utc = instante.Kind switch
            {
                DateTimeKind.Utc => instante,
                DateTimeKind.Local => instante.ToUniversalTime(),
                _ => DateTime.SpecifyKind(instante, DateTimeKind.Utc)
            };
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, Fuso));
        }

        // Primeiro instante (UTC) do dia local.
        public DateTime InicioDoDiaUtc(DateOnly data)
        {
            var meiaNoite = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            // Num fuso com horário de verão a meia-noite pode não existir: vale a primeira hora válida.
            while (Fuso.IsInvalidTime(meiaNoite)) meiaNoite = meiaNoite.AddMinutes(30);
            return TimeZoneInfo.ConvertTimeToUtc(meiaNoite, Fuso);
        }

        // Último instante (UTC) do dia local.
        public DateTime FimDoDiaUtc(DateOnly data) => InicioDoDiaUtc(data.AddDays(1)).AddTicks(-1);

        // Data recebida como parâmetro de período: "2026-10-18" (sem fuso) é a própria
        // data civil, assim como meia-noite UTC (formato antigo); outro instante com
        // fuso é convertido para o dia local.
        public DateOnly DataCivil(DateTime valor) =>
            valor.Kind == DateTimeKind.Unspecified ||
            (valor.Kind == DateTimeKind.Utc && valor.TimeOfDay == TimeSpan.Zero)
                ? DateOnly.FromDateTime(valor)
                : DataLocal(valor);

        private static TimeZoneInfo ResolverFuso(string id)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var idWindows))
                {
                    try { return TimeZoneInfo.FindSystemTimeZoneById(idWindows); }
                    catch (Exception ex2) when (ex2 is TimeZoneNotFoundException or InvalidTimeZoneException) { }
                }

                return TimeZoneInfo.FindSystemTimeZoneById(FusoPadraoWindows);
            }
        }
    }
}
