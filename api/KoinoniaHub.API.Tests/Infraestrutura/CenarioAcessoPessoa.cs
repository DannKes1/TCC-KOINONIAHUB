using System.Net;
using System.Net.Http.Json;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests.Infraestrutura
{
    public sealed class CenarioAcessoPessoa
    {
        public const string Senha = "Senha@123";

        public int IgrejaId { get; init; }
        public int TurmaComAtribuicaoId { get; init; }
        public int TurmaSemAtribuicaoId { get; init; }
        public int AlunoDentroId { get; init; }
        public int AlunoForaId { get; init; }
        public int ProfessorPessoaId { get; init; }
        public int UsuarioComumPessoaId { get; init; }
        public string EmailAdmin { get; init; } = string.Empty;
        public string EmailProfessor { get; init; } = string.Empty;
        public string EmailUsuarioComum { get; init; } = string.Empty;

        // aceitarTermo = false deixa as três contas sem aceite do termo (pendentes), para os
        // testes do RF42; o padrão cria o aceite, pois o filtro global bloqueia o restante.
        public static async Task<CenarioAcessoPessoa> CriarAsync(KoinoniaHubWebApplicationFactory fabrica, string sufixo, bool aceitarTermo = true)
        {
            using var escopo = fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();

            var senhaHash = BCrypt.Net.BCrypt.HashPassword(Senha);

            var igreja = new Igreja { Nome = $"Igreja {sufixo}" };

            var turmaA = new Departamento { Nome = $"Turma A {sufixo}", Igreja = igreja };
            var turmaB = new Departamento { Nome = $"Turma B {sufixo}", Igreja = igreja };

            var adminPessoa = new Pessoa { Nome = $"Admin {sufixo}", Igreja = igreja };
            var professorPessoa = new Pessoa { Nome = $"Professor {sufixo}", Igreja = igreja };
            var usuarioComumPessoa = new Pessoa { Nome = $"Usuario Comum {sufixo}", Igreja = igreja };
            var alunoDentro = new Pessoa
            {
                Nome = $"Aluno Dentro {sufixo}",
                Igreja = igreja,
                Celular = "69999990001",
                Email = $"aluno.dentro.{sufixo}@teste.com",
                Endereco = "Rua Reservada, 1",
                DataNascimento = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            var alunoFora = new Pessoa { Nome = $"Aluno Fora {sufixo}", Igreja = igreja, Celular = "69999990002" };
            var responsavel = new Pessoa { Nome = $"Responsavel {sufixo}", Igreja = igreja, Celular = "69999990003" };

            var emailAdmin = $"admin.{sufixo}@teste.com";
            var emailProfessor = $"professor.{sufixo}@teste.com";
            var emailUsuarioComum = $"usuario.{sufixo}@teste.com";

            var usuarios = new[]
            {
                new Usuario { Email = emailAdmin, SenhaHash = senhaHash, Perfil = "Admin", Igreja = igreja, Pessoa = adminPessoa },
                new Usuario { Email = emailProfessor, SenhaHash = senhaHash, Perfil = "Professor", Igreja = igreja, Pessoa = professorPessoa },
                new Usuario { Email = emailUsuarioComum, SenhaHash = senhaHash, Perfil = "Usuario", Igreja = igreja, Pessoa = usuarioComumPessoa }
            };
            db.AddRange(usuarios);

            if (aceitarTermo)
                foreach (var u in usuarios) db.Add(SementeTermo.AceiteDe(u));

            db.Add(new Atribuicao { Funcao = "Professor", Pessoa = professorPessoa, Departamento = turmaA });

            var matriculaDentroA = new AlunoDepartamento { Pessoa = alunoDentro, Departamento = turmaA };
            var matriculaDentroB = new AlunoDepartamento { Pessoa = alunoDentro, Departamento = turmaB };
            var matriculaForaB = new AlunoDepartamento { Pessoa = alunoFora, Departamento = turmaB };
            db.AddRange(matriculaDentroA, matriculaDentroB, matriculaForaB);

            db.Add(new Parentesco { Pessoa = alunoDentro, Parente = responsavel, TipoRelacionamento = "Mãe" });

            var materiaA = new Materia { Nome = $"Materia A {sufixo}", Departamento = turmaA };
            var materiaB = new Materia { Nome = $"Materia B {sufixo}", Departamento = turmaB };
            var aulaA = new Aula { Data = DateTime.UtcNow.AddDays(-7), Materia = materiaA, Professor = professorPessoa };
            var aulaB = new Aula { Data = DateTime.UtcNow.AddDays(-7), Materia = materiaB, Professor = professorPessoa };

            db.AddRange(
                new Presenca { Aula = aulaA, AlunoDepartamento = matriculaDentroA, Presente = true },
                new Presenca { Aula = aulaB, AlunoDepartamento = matriculaDentroB, Presente = false });

            await db.SaveChangesAsync();

            return new CenarioAcessoPessoa
            {
                IgrejaId = igreja.Id,
                TurmaComAtribuicaoId = turmaA.Id,
                TurmaSemAtribuicaoId = turmaB.Id,
                AlunoDentroId = alunoDentro.Id,
                AlunoForaId = alunoFora.Id,
                ProfessorPessoaId = professorPessoa.Id,
                UsuarioComumPessoaId = usuarioComumPessoa.Id,
                EmailAdmin = emailAdmin,
                EmailProfessor = emailProfessor,
                EmailUsuarioComum = emailUsuarioComum
            };
        }

        public static async Task<HttpClient> ClienteAutenticadoAsync(KoinoniaHubWebApplicationFactory fabrica, string email)
        {
            var cliente = fabrica.CreateClient();
            var login = await cliente.PostAsJsonAsync("/api/auth/login", new { Email = email, Senha = Senha });

            if (login.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException($"Login de teste falhou para {email}: {(int)login.StatusCode}.");

            return cliente;
        }
    }
}
