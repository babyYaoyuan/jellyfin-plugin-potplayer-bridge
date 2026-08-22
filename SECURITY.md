# Security policy

## Supported versions

Only the latest release is supported with security fixes.

## Reporting a vulnerability

Please use GitHub's private vulnerability reporting feature instead of opening
a public issue. Include the Jellyfin version, plugin version, deployment model,
and a minimal reproduction when possible.

## Deployment notes

- Keep **Bind ticket to remote address** enabled unless a trusted reverse proxy
  makes the browser and PotPlayer appear under different addresses.
- Use HTTPS when Jellyfin is accessible outside a trusted LAN.
- A stream ticket grants temporary access to one resolved Jellyfin library file.
  Treat a ticket URL as sensitive until it expires.
