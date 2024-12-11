using System.Linq.Expressions;
using System.Reflection;
using System.Security;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Rewrites a LINQ expression tree, adding runtime checks for code access security.
/// </summary>
internal class GuardExpressionVisitor : ExpressionVisitor
{
    /// <summary>
    /// The assembly that is compiling the expression tree.
    /// </summary>
    private readonly Assembly _assembly;

    /// <summary>
    /// Creates a new visitor for rewriting expression trees.
    /// </summary>
    /// <param name="callingAssembly">The assembly that is compiling expression trees.</param>
    public GuardExpressionVisitor(Assembly callingAssembly)
    {
        _assembly = callingAssembly;
    }

    /// <inheritdoc/>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.Method is not null)
        {
            return Expression.MakeBinary(
                node.NodeType,
                Visit(node.Left),
                Visit(node.Right),
                node.IsLiftedToNull,
                GuardMethodInfo.Create(_assembly, node.Method),
                (LambdaExpression?)Visit(node.Conversion)
            );
        }
        else
        {
            return VisitBinary(node);
        }
    }

    /// <inheritdoc/>
    protected override Expression VisitDynamic(DynamicExpression node)
    {
        throw new SecurityException("Compiling System.Linq.Expressions.DynamicExpression is not supported in CAS contexts.");
    }

    /// <inheritdoc/>
    protected override ElementInit VisitElementInit(ElementInit node)
    {
        return Expression.ElementInit(GuardMethodInfo.Create(_assembly, node.AddMethod), Visit(node.Arguments));
    }

    /// <inheritdoc/>
    protected override Expression VisitIndex(IndexExpression node)
    {
        return Expression.MakeIndex(
            Visit(node.Object)!,
            node.Indexer is null ? null : GuardPropertyInfo.Create(_assembly, node.Indexer),
            Visit(node.Arguments));
    }

    /// <inheritdoc/>
    protected override Expression VisitMember(MemberExpression node)
    {
        return Expression.MakeMemberAccess(Visit(node.Expression), CreateGuardMemberInfo(node.Member));
    }

    /// <inheritdoc/>
    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        return Expression.Bind(CreateGuardMemberInfo(node.Member), Visit(node.Expression));
    }

    /// <inheritdoc/>
    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
    {
        return Expression.ListBind(CreateGuardMemberInfo(node.Member), Visit(node.Initializers, VisitElementInit));
    }

    /// <inheritdoc/>
    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
    {
        return Expression.MemberBind(CreateGuardMemberInfo(node.Member), Visit(node.Bindings, VisitMemberBinding));
    }

    /// <inheritdoc/>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object is null)
        {
            return Expression.Call(Visit(node.Object), GuardMethodInfo.Create(_assembly, node.Method), Visit(node.Arguments));
        }
        else
        {
            return Expression.Call(GuardMethodInfo.Create(_assembly, node.Method), Visit(node.Arguments));
        }
    }

    /// <inheritdoc/>
    protected override Expression VisitNew(NewExpression node)
    {
        if (node.Constructor is null)
        {
            throw new SecurityException("Compiling System.Linq.Expressions.NewExpression with null constructor not supported in CAS contexts.");
        }
        else if (node.Members is null)
        {
            return Expression.New(GuardConstructorInfo.Create(_assembly, node.Constructor), Visit(node.Arguments));
        }
        else
        {
            return Expression.New(GuardConstructorInfo.Create(_assembly, node.Constructor), Visit(node.Arguments), node.Members.Select(CreateGuardMemberInfo));
        }
    }

    /// <inheritdoc/>
    protected override Expression VisitSwitch(SwitchExpression node)
    {
        return Expression.Switch(
            Visit(node.SwitchValue),
            Visit(node.DefaultBody),
            node.Comparison is null ? null : GuardMethodInfo.Create(_assembly, node.Comparison),
            Visit(node.Cases, VisitSwitchCase));
    }

    /// <inheritdoc/>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.Method is null)
        {
            return Expression.MakeUnary(node.NodeType, Visit(node.Operand), node.Type, null);
        }
        else
        {
            return Expression.MakeUnary(node.NodeType, Visit(node.Operand), node.Type, GuardMethodInfo.Create(_assembly, node.Method));
        }
    }

    /// <summary>
    /// Wraps the provided member in a runtime-checked class if necessary.
    /// </summary>
    /// <param name="member">The member to wrap.</param>
    /// <returns>The safeguarded member.</returns>
    /// <exception cref="SecurityException">If the provided member was of an unrecognized type.</exception>
    private MemberInfo CreateGuardMemberInfo(MemberInfo member)
    {
        if (member is FieldInfo field)
        {
            CasAssemblyLoader.CheckAccess(_assembly, field);
            return field;
        }
        else if (member is PropertyInfo property)
        {
            return GuardPropertyInfo.Create(_assembly, property);
        }
        else if (member is MethodInfo method)
        {
            return GuardMethodInfo.Create(_assembly, method);
        }
        else
        {
            throw new SecurityException($"Compiling expression with member {member} not supported in CAS contexts.");
        }
    }
}