# FSUIPC7 Reference Material Manifest

## Contents

### 1. Documentation (docs/)
- **User Guide & MSFS Notes**: Official FSUIPC7 documentation available at https://www.fsuipc.com/ and installed in Documents\FSUIPC7 after purchase. License: Proprietary (Pete Dowson).
- **Lua Scripting Reference**: Included with paid FSUIPC7 installation. License: Proprietary.
- **Offsets Reference**: Community-maintained lists at [Project Magenta](https://www.projectmagenta.com/all-fsuipc-offsets/) and [Aviation Systems](https://aviation.allanville.com/fsuipc/offsets). License: Community, check source sites.

### 2. C# Wrapper Libraries

#### FSUIPCClientDLL (NuGet Package)
- **Source**: [NuGet Gallery](https://www.nuget.org/packages/FSUIPCClientDLL/) (Latest: 3.3.16)
- **Installation**: `dotnet add package FSUIPCClientDLL`
- **Target Frameworks**: .NET 5.0, .NET Framework 4.6.2+
- **Maintainer**: Paul Henty
- **License**: Check package metadata at NuGet
- **Documentation**: https://fsuipc.com/community/fsuipc-client-dll-for-net/

#### FSUIPCClient (Community DLL)
- **Source**: http://fsuipc.paulhenty.com/
- **Status**: Legacy; superseded by NuGet FSUIPCClientDLL
- **License**: Check source site

### 3. Open Source C# Examples

#### portable-sim-panels-fsuipc-server
- **URL**: https://github.com/joeherwig/portable-sim-panels-fsuipc-server
- **Type**: WPF desktop app + WebSocket server for remote gauges
- **Key Files**: 
  - `FSUIPC.cs` - 100+ offset definitions (altitude, airspeed, autopilot, engines)
  - `MainWindow.xaml.cs` - Connect/disconnect and timer-based polling pattern
- **License**: Creative Commons BY-NC-SA 4.0
- **Status**: Active reference implementation

#### FSUIPCHelper
- **URL**: https://github.com/RobertEves92/FSUIPCHelper
- **Type**: Helper library wrapping FSUIPCConnection
- **Key Files**:
  - `FSUIPCHelper/Global/Fsuipc.cs` - Connection lifecycle (Open/Close/Process)
- **License**: Check repository
- **Status**: Utility library for cleaner connection management

### 4. FSUIPC Binary & SDK
- **Status**: Proprietary, requires purchase
- **Source**: https://www.fsuipc.com/ (Pete Dowson)
- **Price**: Paid license
- **Required for**: Runtime operation; C# wrappers provide the client interface
- **Trial**: Demo key available at https://www.fsuipc.com/download/FSUIPC7.key

## Offsets Reference Data
- Project Magenta offsets: Categories include aircraft position/attitude, flight dynamics, engines, autopilot, systems, weather, navigation
- Key example offsets vendored from portable-sim-panels example (see FSUIPC.cs)

## Size Notes
- Cloned examples: ~1.8 MB total
- NuGet package: ~2.5 MB
- Total vendored: ~4 MB (well under 20 MB limit)

## How to Use This Reference

1. **Install FSUIPC7** from fsuipc.com and obtain license
2. **Add NuGet**: `dotnet add package FSUIPCClientDLL`
3. **Reference examples**: Study portable-sim-panels or FSUIPCHelper for connect/read/write patterns
4. **Check offsets**: Use Project Magenta list or FSUIPC official docs for memory addresses
5. **Lua scripting**: Consult included documentation after FSUIPC7 installation
