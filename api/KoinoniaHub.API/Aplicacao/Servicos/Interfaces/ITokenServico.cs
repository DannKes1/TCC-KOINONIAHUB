using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface ITokenServico
    {
        (string token, DateTime expiraEm) GerarToken(Usuario usuario);
    }
}
