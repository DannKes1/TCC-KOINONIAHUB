using System.Security.Cryptography;
using System.Text;

namespace KoinoniaHub.API.Dominio.Termos
{
    // Textos do Termo de Uso e Sigilo (RF42), em código, com todas as versões
    // mantidas (RNF 42.6). A identificação da igreja e os canais de contato NÃO
    // fazem parte do texto: são apresentados dinamicamente na tela a partir do
    // cadastro da igreja, e o hash cobre apenas o texto fixo da versão (RNF 42.7).
    //
    // Para publicar uma nova versão: acrescentar um VersaoTermo à lista com número
    // maior e VigenteDesde posterior. Versões anteriores nunca são alteradas nem
    // removidas, para que cada registro de aceite continue verificável.
    public static class TermosDeUso
    {
        private const string TextoV1_0 = """
            TERMO DE USO E SIGILO DO KOINONIAHUB

            O KoinoniaHub é um sistema utilizado para apoiar a gestão da Escola Bíblica Dominical (EBD), incluindo o cadastro de pessoas, organização de turmas, matrículas, aulas, registro de frequência e consulta de informações necessárias ao acompanhamento das atividades da EBD.

            Ao utilizar o sistema, o usuário poderá ter acesso a dados pessoais de outras pessoas, de acordo com o seu perfil e com as permissões atribuídas pela igreja. Essas informações devem ser utilizadas exclusivamente para as atividades relacionadas à Escola Bíblica Dominical.

            O sistema pode armazenar informações como nome, data de nascimento, dados de contato, endereço, vínculos de parentesco, matrículas, atribuições em turmas e registros relacionados à frequência nas aulas, conforme a necessidade de cada cadastro e das atividades realizadas no sistema. O acesso a essas informações é limitado conforme as permissões de cada usuário. O tratamento dessas informações observa a Lei nº 13.709/2018 (Lei Geral de Proteção de Dados Pessoais — LGPD).

            Ao aceitar este Termo, o usuário se compromete a:

            1. utilizar as informações acessadas no KoinoniaHub somente para as atividades da Escola Bíblica Dominical e para as finalidades autorizadas pela igreja;
            2. acessar somente as informações necessárias ao exercício de suas atividades no sistema;
            3. não copiar, compartilhar, divulgar ou utilizar dados obtidos no sistema para finalidades pessoais ou diferentes daquelas autorizadas;
            4. preservar o sigilo das informações às quais tiver acesso;
            5. não compartilhar suas credenciais de acesso com outras pessoas;
            6. comunicar à administração da igreja caso identifique acesso indevido, exposição de informações ou qualquer situação que possa comprometer a segurança dos dados;
            7. utilizar as funcionalidades do sistema respeitando o perfil de acesso e as atribuições que lhe foram concedidas;
            8. ter cuidado redobrado com informações de crianças e adolescentes, cujo tratamento se dá no melhor interesse do menor, conforme a legislação aplicável.

            O descumprimento das condições deste Termo pode resultar na suspensão ou na inativação do acesso do usuário ao sistema, sem prejuízo das demais medidas cabíveis.

            O usuário reconhece que seu acesso ao KoinoniaHub é pessoal e que as ações realizadas por meio de sua conta devem respeitar as responsabilidades e permissões estabelecidas pela igreja.

            O KoinoniaHub mantém mecanismos de controle de acesso e proteção das informações armazenadas. A igreja é responsável por definir as finalidades de utilização dos dados e as permissões concedidas aos usuários do sistema.

            Os titulares de dados podem, por meio do canal de contato informado pela igreja, solicitar informações sobre o tratamento de seus dados pessoais, bem como sua atualização ou correção.

            O aceite deste Termo é registrado pelo KoinoniaHub com a versão e o resumo criptográfico (hash) do texto aceito, a data e hora, o meio pelo qual o aceite foi realizado e o endereço IP de origem, utilizado exclusivamente para auditoria do aceite.

            Ao marcar a opção de aceite, o usuário declara que leu e está de acordo com as condições de uso e sigilo estabelecidas neste Termo.
            """;

        public static readonly IReadOnlyList<VersaoTermo> Versoes = new List<VersaoTermo>
        {
            new("1.0", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), TextoV1_0)
        };

        // Versão de maior data de vigência que já entrou em vigor.
        public static VersaoTermo Vigente =>
            Versoes
                .Where(v => v.VigenteDesde <= DateTime.UtcNow)
                .OrderByDescending(v => v.VigenteDesde)
                .First();

        public static VersaoTermo? ObterPorVersao(string? versao)
        {
            if (string.IsNullOrWhiteSpace(versao)) return null;
            var alvo = versao.Trim();
            return Versoes.FirstOrDefault(v => v.Versao == alvo);
        }

        public static bool EhVigente(string? versao) =>
            !string.IsNullOrWhiteSpace(versao) && versao.Trim() == Vigente.Versao;

        public static string CalcularHash(string texto)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
