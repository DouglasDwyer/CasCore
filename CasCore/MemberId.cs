using System.Reflection;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Identifies a specific field or method implementation.
/// </summary>
internal record struct MemberId
{
#pragma warning disable IDE0052
    /// <summary>
    /// The member's token.
    /// </summary>
    private readonly int _token;

    /// <summary>
    /// A handle to the module in which the member resides.
    /// </summary>
    private readonly ModuleHandle _module;
#pragma warning restore IDE0052

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
}