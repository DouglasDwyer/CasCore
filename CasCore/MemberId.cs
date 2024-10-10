using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DouglasDwyer.CasCore;

internal struct MemberId
{
    private readonly int _token;
    private readonly ModuleHandle _module;

    public MemberId(MemberInfo member)
    {
        _token = member.MetadataToken;
        _module = member.Module.ModuleHandle;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is MemberId other)
        {
            return this == other;
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return _token.GetHashCode() ^ _module.GetHashCode();
    }

    public static bool operator ==(MemberId lhs, MemberId rhs)
    {
        return lhs._token == rhs._token && lhs._module == rhs._module;
    }

    public static bool operator !=(MemberId lhs, MemberId rhs)
    {
        return !(lhs == rhs);
    }
}