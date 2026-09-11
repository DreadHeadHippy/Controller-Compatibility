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
