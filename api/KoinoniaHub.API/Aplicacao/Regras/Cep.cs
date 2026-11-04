namespace KoinoniaHub.API.Aplicacao.Regras
{
    // Corretiva G4b: o CEP tem 8 dígitos, com ou sem hífen na entrada, e é gravado sempre
    // como 00000-000. Vazio continua aceito onde o campo é opcional (o
    // RegularExpressionAttribute considera válidos nulo e texto vazio).
    public static class Cep
    {
        public const string Padrao = @"^[0-9]{5}-?[0-9]{3}$";

        public const string Mensagem = "Informe o CEP no formato 00000-000.";

        // Oito dígitos viram 00000-000; vazio vira nulo; qualquer outro valor volta como veio
        // (só a importação CSV chega aqui sem a validação do DTO).
        public static string? Normalizar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var digitos = new string(valor.Where(char.IsAsciiDigit).ToArray());
            return digitos.Length == 8
                ? $"{digitos[..5]}-{digitos[5..]}"
                : valor.Trim();
        }
    }
}
