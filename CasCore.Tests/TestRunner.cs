using System.Reflection;

namespace DouglasDwyer.CasCore.Tests;

public class TestRunner
{
    public static void Run()
    {
        var assy = Assembly.GetExecutingAssembly();
        var passedTests = 0;
        var totalTests = 0;

        foreach (var method in assy.GetTypes().SelectMany(x => x.GetMethods()))
        {
            var testExceptionAttr = method.GetCustomAttribute<TestExceptionAttribute>();
            if (testExceptionAttr is not null)
            {
                try
                {
                    method.Invoke(null, []);
                    throw new Exception("Method completed without exception.");
                }
                catch (TargetInvocationException e)
                {
                    if (e.InnerException!.GetType().IsAssignableTo(testExceptionAttr.ExpectedException))
                    {
                        passedTests++;
                    }
                    else
                    {
                        Console.WriteLine($"{method} failed: {e.InnerException}");
                    }
                }

                totalTests++;
            }

            var testSuccessfulAttr = method.GetCustomAttribute<TestSuccessfulAttribute>();
            if (testSuccessfulAttr is not null)
            {
                try
                {
                    method.Invoke(null, []);
                    passedTests++;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{method} failed: {e.InnerException}");
                }

                totalTests++;
            }
        }

        Console.WriteLine($"Test results: {passedTests} passed, {totalTests - passedTests} failed, {totalTests} total");
        Console.ReadKey();
    }
}