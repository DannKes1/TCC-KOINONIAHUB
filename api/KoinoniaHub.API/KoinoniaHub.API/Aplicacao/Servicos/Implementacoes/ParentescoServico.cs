using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class ParentescoServico : IParentescoServico
    {
        private readonly IParentescoRepositorio _repositorio;
        private readonly KoinoniaHubDbContext _db;

        public ParentescoServico(IParentescoRepositorio repositorio, KoinoniaHubDbContext db)
        {
            _repositorio = repositorio;
            _db = db;
        }

        public async Task<ParentescoRespostaDto> AdicionarAsync(int igrejaId, int pessoaId, ParentescoCriarRequisicaoDto dto)
        {
            if (pessoaId == dto.ParenteId)
                throw new InvalidOperationException("Uma pessoa não pode ser parente dela mesma.");

            // Validar Pessoa e Parente na mesma igreja
            var pessoa = await _db.Pessoas.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IgrejaId == igrejaId && p.Id == pessoaId);

            if (pessoa is null)
                throw new InvalidOperationException("Pessoa não encontrada para esta igreja.");

            var parente = await _db.Pessoas.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IgrejaId == igrejaId && p.Id == dto.ParenteId);

            if (parente is null)
                throw new InvalidOperationException("Parente não encontrado para esta igreja.");

            var tipo = dto.TipoRelacionamento.Trim();

            
            var existe = await _db.Parentescos.AnyAsync(p =>
                p.PessoaId == pessoaId &&
                p.ParenteId == dto.ParenteId &&
                p.TipoRelacionamento.ToLower() == tipo.ToLower() &&
                p.Pessoa.IgrejaId == igrejaId);

            if (existe)
                throw new InvalidOperationException("Este parentesco já existe para esta pessoa.");

            var parentesco = new Parentesco
            {
                PessoaId = pessoaId,
                ParenteId = dto.ParenteId,
                TipoRelacionamento = tipo
            };

            var criado = await _repositorio.CriarAsync(parentesco);

            return new ParentescoRespostaDto
            {
                Id = criado.Id,
                PessoaId = pessoaId,
                ParenteId = dto.ParenteId,
                TipoRelacionamento = tipo,
                ParenteNome = parente.Nome,
                ParenteTelefone = parente.Telefone,
                ParenteCelular = parente.Celular
            };
        }

        public async Task<List<ParentescoRespostaDto>> ListarAsync(int igrejaId, int pessoaId)
        {
            var lista = await _repositorio.ListarDaPessoaAsync(igrejaId, pessoaId);

            return lista.Select(p => new ParentescoRespostaDto
            {
                Id = p.Id,
                PessoaId = p.PessoaId,
                ParenteId = p.ParenteId,
                TipoRelacionamento = p.TipoRelacionamento,
                ParenteNome = p.Parente?.Nome ?? string.Empty,
                ParenteTelefone = p.Parente?.Telefone,
                ParenteCelular = p.Parente?.Celular
            }).ToList();
        }

        public async Task<bool> RemoverAsync(int igrejaId, int pessoaId, int parentescoId)
        {
            var parentesco = await _repositorio.ObterPorIdAsync(igrejaId, pessoaId, parentescoId);
            if (parentesco is null) return false;

            await _repositorio.RemoverAsync(parentesco);
            return true;
        }
    }
}