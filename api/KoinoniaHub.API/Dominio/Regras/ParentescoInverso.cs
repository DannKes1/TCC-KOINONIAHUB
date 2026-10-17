namespace KoinoniaHub.API.Dominio.Regras
{
    // RF12, RNFs 12.3/12.4 (Plano, seção 6.15): o tipo descreve o parente em relação à
    // pessoa em cujo cadastro o vínculo foi registrado. No cadastro do parente o sistema
    // apresenta o grau inverso, derivado do tipo e do sexo da pessoa de origem, sem gravar
    // um segundo registro.
    public static class ParentescoInverso
    {
        public const string PaiOuMae = "Pai ou mãe";
        public const string Dependente = "Dependente";

        // tipoRegistrado: tipo gravado no cadastro de origem.
        // sexoOrigem: sexo da pessoa em cujo cadastro o vínculo foi registrado.
        // nomeOrigem: nome dessa pessoa (usado só no texto livre).
        public static string Derivar(string tipoRegistrado, string? sexoOrigem, string nomeOrigem)
        {
            var tipo = (tipoRegistrado ?? string.Empty).Trim();

            if (Igual(tipo, "Pai") || Igual(tipo, "Mãe"))
                return "Filho(a)";

            if (Igual(tipo, "Filho(a)"))
            {
                if (Igual(sexoOrigem, "Masculino")) return "Pai";
                if (Igual(sexoOrigem, "Feminino")) return "Mãe";
                return PaiOuMae;
            }

            if (Igual(tipo, "Irmão(ã)") || Igual(tipo, "Cônjuge"))
                return tipo;

            if (Igual(tipo, "Responsável"))
                return Dependente;

            // "Outro" ou texto livre: sem inversão, marcado com a origem.
            return $"{tipo} (informado no cadastro de {nomeOrigem})";
        }

        private static bool Igual(string? a, string b) =>
            string.Equals(a?.Trim(), b, StringComparison.OrdinalIgnoreCase);
    }
}
