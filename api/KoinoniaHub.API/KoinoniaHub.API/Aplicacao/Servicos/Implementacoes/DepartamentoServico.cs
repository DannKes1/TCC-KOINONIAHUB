using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class DepartamentoServico : IDepartamentoServico
    {
        private readonly IDepartamentoRepositorio _repositorio;
        private readonly KoinoniaHubDbContext _db;

        public DepartamentoServico(IDepartamentoRepositorio repositorio, KoinoniaHubDbContext db)
        {
            _repositorio = repositorio;
            _db = db;
        }

        public async Task<DepartamentoRespostaDto> CriarAsync(int igrejaId, DepartamentoCriarRequisicaoDto dto)
        {
            
            var nome = dto.Nome.Trim();
            var tipo = string.IsNullOrWhiteSpace(dto.Tipo) ? "EBD" : dto.Tipo.Trim();

            var existe = await _db.Departamentos.AnyAsync(d =>
                d.IgrejaId == igrejaId &&
                d.Tipo == tipo &&
                d.Nome.ToLower() == nome.ToLower());

            if (existe)
                throw new InvalidOperationException("Já existe um departamento com este nome e tipo nesta igreja.");

            var departamento = new Departamento
            {
                IgrejaId = igrejaId,
                Nome = nome,
                Tipo = tipo,
                Descricao = dto.Descricao,
                ImagemUrl = dto.ImagemUrl,
                Ativo = dto.Ativo
            };

            var criado = await _repositorio.CriarAsync(departamento);
            return Mapear(criado);
        }

        public async Task<List<DepartamentoRespostaDto>> ListarAsync(int igrejaId, int usuarioId, string perfil)
        {
            
            var perfisAdministrativos = new[] { "Admin", "Pastor", "Superintendente" };
            if (perfisAdministrativos.Contains(perfil, StringComparer.OrdinalIgnoreCase))
            {
                var todos = await _repositorio.ListarAsync(igrejaId);
                return todos.Select(Mapear).ToList();
            }

            
            var usuario = await _db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == usuarioId && u.IgrejaId == igrejaId);

            if (usuario?.PessoaId is null)
                return new List<DepartamentoRespostaDto>();

            var pessoaId = usuario.PessoaId.Value;
            var funcoesPermitidas = new[] { "Professor", "Auxiliar" };

            var departamentoIds = await _db.Atribuicoes.AsNoTracking()
                .Where(a =>
                    a.Ativo &&
                    a.PessoaId == pessoaId &&
                    funcoesPermitidas.Contains(a.Funcao))
                .Select(a => a.DepartamentoId)
                .Distinct()
                .ToListAsync();

            if (departamentoIds.Count == 0)
                return new List<DepartamentoRespostaDto>();

            var departamentos = await _db.Departamentos.AsNoTracking()
                .Where(d => d.IgrejaId == igrejaId && departamentoIds.Contains(d.Id))
                .ToListAsync();

            return departamentos.Select(Mapear).ToList();
        }

        public async Task<DepartamentoRespostaDto?> ObterPorIdAsync(int igrejaId, int departamentoId)
        {
            var dep = await _repositorio.ObterPorIdAsync(igrejaId, departamentoId);
            return dep is null ? null : Mapear(dep);
        }

        public async Task<bool> AtualizarAsync(int igrejaId, int departamentoId, DepartamentoAtualizarRequisicaoDto dto)
        {
            var dep = await _db.Departamentos.FirstOrDefaultAsync(d => d.IgrejaId == igrejaId && d.Id == departamentoId);
            if (dep is null) return false;

            var nome = dto.Nome.Trim();
            var tipo = string.IsNullOrWhiteSpace(dto.Tipo) ? dep.Tipo : dto.Tipo.Trim();

            var existe = await _db.Departamentos.AnyAsync(d =>
                d.IgrejaId == igrejaId &&
                d.Id != departamentoId &&
                d.Tipo == tipo &&
                d.Nome.ToLower() == nome.ToLower());

            if (existe)
                throw new InvalidOperationException("Já existe um departamento com este nome e tipo nesta igreja.");

            dep.Nome = nome;
            dep.Tipo = tipo;
            dep.Descricao = dto.Descricao;
            dep.ImagemUrl = dto.ImagemUrl;
            dep.Ativo = dto.Ativo;

            await _repositorio.AtualizarAsync(dep);
            return true;
        }

        private static DepartamentoRespostaDto Mapear(Departamento d)
        {
            return new DepartamentoRespostaDto
            {
                Id = d.Id,
                Nome = d.Nome,
                Tipo = d.Tipo,
                Ativo = d.Ativo,
                CriadoEm = d.CriadoEm,
                AtualizadoEm = d.AtualizadoEm
            };
        }
    }
}