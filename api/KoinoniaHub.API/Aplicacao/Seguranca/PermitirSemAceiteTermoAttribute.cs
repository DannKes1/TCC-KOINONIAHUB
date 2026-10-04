namespace KoinoniaHub.API.Aplicacao.Seguranca
{
    // Marca os endpoints que uma conta autenticada pode usar ANTES de aceitar a
    // versão vigente do Termo de Uso e Sigilo: o próprio termo (consultar e
    // aceitar) e o encerramento da sessão. Tudo o mais é bloqueado por
    // ExigeAceiteTermoFiltro (RNF 2.5 / 42.1).
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class PermitirSemAceiteTermoAttribute : Attribute
    {
    }
}
