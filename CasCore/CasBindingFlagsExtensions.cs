using System.Reflection;

namespace DouglasDwyer.CasCore;

public static class CasBindingFlagsExtensions
{
    public static BindingFlags GetBindingFlags(this CasBindingFlags flags)
    {
        return (BindingFlags)((uint)flags & 63);
    }
}