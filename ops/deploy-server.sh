#!/bin/bash
set -euo pipefail
umask 077
app=/home/apartmani-tokic/atokicasp
app_parent=/home/apartmani-tokic
archive=${1:?Usage: deploy-server.sh /absolute/path/to/release.tar.gz}
case "$archive" in /tmp/atokicasp-release-*.tar.gz) ;; *) echo "Unexpected archive path" >&2; exit 2;; esac
[ -f "$archive" ] || { echo "Release archive not found" >&2; exit 2; }
exec 8>/run/lock/atokicasp-deploy.lock
flock -n 8 || { echo "Another deployment is running" >&2; exit 1; }
stage=$(mktemp -d /var/tmp/atokicasp-release.XXXXXXXX)
cleanup() { rm -rf -- "$stage"; }
trap cleanup EXIT

# Reject paths, links, or files that could alter production secrets or photos.
python3 - "$archive" <<'PY'
import pathlib, sys, tarfile
with tarfile.open(sys.argv[1], 'r:gz') as archive:
    for item in archive:
        name = item.name.removeprefix('./').rstrip('/')
        parts = pathlib.PurePosixPath(name).parts
        if name in ('', '.'): continue
        if item.name.startswith('/') or '..' in parts or not (item.isfile() or item.isdir()):
            raise SystemExit('Unsafe archive entry: ' + item.name)
        if (parts[0].startswith('appsettings') and parts[0].endswith('.json')) or parts[:2] == ('wwwroot', 'img'):
            raise SystemExit('Protected file in release archive: ' + item.name)
PY
tar -xzf "$archive" -C "$stage" --no-same-owner
for file in Apartmani Apartmani.dll Apartmani.deps.json Apartmani.runtimeconfig.json; do
    [ -f "$stage/$file" ] || { echo "Release missing $file" >&2; exit 2; }
done
backup_output=$(/usr/local/sbin/atokicasp-backup)
printf '%s\n' "$backup_output"
backup_path=$(printf '%s\n' "$backup_output" | sed -n 's/^Backup created and verified: //p' | tail -1)
case "$backup_path" in /var/backups/atokicasp/atokicasp-*.tar.gz) ;; *) echo "No verified rollback archive" >&2; exit 1;; esac
[ -f "$backup_path" ] || exit 1

fix_permissions() {
    chown -R root:www-data "$app"
    chmod -R u=rwX,g=rX,o=rX "$app"
    chmod 755 "$app/Apartmani"
    find "$app" -maxdepth 1 -type f -name 'appsettings*.json' -exec chmod 640 {} +
    chown -R www-data:www-data "$app/wwwroot/img"
    find "$app/wwwroot/img" -type d -exec chmod 755 {} +
    find "$app/wwwroot/img" -type f -exec chmod 644 {} +
}
health_check() {
    for attempt in {1..15}; do
        if systemctl is-active --quiet kestrel-atokicasp.service && curl -fsS http://127.0.0.1:5002/ >/dev/null; then
            curl -fsS http://127.0.0.1:5002/Gallery >/dev/null &&
            curl -fsS http://127.0.0.1:5002/Contact >/dev/null && return 0
        fi
        sleep 2
    done
    return 1
}
rollback() {
    echo "Deployment failed; restoring previous application files" >&2
    systemctl stop kestrel-atokicasp.service || true
    tar -xzf "$backup_path" -C "$app_parent" \
        --exclude='atokicasp/wwwroot/img' --exclude='atokicasp/wwwroot/img/*' \
        --exclude='atokicasp/appsettings*.json' atokicasp
    fix_permissions
    systemctl start kestrel-atokicasp.service
    if health_check; then echo "Previous application restored" >&2; else echo "ROLLBACK HEALTH CHECK FAILED" >&2; fi
}
trap 'status=$?; if [ "$status" -ne 0 ] && [ "${deploy_started:-0}" = 1 ]; then rollback || true; fi; cleanup; exit "$status"' EXIT
deploy_started=1
systemctl stop kestrel-atokicasp.service
tar -cf - -C "$stage" . | tar -xf - -C "$app" --no-same-owner
fix_permissions
systemctl start kestrel-atokicasp.service
health_check || { echo "New release did not pass health checks" >&2; exit 1; }
deploy_started=0
echo "Deployment complete; service and public pages passed local checks."
