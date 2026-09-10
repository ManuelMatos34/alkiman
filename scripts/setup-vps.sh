#!/usr/bin/env bash
# setup-vps.sh — Configura el VPS desde cero para correr Alkiman con Docker
#
# Requisitos: Ubuntu 22.04+ / Debian 12+, ejecutar como root o con sudo.
#
# Uso:
#   ssh root@IP_DEL_VPS
#   curl -sL https://raw.githubusercontent.com/TU_USUARIO/alkiman/main/scripts/setup-vps.sh | bash

set -euo pipefail

echo "==> Actualizando paquetes..."
apt-get update -q && apt-get upgrade -y -q

echo "==> Instalando Docker..."
curl -fsSL https://get.docker.com | sh
systemctl enable docker
systemctl start docker

echo "==> Creando directorio de la app..."
mkdir -p /opt/alkiman/nginx/certbot/{conf,www}
cd /opt/alkiman

echo "==> Descargando archivos de configuración desde el repositorio..."
# Ajusta la URL si tu repo es privado (usa un token de acceso personal)
BASE_RAW="https://raw.githubusercontent.com/ManuelMatos34/alkiman/main"
curl -sL "$BASE_RAW/docker-compose.yml"  -o docker-compose.yml
curl -sL "$BASE_RAW/nginx/nginx.conf"    -o nginx/nginx.conf
curl -sL "$BASE_RAW/.env.example"        -o .env.example

echo ""
echo "==> Próximos pasos:"
echo "    1. Edita /opt/alkiman/nginx/nginx.conf y reemplaza TU_DOMINIO"
echo "    2. Copia .env.example a .env y completa los valores reales:"
echo "       cp /opt/alkiman/.env.example /opt/alkiman/.env"
echo "       nano /opt/alkiman/.env"
echo ""
echo "    3. Obtén el certificado SSL (solo la primera vez):"
echo "       cd /opt/alkiman"
echo "       docker run --rm -v ./nginx/certbot/conf:/etc/letsencrypt \\"
echo "                        -v ./nginx/certbot/www:/var/www/certbot \\"
echo "                        -p 80:80 certbot/certbot certonly --standalone \\"
echo "                        -d TU_DOMINIO --email tu@email.com --agree-tos -n"
echo ""
echo "    4. Levanta la app:"
echo "       docker compose --env-file .env up -d"
echo ""
echo "==> Instalación base completada."
