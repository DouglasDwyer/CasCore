namespace DouglasDwyer.CasCore;

[Flags]
public enum CasBindingFlags
{
    Default = 0,
    DeclaredOnly = 2,
    Instance = 4,
    Static = 8,
    Field = 64,
    Constructor = 128,
    Method = 256,
    All = Field | Constructor | Method | Static | Instance
}