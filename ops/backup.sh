#!/bin/bash
set -euo pipefail
umask 077

backup_dir=/var/backups/atokicasp
sql_dir=/var/opt/mssql/backup/atokicasp
app_parent=/home/apartmani-tokic
install -d -o root -g root -m 700 "$backup_dir"
install -d -o mssql -g mssql -m 700 "$sql_dir"
exec 9>/run/lock/atokicasp-backup.lock
flock -n 9 || { echo "Backup already running" >&2; exit 1; }

free_kb=$(df -Pk "$backup_dir" | awk 'NR==2 {print $4}')
if (( free_kb < 5242880 )); then
    echo "Backup aborted: less than 5 GiB free" >&2
    exit 1
fi

stage=$(mktemp -d "$backup_dir/.pending.XXXXXXXX")
sql_file="$sql_dir/atokicasp-$(date -u +%Y%m%dT%H%M%S)-$$.bak"
cleanup() {
    rm -f -- "$sql_file"
    rm -rf -- "$stage"
}
trap cleanup EXIT

dotnet /usr/local/lib/atokicasp-backup/DatabaseBackup.dll "$sql_file"
cp -- "$sql_file" "$stage/database.bak"
tar -czf "$stage/archive.tar.gz" \
    -C "$app_parent" atokicasp \
    -C /var/lib atokicasp-keys \
    -C /etc/systemd/system kestrel-atokicasp.service kestrel-atokicasp.service.d atokicasp-backup.service atokicasp-backup.timer \
    -C /etc/apache2/sites-available apartmani-tokic.com.conf \
    -C /usr/local/sbin atokicasp-backup \
    -C /usr/local/lib atokicasp-backup \
    -C "$stage" database.bak
tar -tzf "$stage/archive.tar.gz" >/dev/null
name="atokicasp-$(date -u +%Y%m%dT%H%M%S)-$$.tar.gz"
mv -- "$stage/archive.tar.gz" "$backup_dir/$name"
chmod 600 "$backup_dir/$name"
sha256sum "$backup_dir/$name" > "$backup_dir/$name.sha256"
chmod 600 "$backup_dir/$name.sha256"
sha256sum --check --status "$backup_dir/$name.sha256"

# Only prune archives created by this script after the new archive has passed verification.
mapfile -d '' archives < <(find "$backup_dir" -maxdepth 1 -type f \
    -name 'atokicasp-????????T??????-*.tar.gz' -print0 | sort -z)
remove_count=$((${#archives[@]} - 7))
if (( remove_count > 0 )); then
    for ((index = 0; index < remove_count; index++)); do
        old=${archives[index]}
        if [[ -f "$old.sha256" ]]; then
            rm -f -- "$old" "$old.sha256"
        fi
    done
fi
echo "Backup created and verified: $backup_dir/$name"
