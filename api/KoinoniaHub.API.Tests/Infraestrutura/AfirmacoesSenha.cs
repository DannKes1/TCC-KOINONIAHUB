namespace KoinoniaHub.API.Tests.Infraestrutura
{
    // Senha só como hash BCrypt, nunca em texto puro (RNFs 1.2, 2.2, 13.2, 15.4, 40.3), com
    // fator de custo igual ou superior a 10 (monografia, seção 4.8, recomendação da OWASP).
    public static class AfirmacoesSenha
    {
        public static void SoHashBCrypt(string senhaHash, string senhaEmClaro)
        {
            // Formato BCrypt: $2a$ (ou $2b$/$2y$), custo com dois dígitos, 53 caracteres de sal e hash.
            Assert.Matches(@"^\$2[aby]\$\d{2}\$.{53}$", senhaHash);

            var custo = int.Parse(senhaHash.Split('$')[2]);
            Assert.True(custo >= 10, $"Fator de custo do BCrypt {custo}, abaixo de 10 (seção 4.8).");

            Assert.DoesNotContain(senhaEmClaro, senhaHash);
            Assert.True(BCrypt.Net.BCrypt.Verify(senhaEmClaro, senhaHash));
        }
    }
}
