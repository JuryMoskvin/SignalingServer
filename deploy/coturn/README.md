# TURN relay (coturn)

Fixes cross-network calls failing ("app closes the room" a few seconds after
connecting) — STUN alone can't traverse every NAT/firewall combination;
TURN is the relay fallback for when it can't.

## 1. Generate a password and deploy coturn on the VPS

```
openssl rand -hex 16
```

Edit `turnserver.conf`'s `user=webrtcuser:REPLACE_WITH_GENERATED_PASSWORD`
line with that value — only on the server, never commit a real secret here.

```
mkdir -p /opt/coturn && cd /opt/coturn
# copy turnserver.conf (with the real password) and docker-compose.yml from this folder here
docker compose up -d
```

Uses `network_mode: host` deliberately — coturn needs to advertise the
VPS's real public IP as the relay candidate, which is unreliable to get
right with Docker's bridge networking + explicit port mappings.

## 2. Open the ports in the VPS firewall

TURN doesn't go through nginx (it's raw UDP/TCP, not HTTP) — it needs direct
access to the VPS's public IP:

```
ufw allow 3478/udp
ufw allow 3478/tcp
ufw allow 49160:49200/udp
```

(adjust to whatever firewall the VPS actually uses — ufw, iptables, or the
provider's cloud security-group rules, if any sit in front of the VPS).

## 3. Point the signaling server at it

Add these environment variables to the SignalingServer container (same
password you generated in step 1 — must match `turnserver.conf`'s `user=` line):

```
Turn__Host=workoutgeneratorservice.kz
Turn__Port=3478
Turn__Username=webrtcuser
Turn__Password=<the password you generated above>
```

Then restart that container so it picks them up. `/ws/api/ice-servers`
should then return both the STUN entry and two TURN entries (UDP + TCP):

```
curl -s https://workoutgeneratorservice.kz/ws/api/ice-servers
```

## Rotating the password later

Change it in both places at once — `turnserver.conf`'s `user=` line and the
`Turn__Password` env var — then restart both containers. They must always
match.
