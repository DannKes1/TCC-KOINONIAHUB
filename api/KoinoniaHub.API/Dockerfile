# API (implantação, Entrega F): publicação em Release no SDK e execução no runtime aspnet,
# como usuário não root (app, que a própria imagem traz) na porta 8080 (padrão de
# ASPNETCORE_HTTP_PORTS). Contexto de build: esta pasta (docker-compose.prod.yml, serviço api).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY KoinoniaHub.API.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publicado --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# curl só para o healthcheck do compose (a imagem não traz curl nem wget);
# tzdata para Igreja:FusoHorario (America/Porto_Velho).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl tzdata \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publicado ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "KoinoniaHub.API.dll"]
