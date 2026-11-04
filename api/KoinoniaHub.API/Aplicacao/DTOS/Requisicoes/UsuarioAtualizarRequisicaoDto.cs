using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class UsuarioAtualizarRequisicaoDto
    {
        private string? _email;

        public string? Perfil { get; set; }
        public bool? Ativo { get; set; }

        // Corretiva G2 (RF14): e-mail de acesso. Ausente, vazio ou igual ao atual = sem
        // alteração (vazio vira nulo antes do [EmailAddress], como em Meus Dados).
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(100, ErrorMessage = "O campo E-mail deve ter no máximo 100 caracteres.")]
        public string? Email
        {
            get => _email;
            set => _email = string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
