using BCrypt.Net;
using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Termos;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class AuthServico : IAuthServico
    {
        private readonly KoinoniaHubDbContext _db;
        private readonly IIgrejaServico _igrejaServico;
        private readonly ITokenServico _tokenServico;
        private readonly IAceiteTermoServico _aceiteTermoServico;

        public AuthServico(
            KoinoniaHubDbContext db,
            IIgrejaServico igrejaServico,
            ITokenServico tokenServico,
            IAceiteTermoServico aceiteTermoServico)
        {
            _db = db;
            _igrejaServico = igrejaServico;
            _tokenServico = tokenServico;
            _aceiteTermoServico = aceiteTermoServico;
        }

        public async Task<AuthRespostaDto> RegistrarAdminAsync(RegistrarAdminRequisicaoDto dto, string? ipOrigem)
        {
            // RNF 1.5 / 42.1: sem o aceite da versão vigente, nada é criado.
            if (!TermosDeUso.EhVigente(dto.AceiteTermoVersao))
                throw new InvalidOperationException(AceiteTermoServico.MensagemAceiteObrigatorio);

            // Verifica se já existe usuário com esse email
            var email = dto.EmailAdmin.Trim().ToLowerInvariant();
            var existe = await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email);
            if (existe)
                throw new InvalidOperationException("Já existe um usuário com este e-mail.");

            // Igreja, pessoa, usuário e aceite do termo na mesma transação (Plano 6.3):
            // ou o cadastro inicial nasce completo, ou nada é gravado.
            await using var transacao = await _db.Database.BeginTransactionAsync();

            //  Cria igreja
            var igrejaCriada = await _igrejaServico.CriarAsync(dto.Igreja);

            //  Cria pessoa admin
            var pessoaAdmin = new Pessoa
            {
                Nome = dto.NomeAdmin,
                Email = email,
                IgrejaId = igrejaCriada.Id,
                Situacao = "Ativo"
            };
            _db.Pessoas.Add(pessoaAdmin);
            await _db.SaveChangesAsync();

            //  Cria usuário admin
            var usuario = new Usuario
            {
                Email = email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.SenhaAdmin),
                Perfil = "Admin",
                Ativo = true,
                IgrejaId = igrejaCriada.Id,
                PessoaId = pessoaAdmin.Id
            };

            _db.Usuarios.Add(usuario);

            // RNF 42.5: Meio = CadastroInicial.
            _db.AceitesTermo.Add(_aceiteTermoServico.Montar(
                igrejaCriada.Id, dto.AceiteTermoVersao, MeioAceiteTermo.CadastroInicial, ipOrigem, usuario: usuario));

            await _db.SaveChangesAsync();
            await transacao.CommitAsync();

            // Gera token
            var (token, expiraEm) = _tokenServico.GerarToken(usuario);

            return new AuthRespostaDto
            {
                Token = token,
                ExpiraEm = expiraEm,
                IgrejaId = igrejaCriada.Id,
                NomeIgreja = igrejaCriada.Nome,
                UsuarioId = usuario.Id,
                EmailUsuario = usuario.Email,
                Perfil = usuario.Perfil,
                TermoPendente = false
            };
        }

        public async Task<LoginRespostaDto> LoginAsync(LoginRequisicaoDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (usuario is null || !usuario.Ativo)
                throw new InvalidOperationException("Usuário ou senha inválidos.");

            var senhaOk = BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash);
            if (!senhaOk)
                throw new InvalidOperationException("Usuário ou senha inválidos.");

            var (token, expiraEm) = _tokenServico.GerarToken(usuario);

            // RNF 2.5 / 13.4: contas sem aceite da versão vigente fazem o aceite logo após o login.
            var termoPendente = !await _aceiteTermoServico.PossuiAceiteVigenteAsync(usuario.Id);

            return new LoginRespostaDto
            {
                Token = token,
                ExpiraEm = expiraEm,
                UsuarioId = usuario.Id,
                EmailUsuario = usuario.Email,
                Perfil = usuario.Perfil,
                IgrejaId = usuario.IgrejaId,
                PessoaId = usuario.PessoaId,
                TermoPendente = termoPendente
            };
        }

        // Valida um convite de primeiro acesso antes de exibir a tela pública
        // de definição de senha. Retorna null quando o token não existe
        // (ou já foi utilizado, pois o hash é apagado após o uso).
        public async Task<PrimeiroAcessoValidarRespostaDto?> ValidarConviteAsync(string token)
        {
            var usuario = await BuscarPorTokenAsync(token);
            if (usuario is null) return null;

            GarantirConviteUtilizavel(usuario);

            return new PrimeiroAcessoValidarRespostaDto
            {
                Email = usuario.Email,
                NomePessoa = usuario.Pessoa?.Nome
            };
        }

        // Consome o convite: a própria pessoa define a senha, o hash BCrypt é
        // gravado, o aceite do termo é registrado e o token é invalidado (uso único).
        public async Task<PrimeiroAcessoValidarRespostaDto> AtivarPrimeiroAcessoAsync(PrimeiroAcessoAtivarRequisicaoDto dto, string? ipOrigem)
        {
            var usuario = await BuscarPorTokenAsync(dto.Token);

            if (usuario is null)
                throw new InvalidOperationException("Convite inválido ou já utilizado. Solicite um novo link ao administrador.");

            GarantirConviteUtilizavel(usuario);

            // RNF 40.5 / 42.1: sem o aceite da versão vigente, o convite não é consumido.
            // Montar valida a versão antes de qualquer alteração; Meio = PrimeiroAcesso (42.5).
            var aceite = _aceiteTermoServico.Montar(
                usuario.IgrejaId, dto.AceiteTermoVersao, MeioAceiteTermo.PrimeiroAcesso, ipOrigem, usuarioId: usuario.Id);

            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha.Trim());
            usuario.ConviteTokenHash = null;
            usuario.ConviteExpiraEm = null;

            _db.AceitesTermo.Add(aceite);

            // Senha, descarte do convite e aceite no mesmo SaveChanges (mesma transação).
            await _db.SaveChangesAsync();

            return new PrimeiroAcessoValidarRespostaDto
            {
                Email = usuario.Email,
                NomePessoa = usuario.Pessoa?.Nome
            };
        }

        private async Task<Usuario?> BuscarPorTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var hash = ConviteTokenHelper.CalcularHash(token.Trim());

            return await _db.Usuarios
                .Include(u => u.Pessoa)
                .FirstOrDefaultAsync(u => u.ConviteTokenHash == hash);
        }

        private static void GarantirConviteUtilizavel(Usuario usuario)
        {
            if (!usuario.Ativo)
                throw new InvalidOperationException("Este usuário está inativo. Procure o administrador.");

            if (usuario.ConviteExpiraEm is null || usuario.ConviteExpiraEm.Value < DateTime.UtcNow)
                throw new InvalidOperationException("Este convite expirou. Solicite um novo link ao administrador.");
        }
    }
}
