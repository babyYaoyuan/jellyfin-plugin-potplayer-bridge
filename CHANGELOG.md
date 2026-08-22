# Changelog

All notable changes to this project are documented in this file.

## 0.1.0.0 - 2026-08-22

- Add a PotPlayer button to Jellyfin Web movie and episode detail pages.
- Stream the original library file with HTTP Range support and no transcoding.
- Protect streams with random, expiring, item-scoped tickets.
- Bind tickets to the requesting client address by default.
- Bound the in-memory ticket pool to prevent unbounded growth.
- Avoid exposing Jellyfin access tokens in PotPlayer URLs.
- Preserve nested HTTP URLs on Chromium 130 and later.
- Support compressed Jellyfin Web index responses without modifying files on disk.
