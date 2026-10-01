using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests.Infraestrutura
{
    // Semente para as regras de aula (RF32/RF33): uma igreja com Admin, um Professor
    // com atribuição ativa na turma, um segundo Professor sem atribuição nela, uma
    // matéria, dois alunos com matrícula ativa (A e B) e um aluno com matrícula
    // inativa, que não pode contar para a chamada.
    // As aulas são criadas por teste, na situação e com as presenças que cada caso pede.
    public sealed class CenarioAula
    {
        public int IgrejaId { get; init; }
        public int TurmaId { get; init; }
        public int MateriaId { get; init; }
        public int ProfessorPessoaId { get; init; }
        public int MatriculaAId { get; init; }
        public int MatriculaBId { get; init; }
        public int MatriculaInativaId { get; init; }
        public string NomeAlunoA { get; init; } = string.Empty;
        public string NomeAlunoB { get; init; } = string.Empty;
        public string NomeAlunoInativo { get; init; } = string.Empty;
        public string EmailAdmin { get; init; } = string.Empty;
        public string EmailProfessor { get; init; } = string.Empty;
        public string EmailProfessorSemAtribuicao { get; init; } = string.Empty;

        public static async Task<CenarioAula> CriarAsync(KoinoniaHubWebApplicationFactory fabrica, string sufixo)
        {
            using var escopo = fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();

            var senhaHash = BCrypt.Net.BCrypt.HashPassword(CenarioAcessoPessoa.Senha);
            var igreja = new Igreja { Nome = $"Igreja {sufixo}" };
            var turma = new Departamento { Nome = $"Turma {sufixo}", Igreja = igreja };
            var materia = new Materia { Nome = $"Materia {sufixo}", Departamento = turma };

            var adminPessoa = new Pessoa { Nome = $"Admin {sufixo}", Igreja = igreja };
            var professorPessoa = new Pessoa { Nome = $"Professor {sufixo}", Igreja = igreja };
            var professorForaPessoa = new Pessoa { Nome = $"Professor Fora {sufixo}", Igreja = igreja };
            var alunoA = new Pessoa { Nome = $"Aluno A {sufixo}", Igreja = igreja };
            var alunoB = new Pessoa { Nome = $"Aluno B {sufixo}", Igreja = igreja };
            var alunoInativo = new Pessoa { Nome = $"Aluno Inativo {sufixo}", Igreja = igreja };

            var emailAdmin = $"admin.{sufixo}@teste.com";
            var emailProfessor = $"professor.{sufixo}@teste.com";
            var emailProfessorFora = $"professor.fora.{sufixo}@teste.com";

            db.AddRange(
                new Usuario { Email = emailAdmin, SenhaHash = senhaHash, Perfil = "Admin", Igreja = igreja, Pessoa = adminPessoa },
                new Usuario { Email = emailProfessor, SenhaHash = senhaHash, Perfil = "Professor", Igreja = igreja, Pessoa = professorPessoa },
                new Usuario { Email = emailProfessorFora, SenhaHash = senhaHash, Perfil = "Professor", Igreja = igreja, Pessoa = professorForaPessoa });

            db.Add(new Atribuicao { Funcao = "Professor", Pessoa = professorPessoa, Departamento = turma });

            // A matéria precisa ser adicionada explicitamente: nada mais a referencia
            // (as aulas são criadas por teste), então o EF não a alcançaria pelo grafo.
            db.Add(materia);

            var matriculaA = new AlunoDepartamento { Pessoa = alunoA, Departamento = turma };
            var matriculaB = new AlunoDepartamento { Pessoa = alunoB, Departamento = turma };
            var matriculaInativa = new AlunoDepartamento
            {
                Pessoa = alunoInativo,
                Departamento = turma,
                Ativo = false,
                DataSaida = DateTime.UtcNow.AddDays(-30)
            };
            db.AddRange(matriculaA, matriculaB, matriculaInativa);

            await db.SaveChangesAsync();

            return new CenarioAula
            {
                IgrejaId = igreja.Id,
                TurmaId = turma.Id,
                MateriaId = materia.Id,
                ProfessorPessoaId = professorPessoa.Id,
                MatriculaAId = matriculaA.Id,
                MatriculaBId = matriculaB.Id,
                MatriculaInativaId = matriculaInativa.Id,
                NomeAlunoA = alunoA.Nome,
                NomeAlunoB = alunoB.Nome,
                NomeAlunoInativo = alunoInativo.Nome,
                EmailAdmin = emailAdmin,
                EmailProfessor = emailProfessor,
                EmailProfessorSemAtribuicao = emailProfessorFora
            };
        }

        // Cria uma aula de sete dias atrás na situação pedida, com um registro de
        // presença por par (matrícula, presente) informado.
        public Task<int> CriarAulaAsync(KoinoniaHubWebApplicationFactory fabrica, string situacao, params (int matriculaId, bool presente)[] presencas) =>
            CriarAulaEmAsync(fabrica, DateTime.UtcNow.AddDays(-7), situacao, visitantes: 0, presencas);

        // Variante para os relatórios (etapa 4): data, situação e visitantes explícitos.
        public async Task<int> CriarAulaEmAsync(
            KoinoniaHubWebApplicationFactory fabrica,
            DateTime data,
            string situacao,
            int visitantes,
            params (int matriculaId, bool presente)[] presencas)
        {
            using var escopo = fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();

            var aula = new Aula
            {
                Data = data,
                MateriaId = MateriaId,
                ProfessorId = ProfessorPessoaId,
                Situacao = situacao,
                QuantidadeVisitantes = visitantes
            };
            db.Add(aula);

            foreach (var (matriculaId, presente) in presencas)
                db.Add(new Presenca { Aula = aula, AlunoDepartamentoId = matriculaId, Presente = presente });

            await db.SaveChangesAsync();
            return aula.Id;
        }

        // Cria um usuário de perfil Usuario ligado à pessoa de uma matrícula (para RF6).
        public static async Task<string> CriarUsuarioDaMatriculaAsync(KoinoniaHubWebApplicationFactory fabrica, int matriculaId, string sufixo)
        {
            using var escopo = fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();

            var matricula = await db.AlunosDepartamentos
                .Include(m => m.Departamento)
                .SingleAsync(m => m.Id == matriculaId);

            var email = $"aluno.{sufixo}@teste.com";
            db.Add(new Usuario
            {
                Email = email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(CenarioAcessoPessoa.Senha),
                Perfil = "Usuario",
                IgrejaId = matricula.Departamento.IgrejaId,
                PessoaId = matricula.PessoaId
            });
            await db.SaveChangesAsync();

            return email;
        }

        public static async Task<Aula> LerAulaAsync(KoinoniaHubWebApplicationFactory fabrica, int aulaId)
        {
            using var escopo = fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.Aulas.AsNoTracking().SingleAsync(a => a.Id == aulaId);
        }

        public static async Task<List<Presenca>> LerPresencasAsync(KoinoniaHubWebApplicationFactory fabrica, int aulaId)
        {
            using var escopo = fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.Presencas.AsNoTracking()
                .Where(p => p.AulaId == aulaId)
                .OrderBy(p => p.AlunoDepartamentoId)
                .ToListAsync();
        }
    }
}