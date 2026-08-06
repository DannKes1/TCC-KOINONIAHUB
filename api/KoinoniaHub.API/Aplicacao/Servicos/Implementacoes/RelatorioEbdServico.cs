using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Servicos.Implementacoes
{
    public class RelatorioEbdServico : IRelatorioEbdServico
    {
        private readonly KoinoniaHubDbContext _db;

        public RelatorioEbdServico(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        private static DateTime ToUtc(DateTime dt) =>
            dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc) 
            };


        public async Task<ResumoDiaRespostaDto> ObterResumoDoDiaAsync(int igrejaId, DateTime data)
        {
            var inicio = DateTime.SpecifyKind(ToUtc(data).Date, DateTimeKind.Utc);
            var fim = inicio.AddDays(1);

            
            var aulasDoDia = await (
                from a in _db.Aulas.AsNoTracking()
                join m in _db.Materias.AsNoTracking() on a.MateriaId equals m.Id
                join d in _db.Departamentos.AsNoTracking() on m.DepartamentoId equals d.Id
                where d.IgrejaId == igrejaId && a.Data >= inicio && a.Data < fim
                select new { a.Id, a.QuantidadeVisitantes, DepartamentoId = d.Id }
            ).ToListAsync();

            var aulaIds = aulasDoDia.Select(a => a.Id).ToList();
            var contagens = await _db.Presencas.AsNoTracking()
                .Where(p => aulaIds.Contains(p.AulaId))
                .GroupBy(p => p.AulaId)
                .Select(g => new
                {
                    AulaId = g.Key,
                    Presentes = g.Count(p => p.Presente),
                    Ausentes = g.Count(p => !p.Presente)
                })
                .ToListAsync();
            var porAula = contagens.ToDictionary(c => c.AulaId);

            var departamentos = await _db.Departamentos.AsNoTracking()
                .Where(d => d.IgrejaId == igrejaId && d.Ativo)
                .OrderBy(d => d.Nome)
                .Select(d => new { d.Id, d.Nome })
                .ToListAsync();

            var resposta = new ResumoDiaRespostaDto { Data = inicio };
            foreach (var dep in departamentos)
            {
                var aulasDep = aulasDoDia.Where(a => a.DepartamentoId == dep.Id).ToList();
                var presentes = aulasDep.Sum(a => porAula.TryGetValue(a.Id, out var c) ? c.Presentes : 0);
                var ausentes = aulasDep.Sum(a => porAula.TryGetValue(a.Id, out var c) ? c.Ausentes : 0);
                var visitantes = aulasDep.Sum(a => a.QuantidadeVisitantes);

                resposta.Turmas.Add(new ResumoDiaTurmaDto
                {
                    DepartamentoId = dep.Id,
                    Nome = dep.Nome,
                    TemChamada = presentes + ausentes > 0,
                    Presentes = presentes,
                    Ausentes = ausentes,
                    Visitantes = visitantes
                });
            }

            resposta.TotalPresentes = resposta.Turmas.Sum(x => x.Presentes);
            resposta.TotalAusentes = resposta.Turmas.Sum(x => x.Ausentes);
            resposta.TotalVisitantes = resposta.Turmas.Sum(x => x.Visitantes);
            return resposta;
        }

        public async Task<FrequenciaTurmaRespostaDto> ObterFrequenciaTurmaAsync(
            int igrejaId, int departamentoId, DateTime dataInicio, DateTime dataFim)
        {
            
            var inicio = ToUtc(dataInicio.Date);
            var fim = ToUtc(dataFim.Date.AddDays(1).AddTicks(-1));

            var departamento = await _db.Departamentos
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IgrejaId == igrejaId && d.Id == departamentoId);

            if (departamento is null)
                throw new InvalidOperationException("Departamento não encontrado para esta igreja.");

            var totalAulas = await _db.Aulas
                .AsNoTracking()
                .CountAsync(a =>
                    a.Materia.DepartamentoId == departamentoId &&
                    a.Materia.Departamento.IgrejaId == igrejaId &&
                    a.Data >= inicio && a.Data <= fim);

            var alunosBase = await _db.AlunosDepartamentos
                .AsNoTracking()
                .Include(m => m.Pessoa)
                .Where(m =>
                    m.DepartamentoId == departamentoId &&
                    m.Departamento.IgrejaId == igrejaId &&
                    m.DataMatricula <= fim &&
                    m.Ativo &&
                    (m.DataSaida == null || m.DataSaida >= inicio))
                .OrderBy(m => m.Pessoa.Nome)
                .Select(m => new
                {
                    MatriculaId = m.Id,
                    m.PessoaId,
                    NomeAluno = m.Pessoa.Nome
                })
                .ToListAsync();

            var totalAlunos = alunosBase.Count;

           
            if (totalAulas == 0 || totalAlunos == 0)
            {
                return new FrequenciaTurmaRespostaDto
                {
                    DepartamentoId = departamento.Id,
                    NomeDepartamento = departamento.Nome,
                    DataInicio = inicio,
                    DataFim = fim,
                    TotalAulas = totalAulas,
                    TotalAlunos = totalAlunos,
                    TotalPresentes = 0,
                    TotalAusentesMarcados = 0,
                    TotalNaoRegistrado = 0,
                    PercentualPresencaGeral = 0,
                    Alunos = new List<FrequenciaAlunoRespostaDto>(),
                    Aulas = new List<FrequenciaAulaRespostaDto>()
                };
            }

            var matriculasIds = alunosBase.Select(x => x.MatriculaId).ToList();

           
            var statsPorAluno = await _db.AlunosDepartamentos
                .AsNoTracking()
                .Where(m => matriculasIds.Contains(m.Id))
                .Select(m => new
                {
                    MatriculaId = m.Id,
                    m.PessoaId,
                    NomeAluno = m.Pessoa.Nome,

                    Presentes = m.Presencas.Count(p =>
                        p.Aula.Materia.DepartamentoId == departamentoId &&
                        p.Aula.Materia.Departamento.IgrejaId == igrejaId &&
                        p.Aula.Data >= inicio && p.Aula.Data <= fim &&
                        p.Presente),

                    AusentesMarcados = m.Presencas.Count(p =>
                        p.Aula.Materia.DepartamentoId == departamentoId &&
                        p.Aula.Materia.Departamento.IgrejaId == igrejaId &&
                        p.Aula.Data >= inicio && p.Aula.Data <= fim &&
                        !p.Presente),

                    Registros = m.Presencas.Count(p =>
                        p.Aula.Materia.DepartamentoId == departamentoId &&
                        p.Aula.Materia.Departamento.IgrejaId == igrejaId &&
                        p.Aula.Data >= inicio && p.Aula.Data <= fim)
                })
                .ToListAsync();

            var alunos = statsPorAluno
                .Select(s =>
                {
                    var naoRegistrado = totalAulas - s.Registros;
                    var presentes = s.Presentes;

                    
                    var percentual = s.Registros == 0
                        ? 0
                        : Math.Round((decimal)presentes / s.Registros * 100m, 2);

                    return new FrequenciaAlunoRespostaDto
                    {
                        MatriculaId = s.MatriculaId,
                        PessoaId = s.PessoaId,
                        NomeAluno = s.NomeAluno,
                        TotalAulas = totalAulas,
                        Presentes = presentes,
                        AusentesMarcados = s.AusentesMarcados,
                        NaoRegistrado = naoRegistrado,
                        PercentualPresenca = percentual
                    };
                })
                .OrderBy(a => a.NomeAluno)
                .ToList();

         
            var aulas = await _db.Aulas
                .AsNoTracking()
                .Where(a =>
                    a.Materia.DepartamentoId == departamentoId &&
                    a.Materia.Departamento.IgrejaId == igrejaId &&
                    a.Data >= inicio && a.Data <= fim)
                .OrderByDescending(a => a.Data)
                .Select(a => new { a.Id, a.Data, a.Tema })
                .ToListAsync();

            var presencasPorAula = await _db.Presencas
                .AsNoTracking()
                .Where(p =>
                    p.Aula.Materia.DepartamentoId == departamentoId &&
                    p.Aula.Materia.Departamento.IgrejaId == igrejaId &&
                    p.Aula.Data >= inicio && p.Aula.Data <= fim &&
                    p.AlunoDepartamento.Ativo)
                .GroupBy(p => p.AulaId)
                .Select(g => new
                {
                    AulaId = g.Key,
                    Presentes = g.Count(x => x.Presente),
                    AusentesMarcados = g.Count(x => !x.Presente),
                    Registros = g.Count()
                })
                .ToListAsync();

            var aulasResposta = aulas.Select(a =>
            {
                var stat = presencasPorAula.FirstOrDefault(x => x.AulaId == a.Id);

                var presentes = stat?.Presentes ?? 0;
                var ausentesMarcados = stat?.AusentesMarcados ?? 0;
                var registros = stat?.Registros ?? 0;

                var naoRegistrado = totalAlunos - registros;

                var percentual = totalAlunos == 0
                    ? 0
                    : Math.Round((decimal)presentes / totalAlunos * 100m, 2);

                return new FrequenciaAulaRespostaDto
                {
                    AulaId = a.Id,
                    Data = ToUtc(a.Data),
                    Tema = a.Tema,
                    TotalAlunos = totalAlunos,
                    Presentes = presentes,
                    AusentesMarcados = ausentesMarcados,
                    NaoRegistrado = naoRegistrado,
                    PercentualPresenca = percentual
                };
            }).ToList();

          
            var totalPresentes = alunos.Sum(x => x.Presentes);
            var totalAusentesMarcados = alunos.Sum(x => x.AusentesMarcados);
            var totalNaoRegistrado = alunos.Sum(x => x.NaoRegistrado);

            var totalRegistros = totalPresentes + totalAusentesMarcados;
            var percentualGeral = totalRegistros == 0
                ? 0
                : Math.Round((decimal)totalPresentes / totalRegistros * 100m, 2);

            return new FrequenciaTurmaRespostaDto
            {
                DepartamentoId = departamento.Id,
                NomeDepartamento = departamento.Nome,
                DataInicio = inicio,
                DataFim = fim,
                TotalAulas = totalAulas,
                TotalAlunos = totalAlunos,
                TotalPresentes = totalPresentes,
                TotalAusentesMarcados = totalAusentesMarcados,
                TotalNaoRegistrado = totalNaoRegistrado,
                PercentualPresencaGeral = percentualGeral,
                Alunos = alunos,
                Aulas = aulasResposta
            };
        }

       
        public async Task<RankingFaltasRespostaDto> ObterRankingFaltasAsync(
            int igrejaId, int departamentoId, DateTime dataInicio, DateTime dataFim, int top)
        {
            if (top <= 0) top = 10;

            var frequencia = await ObterFrequenciaTurmaAsync(igrejaId, departamentoId, dataInicio, dataFim);

            var itens = frequencia.Alunos
                .OrderByDescending(a => a.FaltasTotais)   
                .ThenBy(a => a.PercentualPresenca)        
                .ThenBy(a => a.NomeAluno)                 
                .Take(top)
                .Select(a => new RankingFaltasItemRespostaDto
                {
                    MatriculaId = a.MatriculaId,
                    PessoaId = a.PessoaId,
                    NomeAluno = a.NomeAluno,
                    TotalAulas = a.TotalAulas,
                    Presentes = a.Presentes,
                    FaltasTotais = a.FaltasTotais,
                    PercentualPresenca = a.PercentualPresenca
                })
                .ToList();

            return new RankingFaltasRespostaDto
            {
                DepartamentoId = frequencia.DepartamentoId,
                NomeDepartamento = frequencia.NomeDepartamento,
                DataInicio = frequencia.DataInicio,
                DataFim = frequencia.DataFim,
                Itens = itens
            };
        }

        public async Task<PainelAcompanhamentoRespostaDto> ObterPainelAcompanhamentoAsync(
            int igrejaId,
            int departamentoId,
            DateTime dataInicio,
            DateTime dataFim,
            decimal limiarAtencao,
            decimal limiarCritico,
            int faltasConsecutivasCritico)
        {
            
            limiarAtencao = Math.Clamp(limiarAtencao, 0m, 100m);
            limiarCritico = Math.Clamp(limiarCritico, 0m, 100m);
            if (limiarCritico > limiarAtencao) limiarCritico = limiarAtencao; 
            faltasConsecutivasCritico = Math.Max(1, faltasConsecutivasCritico);

            const int faltasConsecutivasAtencao = 2; // 2 faltas seguidas já entram como atenção

          
            var inicio = ToUtc(dataInicio.Date);
            var fim = ToUtc(dataFim.Date.AddDays(1).AddTicks(-1));

            var departamento = await _db.Departamentos
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IgrejaId == igrejaId && d.Id == departamentoId);

            if (departamento is null)
                throw new InvalidOperationException("Departamento não encontrado para esta igreja.");

            
            var aulas = await _db.Aulas
                .AsNoTracking()
                .Where(a =>
                    a.Materia.DepartamentoId == departamentoId &&
                    a.Materia.Departamento.IgrejaId == igrejaId &&
                    a.Data >= inicio && a.Data <= fim)
                .OrderBy(a => a.Data)
                .Select(a => new { a.Id, a.Data })
                .ToListAsync();

            var totalAulas = aulas.Count;

            
            var alunosBase = await _db.AlunosDepartamentos
                .AsNoTracking()
                .Where(m =>
                    m.DepartamentoId == departamentoId &&
                    m.Departamento.IgrejaId == igrejaId &&
                    m.DataMatricula <= fim &&
                    m.Ativo &&
                    (m.DataSaida == null || m.DataSaida >= inicio))
                .Select(m => new
                {
                    MatriculaId = m.Id,
                    m.PessoaId,
                    NomeAluno = m.Pessoa.Nome
                })
                .ToListAsync();

            var totalAlunos = alunosBase.Count;

            var painel = new PainelAcompanhamentoRespostaDto
            {
                DepartamentoId = departamento.Id,
                NomeDepartamento = departamento.Nome,
                DataInicio = inicio,
                DataFim = fim,
                TotalAulas = totalAulas,
                TotalAlunos = totalAlunos,
                LimiarAtencao = limiarAtencao,
                LimiarCritico = limiarCritico,
                FaltasConsecutivasCritico = faltasConsecutivasCritico,
                Alunos = new List<AlunoEmAtencaoRespostaDto>()
            };

            if (totalAulas == 0 || totalAlunos == 0)
                return painel;

            
            var presencas = await _db.Presencas
                .AsNoTracking()
                .Where(p =>
                    p.Aula.Materia.DepartamentoId == departamentoId &&
                    p.Aula.Materia.Departamento.IgrejaId == igrejaId &&
                    p.Aula.Data >= inicio && p.Aula.Data <= fim &&
                    p.AlunoDepartamento.Ativo)
                .Select(p => new { p.AlunoDepartamentoId, p.AulaId, p.Presente })
                .ToListAsync();

            
            var mapaPresenca = presencas
                .GroupBy(p => (p.AlunoDepartamentoId, p.AulaId))
                .ToDictionary(g => g.Key, g => g.First().Presente);

            var idsAulasCronologico = aulas.Select(a => a.Id).ToList();
            var datasPorAula = aulas.ToDictionary(a => a.Id, a => a.Data);

            foreach (var aluno in alunosBase)
            {
               
                var presentes = 0;
                var registros = 0;
                DateTime? ultimaPresenca = null;

                foreach (var aulaId in idsAulasCronologico)
                {
                    var temRegistro = mapaPresenca.TryGetValue((aluno.MatriculaId, aulaId), out var presente);
                    if (!temRegistro) continue;

                    registros++;
                    if (presente)
                    {
                        presentes++;
                        var data = datasPorAula[aulaId];
                        if (ultimaPresenca == null || data > ultimaPresenca) ultimaPresenca = data;
                    }
                }

               
                if (registros == 0) continue;

                var faltasTotais = registros - presentes; // ausências efetivamente marcadas
                var percentual = Math.Round((decimal)presentes / registros * 100m, 2);

               
                var faltasConsecutivas = 0;
                for (var i = idsAulasCronologico.Count - 1; i >= 0; i--)
                {
                    var aulaId = idsAulasCronologico[i];

                    var temRegistro = mapaPresenca.TryGetValue((aluno.MatriculaId, aulaId), out var presente);
                    if (!temRegistro) continue;
                    if (presente) break;
                    faltasConsecutivas++;
                }

                // ---- Classificação por critérios objetivos ----
                var motivos = new List<string>();
                var critico = false;
                var atencao = false;

                if (faltasConsecutivas >= faltasConsecutivasCritico)
                {
                    critico = true;
                    motivos.Add($"{faltasConsecutivas} faltas consecutivas");
                }
                else if (faltasConsecutivas >= faltasConsecutivasAtencao)
                {
                    atencao = true;
                    motivos.Add($"{faltasConsecutivas} faltas consecutivas");
                }

                if (percentual <= limiarCritico)
                {
                    critico = true;
                    motivos.Add($"Frequência crítica ({percentual:0.##}%)");
                }
                else if (percentual <= limiarAtencao)
                {
                    atencao = true;
                    motivos.Add($"Frequência abaixo do mínimo ({percentual:0.##}%)");
                }

                if (!critico && !atencao)
                    continue; // sem alerta: não entra no painel

                painel.Alunos.Add(new AlunoEmAtencaoRespostaDto
                {
                    MatriculaId = aluno.MatriculaId,
                    PessoaId = aluno.PessoaId,
                    NomeAluno = aluno.NomeAluno,
                    TotalAulas = totalAulas,
                    Presentes = presentes,
                    FaltasTotais = faltasTotais,
                    PercentualPresenca = percentual,
                    FaltasConsecutivas = faltasConsecutivas,
                    DataUltimaPresenca = ultimaPresenca,
                    Classificacao = critico ? "Critico" : "Atencao",
                    Motivos = motivos
                });
            }

          
            painel.Alunos = painel.Alunos
                .OrderByDescending(a => a.Classificacao == "Critico")
                .ThenByDescending(a => a.FaltasConsecutivas)
                .ThenBy(a => a.PercentualPresenca)
                .ThenBy(a => a.NomeAluno)
                .ToList();

            painel.TotalCritico = painel.Alunos.Count(a => a.Classificacao == "Critico");
            painel.TotalAtencao = painel.Alunos.Count(a => a.Classificacao == "Atencao");

            return painel;
        }

        public async Task<MinhaFrequenciaTurmaRespostaDto> ObterMinhaFrequenciaTurmaAsync(
            int igrejaId,
            int usuarioId,
            int departamentoId,
            DateTime dataInicio,
            DateTime dataFim)
        {
            var inicio = ToUtc(dataInicio.Date);
            var fim = ToUtc(dataFim.Date.AddDays(1).AddTicks(-1));

          
            var usuario = await _db.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == usuarioId && u.IgrejaId == igrejaId);

            if (usuario?.PessoaId is null)
                throw new InvalidOperationException("Seu usuário não está vinculado a uma pessoa.");

            var pessoaId = usuario.PessoaId.Value;

            
            var matricula = await _db.AlunosDepartamentos
                .AsNoTracking()
                .Include(m => m.Departamento)
                .FirstOrDefaultAsync(m =>
                    m.PessoaId == pessoaId &&
                    m.DepartamentoId == departamentoId &&
                    m.Departamento.IgrejaId == igrejaId &&
                    m.Ativo);

            if (matricula is null)
                throw new UnauthorizedAccessException("Você não está matriculado nesta turma.");

            var departamento = matricula.Departamento;

           
            var aulas = await _db.Aulas
                .AsNoTracking()
                .Where(a =>
                    a.Materia.DepartamentoId == departamentoId &&
                    a.Materia.Departamento.IgrejaId == igrejaId &&
                    a.Data >= inicio && a.Data <= fim)
                .OrderByDescending(a => a.Data)
                .Select(a => new { a.Id, a.Data, a.Tema })
                .ToListAsync();

            
            var presencas = await _db.Presencas
                .AsNoTracking()
                .Where(p =>
                    p.AlunoDepartamentoId == matricula.Id &&
                    p.Aula.Data >= inicio && p.Aula.Data <= fim)
                .Select(p => new { p.AulaId, p.Presente })
                .ToListAsync();

            var mapaPresenca = presencas
                .GroupBy(p => p.AulaId)
                .ToDictionary(g => g.Key, g => g.First().Presente);

            var itens = new List<MinhaFrequenciaAulaRespostaDto>();
            var presentes = 0;
            var ausentes = 0;

            foreach (var a in aulas)
            {
                string situacao;
                if (mapaPresenca.TryGetValue(a.Id, out var presente))
                {
                    situacao = presente ? "Presente" : "Ausente";
                    if (presente) presentes++;
                    else ausentes++;
                }
                else
                {
                    situacao = "Não registrado";
                }

                itens.Add(new MinhaFrequenciaAulaRespostaDto
                {
                    AulaId = a.Id,
                    Data = a.Data,
                    Tema = a.Tema,
                    Situacao = situacao
                });
            }

            var totalAulas = aulas.Count;
            var naoRegistrado = totalAulas - presentes - ausentes;

          
            var registros = presentes + ausentes;
            var percentual = registros == 0
                ? 0m
                : Math.Round((decimal)presentes / registros * 100m, 2);

            return new MinhaFrequenciaTurmaRespostaDto
            {
                DepartamentoId = departamento.Id,
                NomeDepartamento = departamento.Nome,
                DataInicio = inicio,
                DataFim = fim,
                TotalAulas = totalAulas,
                Presentes = presentes,
                AusentesMarcados = ausentes,
                NaoRegistrado = naoRegistrado,
                PercentualPresenca = percentual,
                Aulas = itens
            };
        }
    }
}