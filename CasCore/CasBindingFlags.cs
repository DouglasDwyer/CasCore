namespace DouglasDwyer.CasCore;

[Flags]
public enum CasBindingFlags
{
    Default = 0,
    DeclaredOnly = 2,
    Instance = 4,
    Static = 8,
    Public = 16,
    NonPublic = 32,
    Field = 64,
    Constructor = 128,
    Method = 256,
    Member = Field | Constructor | Method | Static | Instance,
    All = Public | NonPublic | Member
}