using System.Collections.Immutable;
using System.Reflection;

namespace DouglasDwyer.CasCore;

public sealed class CasPolicy
{
    private readonly ImmutableHashSet<MemberId> _accessibleMembers;

    internal CasPolicy(ImmutableHashSet<MemberId> members)
    {
        _accessibleMembers = members;
    }

    internal bool CanAccess(FieldInfo field)
    {
        var memberId = new MemberId(field);
        return _accessibleMembers.Contains(memberId);
    }

    internal bool CanAccess(MethodBase method)
    {
        var memberId = new MemberId(method);
        return _accessibleMembers.Contains(memberId);
    }
}