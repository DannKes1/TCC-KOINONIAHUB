using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Termos;

namespace KoinoniaHub.API.Tests.Infraestrutura
{
    // Desde a etapa 5.2 toda rota autenticada exige aceite da versão vigente do
    // Termo (ExigeAceiteTermoFiltro). As sementes criam o aceite junto com cada
    // usuário; os testes que precisam de uma conta pendente pedem `aceitarTermo: false`.
    public static class SementeTermo
    {
        public static AceiteTermo AceiteDe(Usuario usuario)
        {
            var vigente = TermosDeUso.Vigente;
            var agora = DateTime.UtcNow;

            return new AceiteTermo
            {
                Usuario = usuario,
                Igreja = usuario.Igreja,
                TermoVersao = vigente.Versao,
                TermoHash = vigente.Hash,
                AceitoEm = agora,
                CriadoEm = agora,
                Meio = MeioAceiteTermo.Login
            };
        }
    }
}
