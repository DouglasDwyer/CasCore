using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Identifies a specific field or method implementation.
/// </summary>
internal struct MemberId
{
    /// <summary>
    /// The member's token.
    /// </summary>
    private readonly int _token;

    /// <summary>
    /// A handle to the module in which the member resides.
    /// </summary>
    private readonly ModuleHandle _module;

    /// <summary>
    /// Creates a new ID for the given member.
    /// </summary>
    /// <param name="member">The member to identify.</param>
    /// <exception cref="ArgumentException">If the member is not a field or method.</exception>
    public MemberId(MemberInfo member)
    {
        if (member is MethodBase
            || member is FieldInfo)
        {
            _token = member.MetadataToken;
            _module = member.Module.ModuleHandle;
        }
        else
        {
            throw new ArgumentException($"Cannot create CAS ID for member {member}. Only fields and methods may have CAS IDs.");
        }
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _token.GetHashCode() ^ _module.GetHashCode();
    }

    /// <summary>
    /// Compares two member IDs for equality.
    /// </summary>
    /// <param name="lhs">The first member ID.</param>
    /// <param name="rhs">The second member ID.</param>
    /// <returns>Whether the IDs refer to the same member.</returns>
    public static bool operator ==(MemberId lhs, MemberId rhs)
    {
        return lhs._token == rhs._token && lhs._module == rhs._module;
    }

    /// <summary>
    /// Compares two member IDs for inequality.
    /// </summary>
    /// <param name="lhs">The first member ID.</param>
    /// <param name="rhs">The second member ID.</param>
    /// <returns>Whether the IDs refer to the same member.</returns>
    public static bool operator !=(MemberId lhs, MemberId rhs)
    {
        return !(lhs == rhs);
    }
}