using KoinoniaHub.API.Aplicacao.Excecoes;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Implementacoes;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Aplicacao.Tempo;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Infraestrutura.Repositorios;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers + JSON. O filtro global exige o aceite da versão vigente do Termo de
// Uso e Sigilo em toda rota autenticada não marcada com [PermitirSemAceiteTermo]
// (RNF 2.5 / 42.1; decisão registrada na etapa 5.1). O segundo traduz
// RecursoNaoEncontradoException (turma, matéria ou aula inexistente para a igreja,
// lançada pelos guardas de autorização) em 404 { mensagem } para qualquer perfil (7.1).
builder.Services.AddControllers(opcoes =>
{
    opcoes.Filters.Add<ExigeAceiteTermoFiltro>();
    opcoes.Filters.Add<RecursoNaoEncontradoFiltro>();
})
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        x.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT
// Swashbuckle 10 depende do Microsoft.OpenApi 2.x: o esquema não carrega mais
// "Reference" e o requisito de segurança passou a receber um delegado que
// referencia o esquema pelo nome (OpenApiSecuritySchemeReference).
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KoinoniaHub.API", Version = "v1" });

    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Cole aqui apenas o token JWT (sem o prefixo Bearer)."
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)] = []
    });
});

// CORS: origens lidas do appsettings (Cors:OrigensPermitidas). AllowCredentials
// é obrigatório para o navegador enviar/receber o cookie httpOnly de autenticação.
var origensPermitidas = builder.Configuration
    .GetSection("Cors:OrigensPermitidas").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(opcoes =>
{
    opcoes.AddPolicy("CorsVueDev", politica =>
        politica
            .WithOrigins(origensPermitidas)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

// EF Core
builder.Services.AddDbContext<KoinoniaHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// JWT: valida o token e o lê a partir do cookie httpOnly "kh_token"
var chave = builder.Configuration["Jwt:ChaveSecreta"] ?? throw new InvalidOperationException("Jwt:ChaveSecreta não configurado.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Emissor"],
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave))
        };

        // O token passa a chegar pelo cookie httpOnly, não mais pelo header Authorization.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = contexto =>
            {
                if (contexto.Request.Cookies.TryGetValue("kh_token", out var tokenDoCookie)
                    && !string.IsNullOrWhiteSpace(tokenDoCookie))
                {
                    contexto.Token = tokenDoCookie;
                }
                return Task.CompletedTask;
            }
        };
    });

// Política padrão (Plano 6.16, 7.1): qualquer endpoint sem atributo de autorização exige
// usuário autenticado, para que uma rota nova nasça protegida. Os públicos (login,
// cadastro inicial, primeiro acesso, termo vigente) estão marcados com [AllowAnonymous].
builder.Services.AddAuthorization(opcoes =>
{
    opcoes.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 403 por perfil com corpo { mensagem }, como o resto da API (corretiva da 7.3).
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, RespostaAutorizacaoHandler>();

// Repositórios
builder.Services.AddScoped<IIgrejaRepositorio, IgrejaRepositorio>();
builder.Services.AddScoped<IPessoaRepositorio, PessoaRepositorio>();
builder.Services.AddScoped<IDepartamentoRepositorio, DepartamentoRepositorio>();
builder.Services.AddScoped<IMateriaRepositorio, MateriaRepositorio>();
builder.Services.AddScoped<IAulaRepositorio, AulaRepositorio>();
builder.Services.AddScoped<IAlunoDepartamentoRepositorio, AlunoDepartamentoRepositorio>();
builder.Services.AddScoped<IPresencaRepositorio, PresencaRepositorio>();
builder.Services.AddScoped<IAtribuicaoRepositorio, AtribuicaoRepositorio>();
builder.Services.AddScoped<IParentescoRepositorio, ParentescoRepositorio>();

// Serviços
builder.Services.AddScoped<IIgrejaServico, IgrejaServico>();
builder.Services.AddScoped<IPessoaServico, PessoaServico>();
builder.Services.AddScoped<IDepartamentoServico, DepartamentoServico>();
builder.Services.AddScoped<IMateriaServico, MateriaServico>();
builder.Services.AddScoped<IAulaServico, AulaServico>();
builder.Services.AddScoped<IMatriculaServico, MatriculaServico>();
builder.Services.AddScoped<IChamadaServico, ChamadaServico>();
builder.Services.AddScoped<IPresencaHistoricoServico, PresencaHistoricoServico>();
builder.Services.AddScoped<IAtribuicaoServico, AtribuicaoServico>();
builder.Services.AddScoped<IParentescoServico, ParentescoServico>();
builder.Services.AddScoped<IRelatorioEbdServico, RelatorioEbdServico>();

builder.Services.AddScoped<ITokenServico, TokenServico>();
builder.Services.AddScoped<IAuthServico, AuthServico>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IUsuarioServico, UsuarioServico>();
builder.Services.AddScoped<IPessoaImportacaoServico, PessoaImportacaoServico>();

// Termo de Uso e Sigilo (RF42/RF43)
builder.Services.AddScoped<IAceiteTermoServico, AceiteTermoServico>();

// Segurança por atribuição
builder.Services.AddScoped<IAutorizacaoEbdServico, AutorizacaoEbdServico>();

// Dia civil da igreja (Igreja:FusoHorario): períodos dos relatórios e pendência de fechamento.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<RelogioIgreja>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS antes de auth
app.UseCors("CorsVueDev");

// RNF 2.6: escrita só de origens autorizadas (defesa em profundidade contra CSRF).
// Com SameSite=None, POST sem corpo JSON (convite, logout) e multipart (importação CSV)
// são requisições simples e não passam pelo preflight do CORS; aqui o servidor confere
// o Origin, que o navegador preenche em toda requisição de escrita e scripts não alteram.
// Requisições de escrita sem Origin também são recusadas (OWASP, CSRF Prevention Cheat Sheet).
app.Use(async (contexto, proximo) =>
{
    var metodo = contexto.Request.Method;
    var ehLeitura = HttpMethods.IsGet(metodo) || HttpMethods.IsHead(metodo) || HttpMethods.IsOptions(metodo);

    if (!ehLeitura)
    {
        var origem = contexto.Request.Headers.Origin.ToString();

        if (!origensPermitidas.Contains(origem, StringComparer.OrdinalIgnoreCase))
        {
            contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
            await contexto.Response.WriteAsJsonAsync(new { mensagem = "Origem não autorizada." });
            return;
        }
    }

    await proximo();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

// Program é público pelo gerador do SDK do .NET 10 (WebApplicationFactory<Program> nos testes).
