using System.Reflection;

namespace DouglasDwyer.CasCore;

public sealed class CasPolicy
{
    private Dictionary<nint, MemberInfo> _restrictedVirtualMethods;
    private HashSet<MemberInfo> _restrictedMembers;
    private Dictionary<string, Action<CasPolicy>> _lazyRestrictions;

    
}