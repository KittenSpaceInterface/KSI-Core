# KSI-Core

KSI-Core hosts selectable flight interfaces in Kitten Space Agency. StarMap loads each
provider as an ordinary mod. KSI-Core discovers public classes marked
`[KsiUiProvider]` after KSA has loaded mod assets, initializes a provider on
its first activation, and draws only
the selected provider. The chosen provider is restored at the next launch.

The solution has three production projects:

- `KSI.Core.Api` contains the public provider and service contracts and is packed
  as a versioned NuGet package for provider builds.
- `KSI.Core` contains the provider lifecycle and selection logic.
- `KSI.Core.Mod` is the StarMap entry point and owns KSA, ImGui, ModMenu, asset,
  audio, layout, and JSON adapters.

Providers receive optional asset, audio, layout, settings, and ModMenu services
through `IKsiUiContext`. They may draw directly with KSA's ImGui API inside
`IKsiUiProvider.Draw`. KSI2 is the first provider and reference implementation.

## Build

Pass a local KSA assembly directory when the workspace `Import` directory is
unavailable:

```powershell
dotnet restore KSI.Core.slnx -p:KsaReferencePath="C:\path\to\Kitten Space Agency"
dotnet build KSI.Core.slnx --configuration Release --no-restore -p:KsaReferencePath="C:\path\to\Kitten Space Agency"
dotnet pack src/KSI.Core.Api/KSI.Core.Api.csproj --configuration Release --no-build --output artifacts/packages -p:KsaReferencePath="C:\path\to\Kitten Space Agency"
dotnet run --project tests/KSI.Core.Tests --configuration Release -p:KsaReferencePath="C:\path\to\Kitten Space Agency"
```

The `KSI.Core.Mod` Release output is the code mod payload. Its `mod.toml`
exports `KSI.Core.Api` for StarMap dependents. A provider must declare a
StarMap dependency on the `KSI-Core` mod and import that assembly. Borea can
manage installation dependencies separately.

For a manual installation, the `KSI-Core` mod folder needs `mod.toml`,
`KSI.Core.Mod.dll`, `KSI.Core.Mod.deps.json`, `KSI.Core.dll`, and
`KSI.Core.Api.dll` from `src/KSI.Core.Mod/bin/Release/net10.0/`. The provider
API assembly must come from this folder at runtime; providers reference its
NuGet package for compilation and import the installed assembly through StarMap.

On Windows, `tools/build-install.sh` builds, tests, packs the API, and installs
KSI-Core while preserving its `settings/` directory. Override `KSA_REFERENCE_PATH`
or `KSI_CORE_MOD_DIR` for other local paths. Install provider mods separately.

ModMenu is optional at runtime. Its KSI entry opens one configuration window
with the interface selector above the active provider's controls. When no
interface has ever been loaded, the first registered interface is preselected.
Without ModMenu, KSI-Core opens the same configuration window automatically.
Choose **None** in the selector and click **Unload interface** to stop the active
UI. This retains the provider so it can be loaded again later.
