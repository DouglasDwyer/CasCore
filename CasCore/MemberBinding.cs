namespace DouglasDwyer.CasCore;

public abstract class MemberBinding
{
    internal abstract IEnumerable<MemberId> Members { get; }
}