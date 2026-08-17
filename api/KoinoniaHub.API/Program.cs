using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Aplicacao.Servicos.Implementacoes;
using KoinoniaHub.API.Aplicacao.Servicos.Interfaces;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Infraestrutura.Repositorios;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        x.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KoinoniaHub.API", Version = "v1" });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Cole aqui o token JWT: Bearer {seu_token}",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
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
var chave = builder.Configuration["Jwt:ChaveSecreta"] ?? throw new Exception("Jwt:ChaveSecreta não configurado.");
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

builder.Services.AddAuthorization();

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

// Segurança por atribuição
builder.Services.AddScoped<IAutorizacaoEbdServico, AutorizacaoEbdServico>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS antes de auth
app.UseCors("CorsVueDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
