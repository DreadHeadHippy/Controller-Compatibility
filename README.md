
# Playnite Controller Compatibility Plugin

<p align="center">
  <img src="in-action01.png" alt="Controller Compatibility Plugin in action" width="300">
  <img src="in-action02.png" alt="Controller Compatibility Plugin overlays" width="300">
</p>

A Playnite plugin that provides controller compatibility detection and management, similar to Steam's controller compatibility system.

## Features

### 🎮 Controller Detection
- Automatically detects connected controllers (Xbox, PlayStation, Nintendo, Generic)
- Real-time controller status monitoring
- Support for multiple controllers simultaneously

### 🎯 Game Compatibility Database  
- Maintains compatibility ratings for games:
  - ✅ **Full Controller Support** - Native gamepad support
  - 🎮 **Partial Controller Support** - Some limitations or workarounds needed
  - ❌ **No Controller Support** - Keyboard/mouse only

## Installation & Testing

### Quick Test Build
1. Run `build.bat` to compile the plugin
2. Copy the contents of `bin\Release\net462\` to:
   ```
   %AppData%\Playnite\Extensions\ControllerCompatibility\
   ```
3. Restart Playnite
4. Enable the plugin in Settings → Extensions

### What You'll See
- **Controller Status**: Top panel shows connected controllers
- **Game Overlays**: Look for colored badges on game tiles in your library (top-right corner if in 'grid view')
- **Context Menus**: Right-click games for compatibility options
- **Visual Feedback**: Badges appear/disappear based on controller connections

### Test Data
The plugin includes test compatibility data for popular games. Connect a controller to see the full effect!

## Development

### Requirements
- .NET Framework 4.6.2 or higher
- Playnite SDK
- Visual Studio 2019+ or VS Code with C# extension

### Building
```bash
dotnet build
```

### Testing
```bash
dotnet test
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by Steam's controller compatibility system
- Built for the amazing [Playnite](https://playnite.link/) game library manager