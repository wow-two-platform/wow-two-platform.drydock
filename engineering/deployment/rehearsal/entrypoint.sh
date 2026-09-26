#!/bin/sh
set -eu
keys=/rehearsal-keys
# A persisted host key keeps the pinned known_hosts valid across container replacement.
[ -f "$keys/ssh_host_ed25519_key" ] || ssh-keygen -q -t ed25519 -N '' -f "$keys/ssh_host_ed25519_key"
install -m 600 "$keys/ssh_host_ed25519_key" /etc/ssh/ssh_host_ed25519_key
install -m 644 "$keys/ssh_host_ed25519_key.pub" /etc/ssh/ssh_host_ed25519_key.pub
install -d -m 700 -o deploy -g deploy /home/deploy/.ssh
install -m 600 -o deploy -g deploy "$keys/authorized_keys" /home/deploy/.ssh/authorized_keys
# The deploy account joins the Docker socket's group, as the docker group grants on a VPS.
gid=$(stat -c %g /var/run/docker.sock)
group=$(getent group "$gid" | cut -d: -f1 || true)
[ -n "$group" ] || { addgroup -g "$gid" dockerhost; group=dockerhost; }
addgroup deploy "$group"
chown deploy:deploy /srv/wheelhouse
exec /usr/sbin/sshd -D -e -f /etc/ssh/sshd_rehearsal_config
