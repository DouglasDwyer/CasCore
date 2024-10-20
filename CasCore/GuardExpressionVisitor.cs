using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Security;

namespace DouglasDwyer.CasCore;

internal class GuardExpressionVisitor : ExpressionVisitor
{
    private readonly Assembly _assembly;

    public GuardExpressionVisitor(Assembly callingAssembly)
    {
        _assembly = callingAssembly;
    }

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
        return Expression.ElementInit(GuardMethodInfo.Create(_assembly, node.AddMethod), base.Visit(node.Arguments));
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        return Expression.MakeIndex(
            Visit(node.Object)!,
            node.Indexer is null ? null : GuardPropertyInfo.Create(_assembly, node.Indexer),
            Visit(node.Arguments));
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        return Expression.MakeMemberAccess(Visit(node.Expression), CreateGuardMemberInfo(node.Member));
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        return Expression.Bind(CreateGuardMemberInfo(node.Member), Visit(node.Expression));
    }

    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
    {
        return Expression.ListBind(CreateGuardMemberInfo(node.Member), Visit(node.Initializers, VisitElementInit));
    }

    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
    {
        return Expression.MemberBind(CreateGuardMemberInfo(node.Member), Visit(node.Bindings, VisitMemberBinding));
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object is null)
        {
            return Expression.Call(base.Visit(node.Object), GuardMethodInfo.Create(_assembly, node.Method), Visit(node.Arguments));
        }
        else
        {
            return Expression.Call(GuardMethodInfo.Create(_assembly, node.Method), Visit(node.Arguments));
        }
    }

    protected override Expression VisitNew(NewExpression node)
    {
        if (node.Constructor is null)
        {
            throw new SecurityException("Compiling System.Linq.Expressions.NewExpression with null constructor not supported in CAS contexts.");
        }
        else if (node.Members is not null && 0 < node.Members.Count)
        {
            throw new SecurityException("Compiling System.Linq.Expressions.NewExpression with Members property not supported in CAS contexts.");
        }
        else
        {
            return Expression.New(GuardConstructorInfo.Create(_assembly, node.Constructor), base.Visit(node.Arguments));
        }
    }

    protected override Expression VisitSwitch(SwitchExpression node)
    {
        return Expression.Switch(
            base.Visit(node.SwitchValue),
            base.Visit(node.DefaultBody),
            node.Comparison is null ? null : GuardMethodInfo.Create(_assembly, node.Comparison),
            Visit(node.Cases, VisitSwitchCase));
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.Method is null)
        {
            return Expression.MakeUnary(node.NodeType, base.Visit(node.Operand), node.Type, null);
        }
        else
        {
            return Expression.MakeUnary(node.NodeType, base.Visit(node.Operand), node.Type, GuardMethodInfo.Create(_assembly, node.Method));
        }
    }

    private MemberInfo CreateGuardMemberInfo(MemberInfo member)
    {
        if (member is FieldInfo field)
        {
            CasAssemblyLoader.AssertCanAccess(_assembly, field);
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