namespace KoinoniaHub.API.Dominio.Termos
{
    // Uma versão do Termo de Uso e Sigilo (RNF 42.6): número, data de vigência e o
    // texto fixo sobre o qual o hash SHA-256 é calculado (RNF 42.2 / 42.7).
    public sealed class VersaoTermo
    {
        public VersaoTermo(string versao, DateTime vigenteDesde, string texto)
        {
            Versao = versao;
            VigenteDesde = DateTime.SpecifyKind(vigenteDesde, DateTimeKind.Utc);

            // Quebras de linha normalizadas para "\n": o hash não pode depender de o
            // arquivo-fonte ter sido salvo com CRLF (Windows) ou LF.
            Texto = texto.Replace("\r\n", "\n").Trim();
            Hash = TermosDeUso.CalcularHash(Texto);
        }

        public string Versao { get; }
        public DateTime VigenteDesde { get; }
        public string Texto { get; }

        // SHA-256 do Texto, em hexadecimal minúsculo (64 caracteres).
        public string Hash { get; }
    }
}
