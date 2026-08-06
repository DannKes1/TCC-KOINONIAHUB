using System.Threading.Tasks;

namespace KoinoniaHub.API.Aplicacao.Seguranca
{
    public interface IAutorizacaoEbdServico
    {
        Task GarantirAcessoDepartamentoAsync(int igrejaId, int usuarioId, string perfil, int departamentoId);
        Task GarantirAcessoMateriaAsync(int igrejaId, int usuarioId, string perfil, int materiaId);
        Task GarantirAcessoAulaAsync(int igrejaId, int usuarioId, string perfil, int aulaId);
    }
}