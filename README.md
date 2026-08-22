# Jellyfin PotPlayer Bridge

[![Build](https://github.com/babyYaoyuan/jellyfin-plugin-potplayer-bridge/actions/workflows/build.yml/badge.svg)](https://github.com/babyYaoyuan/jellyfin-plugin-potplayer-bridge/actions/workflows/build.yml)
[![License: GPL-3.0](https://img.shields.io/badge/license-GPL--3.0-blue.svg)](LICENSE)

[简体中文](README.zh-CN.md)

Launch PotPlayer from a Jellyfin Web movie or episode page without a userscript,
browser extension, mapped network drive, or companion application.

> This is an unofficial community plugin. It is not affiliated with Jellyfin or
> PotPlayer, and it does not distribute PotPlayer.

## Features

- Adds a dedicated **Play with PotPlayer** button beside Jellyfin's normal play
  button.
- Sends the original library file to PotPlayer over an HTTP Range stream, with
  no media conversion or transcoding.
- Requires only the Jellyfin plugin on the server. The Windows client only needs
  a normal PotPlayer installation with its existing `potplayer:` protocol.
- Does not expose the Jellyfin access token in the external-player URL.
- Uses random, expiring, item-scoped stream tickets with client-address binding
  enabled by default.
- Injects the client script in memory; it does not modify Jellyfin Web files.
- Preserves nested `http://` URLs on Chromium 130 and later.

## Compatibility

- Jellyfin Server 10.11.x (`targetAbi` 10.11.0.0)
- Jellyfin Web movie and episode detail pages
- Windows with PotPlayer installed and the `potplayer:` URL protocol registered

The browser displays its own external-application confirmation on the first
launch. This security prompt cannot be bypassed by a server plugin.

## Install from the plugin repository

1. Open **Dashboard → Plugins → Repositories** in Jellyfin.
2. Add a repository named `PotPlayer Bridge` with this URL:

   ```text
   https://raw.githubusercontent.com/babyYaoyuan/jellyfin-plugin-potplayer-bridge/main/manifest.json
   ```

3. Open **Catalog**, install **PotPlayer Bridge**, and restart Jellyfin.
4. Hard-refresh Jellyfin Web once after the restart.

## Manual installation

1. Download the ZIP from the latest GitHub release.
2. Extract `Jellyfin.Plugin.PotPlayerBridge.dll` to a new directory under the
   Jellyfin plugin directory, for example:

   ```text
   /config/plugins/PotPlayerBridge_0.1.0.0/
   └── Jellyfin.Plugin.PotPlayerBridge.dll
   ```

3. Make sure the DLL is readable by the user that runs Jellyfin.
4. Restart Jellyfin and hard-refresh Jellyfin Web once.

Do not copy the source-tree `meta.json` during a manual Docker installation.
Jellyfin creates and owns its installed manifest.

## How it works

1. The plugin injects a small embedded client script into the Jellyfin Web index
   response.
2. The script adds the PotPlayer button to supported detail pages.
3. The authenticated Jellyfin user requests a short-lived ticket for the chosen
   library item.
4. The button opens `potplayer:http://...` using PotPlayer's existing Windows
   protocol registration.
5. PotPlayer reads the original file from the plugin endpoint with HTTP Range
   requests, enabling seeking without Jellyfin transcoding.

The opaque `potplayer:http://...` form is intentional. Chromium 130 and later
normalize `potplayer://http://...` into the broken `potplayer://http//...` form.

## Configuration

Open **Dashboard → Plugins → PotPlayer Bridge**.

- **Stream ticket lifetime:** 5 to 1440 minutes; default 720 minutes.
- **Bind ticket to remote address:** enabled by default and recommended.

If a trusted reverse proxy makes the browser and PotPlayer appear to come from
different addresses, address binding can be disabled. Prefer correct forwarded
address configuration where possible.

## Security model

- Ticket values contain 256 bits of randomness.
- Each ticket resolves to one existing item path returned by Jellyfin's library
  manager; clients cannot submit arbitrary filesystem paths.
- Ticket creation requires an authenticated Jellyfin user who can access the
  item.
- The external URL contains no Jellyfin access token.
- Responses are marked private/no-store and support only the lifetime of the
  ticket.

See [SECURITY.md](SECURITY.md) for reporting and deployment guidance.

## Current limitations

- PotPlayer playback progress is not reported back to Jellyfin yet.
- Sidecar subtitles are not attached automatically. Embedded subtitles remain
  available in the original media file.
- Playlists, automatic next episode, and multi-version selection are not yet
  implemented.
- Native TV/mobile clients that do not load Jellyfin Web do not get the button.
- Jellyfin Web DOM changes in future releases may require a plugin update.

## Build

Install .NET SDK 9 and run:

```powershell
dotnet restore .\PotPlayerBridge.slnx
dotnet test .\PotPlayerBridge.slnx -c Release --no-restore
dotnet publish .\src\Jellyfin.Plugin.PotPlayerBridge\Jellyfin.Plugin.PotPlayerBridge.csproj `
  -c Release --no-restore -o .\artifacts\publish
```

## License

GPL-3.0-only. See [LICENSE](LICENSE).
