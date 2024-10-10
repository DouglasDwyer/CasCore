using System.Reflection;

namespace DouglasDwyer.CasCore;

public sealed class AssemblyBinding : MemberBinding
{
    internal override IEnumerable<MemberId> Members { get; }

    public AssemblyBinding(Assembly assembly, CasBindingFlags flags)
    {
        var members = new HashSet<MemberId>();
        
        foreach (var type in assembly.DefinedTypes)
        {
            if (type.IsPublic || flags.HasFlag(CasBindingFlags.NonPublic))
            {
                members.UnionWith(new TypeBinding(type, flags).Members);
            }
        }

        Members = members;
    }
}