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
            return Mapear(criada);
        }

        public async Task<IgrejaRespostaDto?> ObterPorIdAsync(int id)
        {
            var igreja = await _igrejaRepositorio.ObterPorIdAsync(id);
            return igreja is null ? null : Mapear(igreja);
        }

        // RF44 (RNFs 44.2, 44.3, 44.5): só os cinco campos do requisito mudam; o restante da
        // entidade (Endereco, CEP, CNPJ, LogoUrl) e os registros de aceite não são tocados.
        // O cabeçalho do termo lê Nome/Email/Telefone da igreja a cada exibição, então a
        // alteração reflete na próxima tela sem mexer nos aceites gravados (hash só do texto).
        public async Task<IgrejaRespostaDto?> AtualizarAsync(int id, IgrejaAtualizarRequisicaoDto dto)
        {
            var igreja = await _igrejaRepositorio.ObterPorIdAsync(id);
            if (igreja is null) return null;

            igreja.Nome = dto.Nome.Trim();
            igreja.Cidade = TextoOuNulo(dto.Cidade);
            igreja.Estado = TextoOuNulo(dto.Estado)?.ToUpperInvariant();
            igreja.Email = TextoOuNulo(dto.Email);
            igreja.Telefone = TextoOuNulo(dto.Telefone);

            await _igrejaRepositorio.AtualizarAsync(igreja);
            return Mapear(igreja);
        }

        private static string? TextoOuNulo(string? valor)
        {
            var texto = valor?.Trim();
            return string.IsNullOrEmpty(texto) ? null : texto;
        }

        private static IgrejaRespostaDto Mapear(Igreja igreja)
        {
            return new IgrejaRespostaDto
            {
                Id = igreja.Id,
                Nome = igreja.Nome,
                Cidade = igreja.Cidade,
                Estado = igreja.Estado,
                Email = igreja.Email,
                Telefone = igreja.Telefone,
                CriadoEm = igreja.CriadoEm,
                AtualizadoEm = igreja.AtualizadoEm
            };
        }
    }
}
