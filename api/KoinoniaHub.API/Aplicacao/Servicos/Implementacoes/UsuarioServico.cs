using BCrypt.Net;
using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class UsuarioServico : IUsuarioServico
    {
        private const int DiasValidadeConvite = 7;

        private readonly IUsuarioRepositorio _repositorio;
        private readonly KoinoniaHubDbContext _db;

        private static readonly string[] PerfisValidos = new[]
        {
            "Admin", "Pastor", "Superintendente", "Professor", "Usuario"
        };

        public UsuarioServico(IUsuarioRepositorio repositorio, KoinoniaHubDbContext db)
        {
            _repositorio = repositorio;
            _db = db;
        }

        public async Task<UsuarioRespostaDto> CriarParaPessoaAsync(int igrejaId, UsuarioCriarRequisicaoDto dto)
        {
            //  Pessoa precisa existir e ser da igreja
            var pessoa = await _db.Pessoas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IgrejaId == igrejaId && p.Id == dto.PessoaId);

            if (pessoa is null)
                throw new InvalidOperationException("Pessoa não encontrada para esta igreja.");

            //  Não permitir 2 usuários vinculados à mesma Pessoa (regra prática)
            var jaTemUsuarioParaPessoa = await _db.Usuarios
                .AnyAsync(u => u.IgrejaId == igrejaId && u.PessoaId == dto.PessoaId);

            if (jaTemUsuarioParaPessoa)
                throw new InvalidOperationException("Esta pessoa já possui um usuário vinculado.");

            // Determinar e-mail
            var emailFinal = (dto.Email ?? pessoa.Email)?.Trim();
            if (string.IsNullOrWhiteSpace(emailFinal))
                throw new InvalidOperationException("Informe um e-mail no usuário ou cadastre e-mail na Pessoa.");

            var emailNormalizado = emailFinal.ToLowerInvariant();

            // E-mail não pode repetir 
            var existe = await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == emailNormalizado);
            if (existe)
                throw new InvalidOperationException("Já existe um usuário com este e-mail.");

            //  Validar perfil
            var perfil = (dto.Perfil ?? "Usuario").Trim();
            if (!PerfisValidos.Contains(perfil))
                throw new InvalidOperationException("Perfil inválido. Use: Admin, Pastor, Superintendente, Professor ou Usuario.");

            // Modo de definição da senha:
            // - Senha informada -> fluxo original (o admin define a senha inicial).
            // - Senha em branco -> convite de primeiro acesso: o sistema gera um
            //   token de uso único e a própria pessoa define a senha pelo link.
            var senhaInformada = !string.IsNullOrWhiteSpace(dto.Senha);

            string? tokenConvite = null;
            DateTime? conviteExpiraEm = null;
            string senhaHash;

            if (senhaInformada)
            {
                senhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha!.Trim());
            }
            else
            {
                tokenConvite = ConviteTokenHelper.GerarToken();
                conviteExpiraEm = DateTime.UtcNow.AddDays(DiasValidadeConvite);

                // Senha aleatória impossível de adivinhar: a conta só se torna
                // utilizável quando a pessoa define a senha pelo convite.
                senhaHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N") + tokenConvite);
            }

            // Criar usuário
            var usuario = new Usuario
            {
                IgrejaId = igrejaId,
                PessoaId = dto.PessoaId,
                Email = emailNormalizado,
                SenhaHash = senhaHash,
                Perfil = perfil,
                Ativo = true,
                ConviteTokenHash = tokenConvite is null ? null : ConviteTokenHelper.CalcularHash(tokenConvite),
                ConviteExpiraEm = conviteExpiraEm
            };

            var criado = await _repositorio.CriarAsync(usuario);

            var resposta = MapearParaResposta(criado, pessoa.Nome);
            resposta.ConviteToken = tokenConvite;
            resposta.ConviteExpiraEm = conviteExpiraEm;

            return resposta;
        }

        // Gera (ou regenera) um convite de primeiro acesso para uma conta já
        // existente. Útil quando o link expirou, foi perdido, ou quando o admin
        // prefere que a própria pessoa defina uma nova senha em vez de digitá-la.
        // Observação: a senha atual do usuário continua válida até que o convite
        // seja utilizado.
        public async Task<ConviteRespostaDto?> GerarConviteAsync(int igrejaId, int usuarioId)
        {
            var usuario = await _repositorio.ObterPorIdAsync(igrejaId, usuarioId);
            if (usuario is null) return null;

            if (!usuario.Ativo)
                throw new InvalidOperationException("Não é possível gerar convite para um usuário inativo.");

            var token = ConviteTokenHelper.GerarToken();
            var expiraEm = DateTime.UtcNow.AddDays(DiasValidadeConvite);

            usuario.ConviteTokenHash = ConviteTokenHelper.CalcularHash(token);
            usuario.ConviteExpiraEm = expiraEm;

            await _repositorio.AtualizarAsync(usuario);

            return new ConviteRespostaDto
            {
                UsuarioId = usuario.Id,
                Email = usuario.Email,
                NomePessoa = usuario.Pessoa?.Nome,
                Token = token,
                ExpiraEm = expiraEm
            };
        }

        public async Task<List<UsuarioRespostaDto>> ListarAsync(int igrejaId)
        {
            var usuarios = await _repositorio.ListarAsync(igrejaId);

            return usuarios
                .Select(u => MapearParaResposta(u, u.Pessoa?.Nome))
                .ToList();
        }

        public async Task<UsuarioRespostaDto?> ObterPorIdAsync(int igrejaId, int usuarioId)
        {
            var u = await _repositorio.ObterPorIdAsync(igrejaId, usuarioId);
            if (u is null) return null;

            return MapearParaResposta(u, u.Pessoa?.Nome);
        }

        public async Task<bool> AtualizarAsync(int igrejaId, int usuarioId, int usuarioLogadoId, UsuarioAtualizarRequisicaoDto dto)
        {
            var usuario = await _repositorio.ObterPorIdAsync(igrejaId, usuarioId);
            if (usuario is null) return false;


            if (dto.Ativo.HasValue && dto.Ativo.Value == false && usuarioId == usuarioLogadoId)
                throw new InvalidOperationException("Você não pode desativar o seu próprio usuário.");

            if (!string.IsNullOrWhiteSpace(dto.Perfil))
            {
                var perfil = dto.Perfil.Trim();
                if (!PerfisValidos.Contains(perfil))
                    throw new InvalidOperationException("Perfil inválido. Use: Admin, Pastor, Superintendente, Professor ou Usuario.");

                usuario.Perfil = perfil;
            }

            if (dto.Ativo.HasValue)
                usuario.Ativo = dto.Ativo.Value;

            await _repositorio.AtualizarAsync(usuario);
            return true;
        }

        public async Task<bool> ResetarSenhaAsync(int igrejaId, int usuarioId, UsuarioResetarSenhaRequisicaoDto dto)
        {
            var usuario = await _repositorio.ObterPorIdAsync(igrejaId, usuarioId);
            if (usuario is null) return false;

            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);

            // Um reset manual do admin invalida qualquer convite pendente.
            usuario.ConviteTokenHash = null;
            usuario.ConviteExpiraEm = null;

            await _repositorio.AtualizarAsync(usuario);

            return true;
        }

        private static UsuarioRespostaDto MapearParaResposta(Usuario u, string? nomePessoa)
        {
            return new UsuarioRespostaDto
            {
                Id = u.Id,
                IgrejaId = u.IgrejaId,
                Email = u.Email,
                Perfil = u.Perfil,
                Ativo = u.Ativo,
                PessoaId = u.PessoaId,
                NomePessoa = nomePessoa,
                ConvitePendente = u.ConviteTokenHash != null
            };
        }
    }
}
