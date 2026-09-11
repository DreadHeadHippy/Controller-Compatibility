## [1.0.5] - 2026-09-11

### Fixed
- Game tile overlays no longer randomly flip to the wrong compatibility color while scrolling through the game grid
- Replaced the periodic visual-tree scan (which raced against Playnite's container virtualization/recycling) with an event-driven approach that updates a tile's overlay synchronously the instant its bound game changes
- Compatibility database entries are now keyed by the game's stable Playnite GUID instead of a source/name-derived key, preventing the same game from resolving to conflicting stored values
- Game tile overlay badges now use the original controller icon instead of a single letter

## [1.0.4] - 2026-09-11

### Fixed
- Game tile overlays now populate automatically again; the tile control reads the Game from Playnite's actual tile DataContext wrapper (e.g. GamesCollectionViewEntry) instead of only accepting a raw Game object, which never matched in real grid/theme tiles

## [1.0.3] - 2026-09-11

### Fixed
- Game tile overlays no longer randomly flip to the wrong compatibility color when clicking between unrelated games
- Removed a legacy visual-tree overlay system that matched game tiles by fuzzy name matching, which broke under Playnite's UI virtualization/container recycling
- Manual compatibility overrides now notify only the specific tile control bound to that game, instead of triggering a global, error-prone tile scan
- Removed a loose substring-based name-matching fallback in the compatibility database that could return a different game's compatibility data

## [1.0.1] - 2025-11-02

### Added
- Initial public release of Playnite Controller Compatibility Plugin
- Controller detection (Xbox, PlayStation, Nintendo, Generic)
- Game compatibility overlays and top panel status
- Manual and auto compatibility overrides
