using System.Linq.Expressions;
using System.Reflection;
using System.Security;

namespace DouglasDwyer.CasCore;

internal class GuardExpressionVisitor : ExpressionVisitor
{
    private readonly Assembly _assembly;

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
            return base.VisitBinary(node);
        }
    }

    protected override Expression VisitDynamic(DynamicExpression node)
    {
        throw new SecurityException("Compiling System.Linq.Expressions.DynamicExpression is not supported in CAS contexts.");
    }

    protected override ElementInit VisitElementInit(ElementInit node)
    {
        return base.VisitElementInit(node);
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        return base.VisitIndex(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        return base.VisitMember(node);
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        return base.VisitMemberAssignment(node);
    }

    protected override MemberBinding VisitMemberBinding(MemberBinding node)
    {
        return base.VisitMemberBinding(node);
    }

    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
    {
        return base.VisitMemberListBinding(node);
    }

    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
    {
        return base.VisitMemberMemberBinding(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitNew(NewExpression node)
    {
        return base.VisitNew(node);
    }

    protected override Expression VisitSwitch(SwitchExpression node)
    {
        return base.VisitSwitch(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        return base.VisitUnary(node);
    }
}