using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class IgrejaServico : IIgrejaServico
    {
        private readonly IIgrejaRepositorio _igrejaRepositorio;

        public IgrejaServico(IIgrejaRepositorio igrejaRepositorio)
        {
            _igrejaRepositorio = igrejaRepositorio;
        }

        public async Task<IgrejaRespostaDto> CriarAsync(IgrejaCriarRequisicaoDto dto)
        {
            var igreja = new Igreja
            {
                Nome = dto.Nome,
                Endereco = dto.Endereco,
                Cidade = dto.Cidade,
                Estado = dto.Estado,
                CEP = dto.CEP,
                Telefone = dto.Telefone,
                Email = dto.Email,
                CNPJ = dto.CNPJ,
                LogoUrl = dto.LogoUrl
            };

            var criada = await _igrejaRepositorio.CriarAsync(igreja);

            return new IgrejaRespostaDto
            {
                Id = criada.Id,
                Nome = criada.Nome,
                Cidade = criada.Cidade,
                Estado = criada.Estado,
                Email = criada.Email,
                CriadoEm = criada.CriadoEm
            };
        }

        public async Task<IgrejaRespostaDto?> ObterPorIdAsync(int id)
        {
            var igreja = await _igrejaRepositorio.ObterPorIdAsync(id);
            if (igreja is null) return null;

            return new IgrejaRespostaDto
            {
                Id = igreja.Id,
                Nome = igreja.Nome,
                Cidade = igreja.Cidade,
                Estado = igreja.Estado,
                Email = igreja.Email,
                CriadoEm = igreja.CriadoEm
            };
        }
    }
}
