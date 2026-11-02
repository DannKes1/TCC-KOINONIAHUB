# Implantação em VPS

Ubuntu 24.04 com Docker Engine e o plugin Compose. Três contêineres (`docker-compose.prod.yml`,
na raiz): `db` (PostgreSQL 18), `api` (.NET 10, porta interna 8080) e `caddy` (front estático e
proxy de `/api`, HTTPS automático com HSTS). De fora só o Caddy responde, nas portas 80 e 443.

## Antes da primeira subida

1. DNS: registros `A` do domínio e de `www` apontando para o IP da VPS (o Caddy só emite o
   certificado com o DNS resolvendo).
2. Firewall: liberar 22, 80 e 443 (`sudo ufw allow 22,80,443/tcp`).
3. Código em `/opt/koinoniahub/app` (caminho que `deploy/backup.sh` assume):

```bash
sudo mkdir -p /opt/koinoniahub && sudo chown "$USER" /opt/koinoniahub
git clone <url-do-repositorio> /opt/koinoniahub/app
cd /opt/koinoniahub/app
cp .env.example .env
nano .env        # DOMINIO, POSTGRES_PASSWORD (openssl rand -base64 24), JWT_CHAVE_SECRETA (openssl rand -base64 48)
chmod 600 .env
```

O front chama a API por caminho relativo (`/api`, mesma origem); não há `VITE_API_BASE_URL`
a definir. A origem autorizada da API (`Cors__OrigensPermitidas__0`) é `https://DOMINIO`,
montada pelo compose a partir do `.env`.

## Subir

```bash
cd /opt/koinoniahub/app
docker compose -f docker-compose.prod.yml config --quiet     # valida o compose e o .env
docker compose -f docker-compose.prod.yml up -d --build
docker compose -f docker-compose.prod.yml ps                 # db e api "healthy", caddy "running"
curl -I https://DOMINIO                                      # 200, Strict-Transport-Security
curl -s https://DOMINIO/api/saude                            # {"situacao":"ok"}
curl -I http://www.DOMINIO                                   # 301 para https://DOMINIO
```

Na primeira subida a API aplica as migrations antes de responder (o healthcheck espera até
60 s). Depois, abrir `https://DOMINIO` e fazer o cadastro inicial da igreja (RF1).

## Atualizar

```bash
cd /opt/koinoniahub/app
git pull
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d
```

Migrations novas são aplicadas pela API na subida. Mudança só no `deploy/Caddyfile`:
`docker compose -f docker-compose.prod.yml restart caddy`.

## Logs

```bash
docker compose -f docker-compose.prod.yml logs -f api
docker compose -f docker-compose.prod.yml logs -f caddy
docker compose -f docker-compose.prod.yml logs --since 1h db
```

## Cópia e restauração do banco

`deploy/backup.sh` grava `pg_dump -Fc` em `/opt/koinoniahub/backups/koinoniahub-AAAAMMDD-HHMM.dump`
e apaga cópias com mais de 7 dias. Agendar às 03:00 em `sudo crontab -e`:

```
0 3 * * * /opt/koinoniahub/app/deploy/backup.sh >> /var/log/koinoniahub-backup.log 2>&1
```

Restaurar uma cópia (a API fica parada durante a restauração):

```bash
cd /opt/koinoniahub/app
docker compose -f docker-compose.prod.yml stop api
docker compose -f docker-compose.prod.yml exec -T db \
    sh -c 'pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists' \
    < /opt/koinoniahub/backups/koinoniahub-AAAAMMDD-HHMM.dump
docker compose -f docker-compose.prod.yml start api
```

Copie as cópias para fora da VPS com regularidade (`scp`, por exemplo): o diretório de
backups está no mesmo disco do banco.

## Parar e remover

```bash
docker compose -f docker-compose.prod.yml down          # mantém os volumes (dados e certificados)
docker compose -f docker-compose.prod.yml down -v       # apaga tudo, inclusive o banco
```
