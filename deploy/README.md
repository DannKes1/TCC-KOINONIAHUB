# Implantação em VPS

Ubuntu 24.04 com Docker Engine e o plugin Compose. Três contêineres (`docker-compose.prod.yml`,
na raiz): `db` (PostgreSQL 18), `api` (.NET 10, porta interna 8080) e `caddy` (front estático e
proxy de `/api`, HTTPS automático com HSTS). De fora só o Caddy responde, nas portas 80 e 443.

## Antes da primeira subida

1. DNS: registros `A` do domínio e de `www` apontando para o IP da VPS (o Caddy só emite o
   certificado com o DNS resolvendo).
2. Firewall: liberar 22, 80 e 443 (`sudo ufw allow 22,80,443/tcp`; o restante do servidor está
   em [Hardening](#hardening)).
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

O diretório de backups está no mesmo disco do banco: as cópias saem do servidor uma vez por
semana (ver [Hardening](#hardening), item 5).

## Hardening

Aplicado em 05/11/2026 na VPS de produção.

### 1. SSH só por chave

Acesso somente por chave ed25519; senha desligada para todos e `root` só por chave. Gerar a
chave na máquina de quem administra e copiá-la para o servidor **antes** de desligar a senha:

```bash
ssh-keygen -t ed25519
ssh-copy-id usuario@IP_DA_VPS
```

No servidor, `/etc/ssh/sshd_config.d/00-hardening.conf`:

```
PasswordAuthentication no
PermitRootLogin prohibit-password
```

No `sshd` vale a primeira ocorrência de cada opção, e os arquivos de `sshd_config.d/` são lidos
em ordem alfabética. O prefixo `00-` garante que estes valores vencem os de qualquer outro arquivo
do diretório (por exemplo, um `50-cloud-init.conf` com `PasswordAuthentication yes`).

```bash
sudo sshd -t                                                       # sintaxe
sudo sshd -T | grep -Ei '^(passwordauthentication|permitrootlogin)' # no / prohibit-password
sudo systemctl reload ssh
```

Conferir de outro terminal, sem fechar a sessão aberta, que a senha é recusada:

```bash
ssh -o PubkeyAuthentication=no root@IP_DA_VPS
# root@IP_DA_VPS: Permission denied (publickey).    a senha nem é oferecida
ssh root@IP_DA_VPS                                   # com a chave: entra
```

### 2. Fail2ban

Bloqueia por 1 hora o IP com 5 falhas de login no SSH. No Ubuntu 24.04 o serviço se chama
`ssh.service` e o log está no journal do systemd; daí o `backend` e o `journalmatch` da jail
(arquivo local da configuração, por exemplo `/etc/fail2ban/jail.local`):

```
[sshd]
enabled      = true
backend      = systemd
journalmatch = _SYSTEMD_UNIT=ssh.service + _COMM=sshd
bantime      = 1h
maxretry     = 5
```

```bash
sudo apt install fail2ban
sudo systemctl enable --now fail2ban
sudo systemctl restart fail2ban                    # depois de mudar a jail
systemctl is-active fail2ban                       # active
sudo fail2ban-client get sshd journalmatch         # _SYSTEMD_UNIT=ssh.service + _COMM=sshd
sudo fail2ban-client status sshd                   # falhas e IPs bloqueados
```

### 3. Atualizações de segurança automáticas

```bash
sudo apt install unattended-upgrades
sudo dpkg-reconfigure -plow unattended-upgrades    # responde "Sim": grava /etc/apt/apt.conf.d/20auto-upgrades
cat /etc/apt/apt.conf.d/20auto-upgrades            # Update-Package-Lists "1"; Unattended-Upgrade "1"
sudo unattended-upgrade --dry-run --debug 2>&1 | tail -5   # simulação, sem instalar nada
```

Só as atualizações de segurança do Ubuntu são instaladas sozinhas. As imagens dos contêineres
(PostgreSQL, .NET, Caddy) são atualizadas com `build` e `up -d` (seção [Atualizar](#atualizar)).

### 4. Firewall e portas expostas

```bash
sudo ufw allow OpenSSH          # 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 443/udp          # HTTP/3, que o Caddy liga por padrão
sudo ufw enable
sudo ufw status                 # só essas regras, em IPv4 e IPv6
```

Só 22 (SSH), 80 e 443 (Caddy) ficam abertas. `db` e `api` não têm `ports` em
`docker-compose.prod.yml`: só são alcançáveis pela rede interna do compose. Isso importa porque
uma porta publicada pelo Docker passa ao largo das regras do UFW. Para conferir:

```bash
docker compose -f docker-compose.prod.yml ps --format "table {{.Service}}\t{{.Ports}}"
# api 8080/tcp e db 5432/tcp sem endereço do host (não publicadas);
# só o caddy com 0.0.0.0:80, 0.0.0.0:443 (tcp e udp) e os mesmos em [::]
```

### 5. Cópia semanal do backup para fora do servidor

Uma vez por semana, os dumps de `/opt/koinoniahub/backups/` são copiados por `scp` para fora da
VPS, a partir da máquina de quem administra (a mesma chave ed25519 do item 1):

```bash
scp 'usuario@IP_DA_VPS:/opt/koinoniahub/backups/koinoniahub-*.dump' <pasta-local-de-backups>/
```

A restauração de uma cópia trazida de fora segue a seção
[Cópia e restauração do banco](#cópia-e-restauração-do-banco).

## Parar e remover

```bash
docker compose -f docker-compose.prod.yml down          # mantém os volumes (dados e certificados)
docker compose -f docker-compose.prod.yml down -v       # apaga tudo, inclusive o banco
```
