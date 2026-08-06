using BCrypt.Net;
using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class AuthServico : IAuthServico
    {
        private readonly KoinoniaHubDbContext _db;
        private readonly IIgrejaServico _igrejaServico;
        private readonly ITokenServico _tokenServico;

        public AuthServico(KoinoniaHubDbContext db, IIgrejaServico igrejaServico, ITokenServico tokenServico)
        {
            _db = db;
            _igrejaServico = igrejaServico;
            _tokenServico = tokenServico;
        }

        public async Task<AuthRespostaDto> RegistrarAdminAsync(RegistrarAdminRequisicaoDto dto)
        {
            // Verifica se já existe usuário com esse email
            var email = dto.EmailAdmin.Trim().ToLowerInvariant();
            var existe = await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email);
            if (existe)
                throw new InvalidOperationException("Já existe um usuário com este e-mail.");

            //  Cria igreja
            var igrejaCriada = await _igrejaServico.CriarAsync(dto.Igreja);

            //  Cria pessoa admin 
            var pessoaAdmin = new Pessoa
            {
                Nome = dto.NomeAdmin,
                Email = email,
                IgrejaId = igrejaCriada.Id,
                Situacao = "Ativo",
                Categoria = "Membro"
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
            await _db.SaveChangesAsync();

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
                Perfil = usuario.Perfil
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

            return new LoginRespostaDto
            {
                Token = token,
                ExpiraEm = expiraEm,
                UsuarioId = usuario.Id,
                EmailUsuario = usuario.Email,
                Perfil = usuario.Perfil,
                IgrejaId = usuario.IgrejaId,
                PessoaId = usuario.PessoaId
            };
        }
    }
}