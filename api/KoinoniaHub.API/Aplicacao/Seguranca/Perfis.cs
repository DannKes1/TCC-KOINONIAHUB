namespace KoinoniaHub.API.Aplicacao.Seguranca
{
    public static class Perfis
    {
        public const string Admin = "Admin";
        public const string Pastor = "Pastor";
        public const string Superintendente = "Superintendente";
        public const string Professor = "Professor";
        public const string Usuario = "Usuario";

        public static readonly string[] Administrativos = { Admin, Pastor, Superintendente };

        public static bool EhAdministrativo(string? perfil) =>
            Administrativos.Contains(perfil ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        public static bool EhUsuarioComum(string? perfil) =>
            string.Equals(perfil, Usuario, StringComparison.OrdinalIgnoreCase);
    }
}
