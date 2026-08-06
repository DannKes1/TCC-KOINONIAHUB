using BCrypt.Net;
using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class UsuarioServico : IUsuarioServico
    {
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

            // Criar usuário
            var usuario = new Usuario
            {
                IgrejaId = igrejaId,
                PessoaId = dto.PessoaId,
                Email = emailNormalizado,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                Perfil = perfil,
                Ativo = true
            };

            var criado = await _repositorio.CriarAsync(usuario);

            return new UsuarioRespostaDto
            {
                Id = criado.Id,
                IgrejaId = criado.IgrejaId,
                Email = criado.Email,
                Perfil = criado.Perfil,
                Ativo = criado.Ativo,
                PessoaId = criado.PessoaId,
                NomePessoa = pessoa.Nome
            };
        }

        public async Task<List<UsuarioRespostaDto>> ListarAsync(int igrejaId)
        {
            var usuarios = await _repositorio.ListarAsync(igrejaId);

            return usuarios.Select(u => new UsuarioRespostaDto
            {
                Id = u.Id,
                IgrejaId = u.IgrejaId,
                Email = u.Email,
                Perfil = u.Perfil,
                Ativo = u.Ativo,
                PessoaId = u.PessoaId,
                NomePessoa = u.Pessoa?.Nome
            }).ToList();
        }

        public async Task<UsuarioRespostaDto?> ObterPorIdAsync(int igrejaId, int usuarioId)
        {
            var u = await _repositorio.ObterPorIdAsync(igrejaId, usuarioId);
            if (u is null) return null;

            return new UsuarioRespostaDto
            {
                Id = u.Id,
                IgrejaId = u.IgrejaId,
                Email = u.Email,
                Perfil = u.Perfil,
                Ativo = u.Ativo,
                PessoaId = u.PessoaId,
                NomePessoa = u.Pessoa?.Nome
            };
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
            await _repositorio.AtualizarAsync(usuario);

            return true;
        }
    }
}