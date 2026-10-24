# KSI.Core.Api

Public contracts for KSI-Core UI providers. Implement `IKsiUiProvider` in a
public class with a public parameterless constructor and add `[KsiUiProvider]`.
KSI-Core supplies `IKsiUiContext` during initialization and calls `Draw` only
while the provider is selected. Providers can use the context services or draw
directly with KSA's ImGui API.

Declare a StarMap dependency on the `KSI-Core` mod and import
`KSI.Core.Api`. Borea handles the separate installation dependency.
Place `<entry assembly name>.ksi-provider.json` beside the provider DLL with
an inclusive `apiMinVersion` and exclusive `apiMaxVersionExclusive`. Core
checks these before inspecting provider types. `Initialize` runs only on the
provider's first activation; put temporary KSA input and canvas hooks in
`Activate` and release them in `Deactivate`.
