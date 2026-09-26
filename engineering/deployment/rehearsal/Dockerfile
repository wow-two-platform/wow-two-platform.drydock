# syntax=docker/dockerfile:1
# Disposable SSH deployment target for local rehearsal. Never a production image.
FROM docker:cli
RUN apk add --no-cache openssh-server python3 \
    && adduser -D -s /bin/sh deploy \
    && sed -i 's/^deploy:!/deploy:*/' /etc/shadow
COPY sshd_config /etc/ssh/sshd_rehearsal_config
COPY entrypoint.sh /usr/local/bin/rehearsal-entrypoint
RUN chmod 755 /usr/local/bin/rehearsal-entrypoint
EXPOSE 22
ENTRYPOINT ["/usr/local/bin/rehearsal-entrypoint"]
