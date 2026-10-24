# Building a UI provider

KSI2 is the reference provider. Its public `Ksi2UiProvider` class is marked
`[KsiUiProvider]`, implements `IKsiUiProvider`, and has a public parameterless
constructor. KSI-Core discovers that type after StarMap loads all mods.

A provider should keep `Initialize` passive: retain the context without touching
KSA input, global layouts, or rendering state. Core calls it on the provider's
first activation. Start textures and input hooks in `Activate`, cancel input and
hide any native KSA copies in `Deactivate`, draw
inside `Draw`, and release resources in `Dispose`. The host calls `Deactivate`
for an active provider before `Dispose`; providers should tolerate a cleanup
call after a partially failed `Activate`. `DrawOptions` draws provider controls
inside KSI-Core's configuration window, below the interface selector. KSI-Core
owns the window and its body style; providers do not open another main settings
window. `OnConfigurationClosed` can close secondary editor windows.
`OnKsaLayoutChanged` receives the selected KSA layout, including its stable save key.

The supplied `IKsiUiContext` offers:

- `Canvas` for standard ImGui artwork and input windows.
- `Assets` for registered KSA asset IDs and texture preparation.
- `Audio` for registered sounds and live screen-position normalization.
- `Layouts` for the selected KSA layout, normalized positioning, and KSA
  profile registration.
- `Settings` for JSON files scoped to the provider ID. Pass only the file name.
- `ModMenu` for optional toggle items under the KSI ModMenu entry. Toggle IDs
  are scoped to the provider ID.

A provider can use raw ImGui in `Draw` and can own its own layout format.
KSI2 uses the Core services above while keeping its navball, panel geometry,
and KSP2-inspired behavior in its own repository.

The provider mod's `mod.toml` must declare a required StarMap dependency on
`KSI-Core` and import `KSI.Core.Api`. Add a compile-time package reference to
the matching `KSI.Core.Api` NuGet version. Do not ship another runtime copy of
the API DLL in the provider folder; StarMap resolves it from KSI-Core.

Place `<entry assembly name>.ksi-provider.json` beside the provider DLL with
`apiMinVersion` and `apiMaxVersionExclusive` strings. Core checks this range
against its installed API assembly before inspecting provider types. A missing
or incompatible file causes Core to skip the assembly and show an error in its
configuration window. The referenced API assembly version must also be no
newer than the installed API. Keep a provider within the same API minor version
unless the API explicitly documents compatibility beyond it.

KSA's selected HUD layout and native canvas state are global. A provider that
changes either must restore its own temporary changes on `Deactivate`, without
overriding a later layout choice made by the user. Input hooks must also be
removed on `Deactivate`; Core calls that method before activating another UI.
KSI2 currently retains a persistent `KSI2 Default` KSA default-layout flag for
existing startup behavior. Core does not yet keep a separate default KSA layout
per provider. A future provider that needs native gauges may need a dedicated
layout, and Core should add per-provider layout selection before treating such
switches as fully isolated.
