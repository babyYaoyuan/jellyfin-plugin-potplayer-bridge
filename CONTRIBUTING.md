# Contributing

Issues and pull requests are welcome.

## Development

Requirements:

- .NET SDK 9
- Node.js, only for the JavaScript syntax check

Run the checks from the repository root:

```powershell
dotnet restore .\PotPlayerBridge.slnx
dotnet test .\PotPlayerBridge.slnx -c Release --no-restore
node --check .\src\Jellyfin.Plugin.PotPlayerBridge\Web\potplayer-bridge.js
```

Keep pull requests focused. Do not include media paths, server addresses,
Jellyfin access tokens, or stream tickets in tests, screenshots, or logs.
