namespace KSI.Core.Api.Providers;

public sealed record KsiLayoutSelection(string Id, long CreatedTicks)
{
    public string Key => $"{Id}|{CreatedTicks}";
}
