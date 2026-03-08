using System.Linq.Expressions;
using System.Security;
using DouglasDwyer.CasCore.Tests.Shared;

namespace DouglasDwyer.CasCore.Tests;

public static class TestExpressionVisitor
{
    [TestSuccessful]
    public static void TestInstanceMethodCallExpression()
    {
        // Regression test: VisitMethodCall had its null check inverted - instance methods (node.Object != null)
        // fell into the else branch which called the static-method overload of Expression.Call,
        // causing an ArgumentException because the method requires an instance.
        var instance = Expression.Constant("hello");
        var method = typeof(string).GetMethod("ToUpper", [])!;
        var call = Expression.Call(instance, method);
        var lambda = Expression.Lambda<Func<string>>(call);
        lambda.Compile()();
    }

    [TestSuccessful]
    public static void TestBinaryExpressionNoMethod()
    {
        // Regression test: VisitBinary's else branch (node.Method == null, e.g. built-in int addition)
        // called VisitBinary(node) instead of base.VisitBinary(node), causing infinite
        // recursion and a StackOverflowException that crashes the process.
        var add = Expression.Add(Expression.Constant(1), Expression.Constant(2));
        var lambda = Expression.Lambda<Func<int>>(add);
        lambda.Compile()();
    }

    [TestSuccessful]
    public static void TestNewExpressionNullConstructorValueType()
    {
        // Value types have no ConstructorInfo for their implicit parameterless initializer,
        // so Expression.New(valueType).Constructor is null. This should be allowed.
        var newExpr = Expression.New(typeof(int));
        var lambda = Expression.Lambda<Func<int>>(newExpr);
        lambda.Compile()();
    }

    [TestSuccessful]
    public static void TestNewExpressionAllowedConstructor()
    {
        var ctor = typeof(SharedClass).GetConstructor([])!;
        var newExpr = Expression.New(ctor);
        var lambda = Expression.Lambda<Func<SharedClass>>(newExpr);
        lambda.Compile()();
    }

    [TestException(typeof(SecurityException))]
    public static void TestNewExpressionDeniedConstructor()
    {
        var ctor = typeof(SharedClass).GetConstructor([typeof(string)])!;
        var newExpr = Expression.New(ctor, Expression.Constant("denied"));
        var lambda = Expression.Lambda<Func<SharedClass>>(newExpr);
        lambda.Compile()();
    }

    [TestException(typeof(SecurityException))]
    public static void TestBinaryExpressionDeniedOperand()
    {
        // Regression test: a denied method call used as an operand in int + int (Method == null)
        // must be blocked. Verifies that guards are correctly applied to operands of null-Method
        // binary expressions.
        var deniedGetter = Expression.Call(
            Expression.Constant(new SharedClass()),
            typeof(SharedClass).GetProperty("DeniedProperty")!.GetMethod!);
        var add = Expression.Add(Expression.Constant(0), deniedGetter);
        var lambda = Expression.Lambda<Func<int>>(add);
        lambda.Compile()();
    }
}
