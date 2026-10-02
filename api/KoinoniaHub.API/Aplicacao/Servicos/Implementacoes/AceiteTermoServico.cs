using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Termos;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class AceiteTermoServico : IAceiteTermoServico
    {
        public const string MensagemAceiteObrigatorio =
            "É necessário aceitar a versão vigente do Termo de Uso e Sigilo para concluir esta operação.";

        private readonly KoinoniaHubDbContext _db;

        public AceiteTermoServico(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<TermoVigenteRespostaDto> ObterVigenteAsync(int? igrejaId = null, string? tokenConvite = null)
        {
            var vigente = TermosDeUso.Vigente;

            TermoIgrejaRespostaDto? igreja = null;

            if (igrejaId.HasValue)
            {
                igreja = await _db.Igrejas.AsNoTracking()
                    .Where(i => i.Id == igrejaId.Value)
                    .Select(i => new TermoIgrejaRespostaDto { Nome = i.Nome, Email = i.Email, Telefone = i.Telefone })
                    .FirstOrDefaultAsync();
            }
            else if (!string.IsNullOrWhiteSpace(tokenConvite))
            {
                // Tela pública de primeiro acesso: a igreja é a do convite (só o hash vai ao banco).
                var hash = ConviteTokenHelper.CalcularHash(tokenConvite.Trim());
                igreja = await _db.Usuarios.AsNoTracking()
                    .Where(u => u.ConviteTokenHash == hash)
                    .Select(u => new TermoIgrejaRespostaDto { Nome = u.Igreja.Nome, Email = u.Igreja.Email, Telefone = u.Igreja.Telefone })
                    .FirstOrDefaultAsync();
            }

            return new TermoVigenteRespostaDto
            {
                Versao = vigente.Versao,
                VigenteDesde = vigente.VigenteDesde,
                Texto = vigente.Texto,
                Hash = vigente.Hash,
                Igreja = igreja
            };
        }

        public AceiteTermo Montar(int igrejaId, string? versao, string meio, string? ip, Usuario? usuario = null, int usuarioId = 0)
        {
            if (!TermosDeUso.EhVigente(versao))
                throw new InvalidOperationException(MensagemAceiteObrigatorio);

            if (!MeioAceiteTermo.Todos.Contains(meio))
                throw new ArgumentException("Meio de aceite inválido.", nameof(meio));

            var vigente = TermosDeUso.Vigente;
            var agora = DateTime.UtcNow;

            var aceite = new AceiteTermo
            {
                IgrejaId = igrejaId,
                TermoVersao = vigente.Versao,
                TermoHash = vigente.Hash,
                AceitoEm = agora,
                CriadoEm = agora,
                Meio = meio,
                Ip = NormalizarIp(ip)
            };

            if (usuario is not null) aceite.Usuario = usuario;
            else aceite.UsuarioId = usuarioId;

            return aceite;
        }

        public async Task<AceiteTermoRespostaDto> RegistrarAsync(int usuarioId, int igrejaId, string? versao, string? ip)
        {
            if (!TermosDeUso.EhVigente(versao))
                throw new InvalidOperationException(
                    "A versão informada do termo não é a vigente. Recarregue o termo e aceite novamente.");

            var vigente = TermosDeUso.Vigente;

            var existente = await _db.AceitesTermo.AsNoTracking()
                .Where(a => a.UsuarioId == usuarioId && a.IgrejaId == igrejaId && a.TermoVersao == vigente.Versao)
                .OrderByDescending(a => a.AceitoEm)
                .FirstOrDefaultAsync();

            if (existente is not null)
                return Mapear(existente);

            var aceite = Montar(igrejaId, versao, MeioAceiteTermo.Login, ip, usuarioId: usuarioId);
            _db.AceitesTermo.Add(aceite);
            await _db.SaveChangesAsync();

            return Mapear(aceite);
        }

        public async Task<bool> PossuiAceiteVigenteAsync(int usuarioId)
        {
            var versao = TermosDeUso.Vigente.Versao;
            return await _db.AceitesTermo.AsNoTracking()
                .AnyAsync(a => a.UsuarioId == usuarioId && a.TermoVersao == versao);
        }

        public async Task<AceiteTermoRespostaDto?> ObterUltimoAceiteAsync(int usuarioId)
        {
            var ultimo = await _db.AceitesTermo.AsNoTracking()
                .Where(a => a.UsuarioId == usuarioId)
                .OrderByDescending(a => a.AceitoEm)
                .ThenByDescending(a => a.Id)
                .FirstOrDefaultAsync();

            return ultimo is null ? null : Mapear(ultimo);
        }

        public async Task<Dictionary<int, AceiteTermoRespostaDto>> ObterUltimosAceitesAsync(int igrejaId)
        {
            var aceites = await _db.AceitesTermo.AsNoTracking()
                .Where(a => a.IgrejaId == igrejaId)
                .OrderByDescending(a => a.AceitoEm)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            return aceites
                .GroupBy(a => a.UsuarioId)
                .ToDictionary(g => g.Key, g => Mapear(g.First()));
        }

        private static AceiteTermoRespostaDto Mapear(AceiteTermo a) => new()
        {
            Versao = a.TermoVersao,
            AceitoEm = a.AceitoEm,
            Meio = a.Meio,
            Vigente = a.TermoVersao == TermosDeUso.Vigente.Versao
        };

        // RNF 42.8: só auditoria. Coluna de 45 caracteres (IPv6 máximo).
        private static string? NormalizarIp(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return null;
            var texto = ip.Trim();
            return texto.Length <= 45 ? texto : texto[..45];
        }
    }
}
