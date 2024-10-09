using System.Reflection;
using System.Runtime.Loader;

namespace DouglasDwyer.CasCore;

public sealed class CasPolicy
{
    internal bool CanAccess(FieldInfo field)
    {
        return true;
    }

    internal bool CanAccess(MethodBase method)
    {
        Console.WriteLine($"Check {method} ==> {method.DeclaringType.Name}");
        return !method.DeclaringType.Name.Contains("Banned") && !method.Name.Contains("Banned") && !method.Name.Contains("MessageBox");
    }
}