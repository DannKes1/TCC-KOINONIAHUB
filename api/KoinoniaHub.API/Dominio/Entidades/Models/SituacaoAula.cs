namespace KoinoniaHub.API.Dominio.Entidades
{
    public static class SituacaoAula
    {
        public const string EmAberto = "EmAberto";
        public const string Consolidada = "Consolidada";
        public const string NaoRealizada = "NaoRealizada";

        public static readonly string[] Todas = { EmAberto, Consolidada, NaoRealizada };
    }
}
