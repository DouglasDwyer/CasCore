using System.Reflection;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Event handler that is invoked when an assembly attempts to access a field or method without permission.
/// </summary>
public interface ICasViolationHandler
{
    /// <summary>
    /// Called when a sandboxed assembly accesses a field or method that is not
    /// on the <see cref="CasPolicy"/> whitelist. If this method does not throw an
    /// exception, then control flow will return to the sandboxed assembly and the
    /// invalid access will complete successfully.
    /// </summary>
    /// <param name="caller">The offending assembly.</param>
    /// <param name="member">The member that it attempted to read, write, or call.</param>
    void OnViolation(Assembly caller, MemberInfo member);
}