using System.Diagnostics;
using System.Reflection;
using System.Security;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Raises a security exception whenever a CAS violation occurs.
/// </summary>
public sealed class ExceptionViolationHandler : ICasViolationHandler
{
    /// <inheritdoc/>
    [StackTraceHidden]
    public void OnViolation(Assembly caller, MemberInfo member)
    {
        throw new SecurityException(FormatSecurityException(caller, member));
    }

    /// <summary>
    /// Produces the text description for a security access exception.
    /// </summary>
    /// <param name="assembly">The assembly that attempted the illegal access.</param>
    /// <param name="member">The member that was accessed.</param>
    /// <returns>A string describing the problem.</returns>
    private static string FormatSecurityException(Assembly assembly, MemberInfo member)
    {
        return FormatSecurityException(assembly.GetName().Name, member);
    }

    /// <summary>
    /// Creates a string describing a member access security violation.
    /// </summary>
    /// <param name="assemblyName">The assembly that attempted to access a member.</param>
    /// <param name="member">The member which the assembly is not allowed to access.</param>
    /// <returns>The string describing the error.</returns>
    private static string FormatSecurityException(string? assemblyName, MemberInfo member)
    {
        if (assemblyName is null)
        {
            return $"Assembly does not have permission to access {member} of {member.DeclaringType}.";
        }
        else
        {
            return $"Assembly {assemblyName} does not have permission to access {member} of {member.DeclaringType}.";
        }
    }
}