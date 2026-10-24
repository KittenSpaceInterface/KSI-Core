namespace KSI.Core.Mod.Integration;

// ModMenu discovers this attribute by name; the host can still run without ModMenu.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class ModMenuEntryAttribute(string menuName) : Attribute
{
    public string MenuName { get; } = menuName;
}
