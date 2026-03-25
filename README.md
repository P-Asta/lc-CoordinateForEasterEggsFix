# Asta.CfeeLowVersionFix

`Asta.CfeeLowVersionFix` is a BepInEx compatibility plugin for Lethal Company that makes `CoordinateForEasterEggs` work on lower versions where it would normally only work on higher versions.

The original mod can crash on some older game/mod combinations after `StunGrenadeItem.SetExplodeOnThrowClientRpc(bool)` when it tries to send an extra custom `ClientRpc`. If that RPC is not registered on the target version, the game can throw a `KeyNotFoundException`.

This fix avoids that path and generates the visual egg line locally on the client using data the game has already synchronized.

In short, this project exists to bring a higher-version-only mod behavior down to lower-version environments.

## Goal

The purpose of this project is not to replace CFEE.

Its job is to keep the original mod usable on lower versions by removing the fragile version-dependent RPC path that breaks there.

## What It Fixes

This project makes CFEE usable on lower versions by stopping crashes in a flow like this:

1. `StunGrenadeItem.SetExplodeOnThrowClientRpc(bool)` runs.
2. The original CFEE patch calls `EggLineSync.Instance.EggLinesClientRpc(...)`.
3. A lower-version environment does not have that custom RPC registered.
4. Netcode lookup fails and the game throws.

Instead of relying on the extra custom RPC, this fix:

- blocks the original `EggLinesClientRpc(...)` path
- blocks the original `SetExplodeOnThrowClinetRPCExtension(...)` path
- listens to `SetExplodeOnThrowClientRpc(bool)` directly
- calls the original CFEE `GenerateEggLine(Vector3, bool)` locally through reflection

## Behavior

At runtime, the plugin:

- installs Harmony bypass patches over the original CFEE network extension methods
- keeps the base game RPC flow intact
- generates the egg line locally after the stun grenade receives its explode state
- regenerates the indicator when the grenade is picked up again

This keeps the effect visible without depending on a custom networked RPC path.

## Requirements

- Lethal Company
- BepInEx 5
- `CoordinateForEasterEggs` installed

This plugin has a hard dependency on the original CFEE mod and does nothing on its own.

Plugin metadata:

- GUID: `asta.cfee.lowverfix`
- Name: `CFEE Low Version Fix`
- Version: `0.1.0`

## Installation

1. Install the original `CoordinateForEasterEggs` mod.
2. Build this project or use the compiled DLL.
3. Put `CFEELowVersionFix.dll` into your BepInEx `plugins` folder.
4. Launch the game.

Default build output:

- `src/bin/Debug/netstandard2.1/CFEELowVersionFix.dll`

## Build

Project file:

- `src/Asta.CfeeLowVersionFix.csproj`

Build command:

```powershell
dotnet build src\Asta.CfeeLowVersionFix.csproj
```

## Development Notes

The `.csproj` currently references Lethal Company assemblies from the default Steam install path:

- `c:/Program Files (x86)/Steam/steamapps/common/Lethal Company/Lethal Company_Data/Managed/Assembly-CSharp.dll`
- `c:/Program Files (x86)/Steam/steamapps/common/Lethal Company/Lethal Company_Data/Managed/Unity.Netcode.Runtime.dll`
- `c:/Program Files (x86)/Steam/steamapps/common/Lethal Company/Lethal Company_Data/Managed/Unity.InputSystem.dll`
- `c:/Program Files (x86)/Steam/steamapps/common/Lethal Company/Lethal Company_Data/Managed/Unity.TextMeshPro.dll`
- `c:/Program Files (x86)/Steam/steamapps/common/Lethal Company/Lethal Company_Data/Managed/UnityEngine.UI.dll`

If your game is installed somewhere else, update the `HintPath` values in the project file.

## Project Layout

- `src/CfeeLowVersionFixPlugin.cs`: plugin entry point
- `src/OriginalCfeeHooks.cs`: CFEE bypass hooks and reflection-based access to `GenerateEggLine`
- `src/Patches/StunGrenadeItemSetExplodeOnThrowClientRpcPatch.cs`: local egg line trigger
- `LethalCompanyPatched.sln`: Visual Studio solution

## Limitations

- This is a compatibility workaround, not a full rewrite of CFEE.
- If the original CFEE type names or method names change, the reflection lookup may need updates.
- Runtime testing still needs to be done in-game.

## Summary

The main idea is simple:

avoid the fragile `custom ClientRpc + internal handler` path from the higher-version implementation and reuse the game's existing `SetExplodeOnThrowClientRpc(bool)` signal so CFEE can still function on lower versions.
