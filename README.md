# KoinoniaHub

Sistema web de gestão da Escola Bíblica Dominical (EBD) — um Diário de Classe Digital desenvolvido para a 4ª Igreja Presbiteriana do Brasil em Ji-Paraná/RO.

> **Trabalho de Conclusão de Curso** — Tecnologia em Análise e Desenvolvimento de Sistemas (CST-ADS)
> Instituto Federal de Rondônia (IFRO), Campus Ji-Paraná


## Sobre o projeto

O KoinoniaHub substitui o diário de classe em papel da EBD por um sistema web, permitindo:

- Registro digital de presença (chamada) por aula;
- Histórico de frequência por aluno e por turma;
- Relatórios e painel consolidado de acompanhamento;
- Gestão de turmas, matérias, matrículas e pessoas;
- Acesso seguro com perfis de usuário (RBAC em dois níveis: perfil global + atribuição por turma).

O tratamento de dados observa a LGPD (Lei nº 13.709/2018), com atenção especial a dados de menores e ao contexto de convicção religiosa (dado pessoal sensível).

## Tecnologias

| Camada            | Stack                                                                                                                     |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------- |
| Backend (`/api`)  | ASP.NET Core 8 (.NET 8), Entity Framework Core, PostgreSQL, autenticação JWT em cookie httpOnly, multi-tenancy por igreja |
| Frontend (`/web`) | Vue 3, TypeScript, PrimeVue, Pinia, Vite                                                                                  |

## Estrutura do repositório

```
api/   → KoinoniaHub.API (ASP.NET Core 8)
web/   → koinoniahub.web (Vue 3 + TypeScript)
```

## Como executar

### Backend

```bash
cd cd api/KoinoniaHub.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Database=koinoniahub;Username=postgres;Password=SUA_SENHA"
dotnet user-secrets set "Jwt:ChaveSecreta" "uma-chave-longa-e-aleatoria-com-mais-de-32-caracteres"
dotnet ef database update
dotnet run
```

### Frontend

```bash
cd web
npm install
npm run dev
```

## Linha do tempo do desenvolvimento

O sistema foi desenvolvido **localmente entre fevereiro e agosto de 2026**, em paralelo à elaboração do projeto de pesquisa (TCC 1), e importado para este repositório em agosto/2026 como commit inicial. As migrations em `api/**/Migrations` preservam os carimbos de data originais da evolução do schema (primeira migration: `20260222234613_Inicial`).

A partir deste ponto, o versionamento acompanha as etapas do TCC 2: testes funcionais por caso de uso, validação com usuários segundo a ISO/IEC 25010 e ajustes finais.

## Uso

Projeto acadêmico. Todos os direitos reservados ao autor; uso e reprodução mediante autorização.
