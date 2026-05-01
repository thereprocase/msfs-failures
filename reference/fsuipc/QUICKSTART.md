# FSUIPC7 C# Integration Quick Reference

## Installation

1. **Install FSUIPC7** from https://www.fsuipc.com/ (requires purchase)
2. **Add NuGet package**:
   ```bash
   dotnet add package FSUIPCClientDLL
   ```

## Runtime Detection

```csharp
bool IsFsuipcAvailable()
{
    try 
    {
        FSUIPCConnection.Open();
        FSUIPCConnection.Close();
        return true;
    }
    catch 
    {
        return false;
    }
}
```

## Basic Connection & Data Loop

```csharp
using FSUIPC;

// Define offsets at class level
private static Offset<uint> Altitude = new Offset<uint>(0x3324);
private static Offset<uint> Airspeed = new Offset<uint>(0x02BC);
private static Offset<uint> Heading = new Offset<uint>(0x0580);

// Connect to FSUIPC
public void Connect()
{
    FSUIPCConnection.Open();
}

// Read simulator data
public void ReadData()
{
    try
    {
        FSUIPCConnection.Process();
        uint alt = Altitude.Value;
        uint spd = Airspeed.Value;
        uint hdg = Heading.Value;
    }
    catch (FSUIPCException ex)
    {
        Console.WriteLine($"Error reading: {ex.Message}");
    }
}

// Write to simulator
public void WriteData(uint newAltitude)
{
    try
    {
        Altitude.Value = newAltitude;
        FSUIPCConnection.Process();
    }
    catch (FSUIPCException ex)
    {
        Console.WriteLine($"Error writing: {ex.Message}");
    }
}

// Disconnect
public void Disconnect()
{
    FSUIPCConnection.Close();
}
```

## Common Offset Examples

| Variable | Offset | Type | Notes |
|----------|--------|------|-------|
| Indicated Altitude | 0x3324 | uint | feet |
| Airspeed Indicated | 0x02BC | uint | knots |
| Heading Magnetic | 0x0580 | uint | degrees |
| Vertical Speed | 0x02C8 | int | ft/min |
| Autopilot Master | 0x7BC | uint | 0=off, 1=on |
| Gear Handle Position | 0x0BE8 | uint | 0=up, 1=down |
| Flaps Left % | 0xBE0 | int | 0-100% |
| Landing Gear Left | 0x0BF4 | uint | 0=up, 1=down |

For full offset list, see `portable-sim-panels/FSUIPC.cs` or Project Magenta offsets reference.

## Error Handling Pattern

```csharp
catch (FSUIPCException ex)
{
    if (ex.FSUIPCErrorCode == FSUIPCError.FSUIPC_ERR_SENDMSG)
    {
        // Connection lost; reconnect
        FSUIPCConnection.Close();
        FSUIPCConnection.Open();
    }
}
```

## Key Takeaways

- **Offset<T>**: Generic wrapper for memory address + type (uint, int, float, string, etc.)
- **Process()**: Syncs all defined offsets with FSUIPC memory; required for read/write
- **Value**: Property to access offset data
- **Timer-based polling**: Use DispatcherTimer (WPF) or System.Timers.Timer for periodic updates
- **Thread-safe**: Consider locking if multi-threaded
