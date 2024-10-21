namespace DouglasDwyer.CasCore;

/// <summary>
/// Identifies a member of subset of members by their external visibility.
/// </summary>
public enum Accessibility
{
    /// <summary>
    /// Identifies no members.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Identifies all public members.
    /// </summary>
    Public = 1,

    /// <summary>
    /// Identifies all public or family members.
    /// </summary>
    Protected = 2,

    /// <summary>
    /// Identifies all members.
    /// </summary>
    Private = 3
}