# Architecture

`KSI.Core.Api` is the contract package. It defines `IKsiUiProvider`, the
context passed to providers, and service interfaces. It contains no provider
selection state. Provider mods compile against this package.

`KSI.Core` owns the provider lifecycle. `KsiProviderHost` registers providers,
initializes each only when first activated, activates one provider, invokes its draw and options
callbacks, forwards KSA layout changes, and disposes providers. It depends on
the API contracts and knows nothing about KSA or ModMenu.

`KSI.Core.Mod` is the composition root and the only project that handles
StarMap callbacks. Its adapters implement the API services with KSA, ImGui,
ModMenu, and JSON storage. The host entry point discovers attributed provider
types after mod loading, checks each assembly's API compatibility metadata before
type inspection, connects the services, and draws the selector above
the active provider's controls in one configuration window.

The dependency direction is API <- Core <- Mod. A provider depends on the API
at compile time and imports the API assembly exported by the KSI-Core mod at
runtime. StarMap loads provider assemblies; KSI-Core activates their UI
instances. Borea manages the installation relationship between packages.

One UI is active at a time. Before any interface has ever been loaded, the
first registered interface is selected in the selector, ready to load. A draw
failure deactivates that provider and opens the selector. A failed activation restores
the previously active UI.
Choosing None and unloading deactivates the active provider while keeping it
registered for a later load. The saved active provider is cleared without
clearing the history of a successful load.
Each provider receives settings and ModMenu facades bound to its registered ID.
Its settings directory is under KSI-Core's `settings/` directory, and the host
stores only the selected provider and selector state.
Unreadable settings are preserved and reported; Core does not replace them with
defaults. An affected provider fails initialization until its file is repaired.

The service facades are optional. A provider may use Core's canvas setup,
assets, audio, positioning, KSA layout bridge, settings, and ModMenu option
registration while drawing its specialized widgets directly with ImGui.
The selected KSA layout and saved default-layout flag remain global KSA state.
Core serializes provider activation and deactivation; providers release their
temporary input and canvas changes before another provider activates. Core does
not yet maintain separate saved KSA default layouts for each provider.
