using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class AtribuicaoServico : IAtribuicaoServico
    {
        private readonly IAtribuicaoRepositorio _repositorio;
        private readonly KoinoniaHubDbContext _db;

        public AtribuicaoServico(IAtribuicaoRepositorio repositorio, KoinoniaHubDbContext db)
        {
            _repositorio = repositorio;
            _db = db;
        }

        public async Task<AtribuicaoRespostaDto> CriarAsync(int igrejaId, AtribuicaoCriarRequisicaoDto dto)
        {
            var funcao = dto.Funcao.Trim();

            // Validar Departamento 
            var dep = await _db.Departamentos.AsNoTracking()
                .FirstOrDefaultAsync(d => d.IgrejaId == igrejaId && d.Id == dto.DepartamentoId);

            if (dep is null)
                throw new InvalidOperationException("Departamento não encontrado para esta igreja.");

            // Validar Pessoa
            var pessoa = await _db.Pessoas.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IgrejaId == igrejaId && p.Id == dto.PessoaId);

            if (pessoa is null)
                throw new InvalidOperationException("Pessoa não encontrada para esta igreja.");

            // Impedir duplicidade ativa 
            var existeAtiva = await _db.Atribuicoes.AnyAsync(a =>
                a.Ativo &&
                a.DepartamentoId == dto.DepartamentoId &&
                a.PessoaId == dto.PessoaId &&
                a.Funcao.ToLower() == funcao.ToLower() &&
                a.Departamento.IgrejaId == igrejaId);

            if (existeAtiva)
                throw new InvalidOperationException("Esta pessoa já possui uma atribuição ativa com esta função neste departamento.");

            var atribuicao = new Atribuicao
            {
                PessoaId = dto.PessoaId,
                DepartamentoId = dto.DepartamentoId,
                Funcao = funcao,
                DataInicio = dto.DataInicio?.ToUniversalTime() ?? DateTime.UtcNow,
                Ativo = dto.Ativo,
                DataFim = null
            };

            var criado = await _repositorio.CriarAsync(atribuicao);

           
            var completo = await _repositorio.ObterPorIdAsync(igrejaId, criado.Id);
            return Mapear(completo!);
        }

        public async Task<List<AtribuicaoRespostaDto>> ListarAsync(int igrejaId)
        {
            var lista = await _repositorio.ListarAsync(igrejaId);
            return lista.Select(Mapear).ToList();
        }

        public async Task<List<AtribuicaoRespostaDto>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId, string? funcao = null, bool? ativo = null)
        {
            var lista = await _repositorio.ListarPorDepartamentoAsync(igrejaId, departamentoId, funcao, ativo);
            return lista.Select(Mapear).ToList();
        }

        public async Task<AtribuicaoRespostaDto?> ObterPorIdAsync(int igrejaId, int atribuicaoId)
        {
            var atrib = await _repositorio.ObterPorIdAsync(igrejaId, atribuicaoId);
            return atrib is null ? null : Mapear(atrib);
        }

        public async Task<bool> AtualizarAsync(int igrejaId, int atribuicaoId, AtribuicaoAtualizarRequisicaoDto dto)
        {
            var atrib = await _db.Atribuicoes
                .Include(a => a.Departamento)
                .FirstOrDefaultAsync(a => a.Id == atribuicaoId && a.Departamento.IgrejaId == igrejaId);

            if (atrib is null) return false;

            var funcao = dto.Funcao.Trim();

           
            if (atrib.Ativo && dto.Ativo)
            {
                var duplicada = await _db.Atribuicoes.AnyAsync(a =>
                    a.Id != atribuicaoId &&
                    a.Ativo &&
                    a.DepartamentoId == atrib.DepartamentoId &&
                    a.PessoaId == atrib.PessoaId &&
                    a.Funcao.ToLower() == funcao.ToLower() &&
                    a.Departamento.IgrejaId == igrejaId);

                if (duplicada)
                    throw new InvalidOperationException("Já existe uma atribuição ativa com esta função para esta pessoa neste departamento.");
            }

            atrib.Funcao = funcao;
            atrib.Ativo = dto.Ativo;

            if (!dto.Ativo)
                atrib.DataFim ??= dto.DataFim?.ToUniversalTime() ?? DateTime.UtcNow;
            else
                atrib.DataFim = null;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EncerrarAsync(int igrejaId, int atribuicaoId, DateTime? dataFim = null)
        {
            var atrib = await _db.Atribuicoes
                .Include(a => a.Departamento)
                .FirstOrDefaultAsync(a => a.Id == atribuicaoId && a.Departamento.IgrejaId == igrejaId);

            if (atrib is null) return false;

            atrib.Ativo = false;
            atrib.DataFim = dataFim?.ToUniversalTime() ?? DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        private static AtribuicaoRespostaDto Mapear(Atribuicao a)
        {
            return new AtribuicaoRespostaDto
            {
                Id = a.Id,
                PessoaId = a.PessoaId,
                PessoaNome = a.Pessoa?.Nome ?? string.Empty,
                DepartamentoId = a.DepartamentoId,
                DepartamentoNome = a.Departamento?.Nome ?? string.Empty,
                Funcao = a.Funcao,
                DataInicio = a.DataInicio,
                DataFim = a.DataFim,
                Ativo = a.Ativo
            };
        }
    }
}