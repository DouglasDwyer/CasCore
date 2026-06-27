namespace DouglasDwyer.CasCore.Tests.Host;

using DouglasDwyer.CasCore;
using DouglasDwyer.CasCore.Tests.Shared;
using System.Reflection;

internal class Program
{
    public static void Main()
    {
        var testAssyNames = new[] { "CasCore.Tests.Cil.dll", "CasCore.Tests.Csharp.dll" };

        var policy = new CasPolicyBuilder()
            .WithDefaultSandbox()
            .Allow(new TypeBinding(typeof(SharedClass), Accessibility.None)
                .WithConstructor([], Accessibility.Public)
                .WithField(nameof(SharedClass.AllowedStaticField), Accessibility.Public)
                .WithField(nameof(SharedClass.AllowedField), Accessibility.Public)
                .WithField(nameof(SharedClass.AllowedReadonlyStaticField), Accessibility.Public)
                .WithField(nameof(SharedClass.AllowedReadonlyField), Accessibility.Public)
                .WithMethod(nameof(SharedClass.InterfaceMethod), Accessibility.Public)
)
            .Allow(new TypeBinding(typeof(SharedClass.SharedNested), Accessibility.None)
                .WithConstructor([], Accessibility.Public)
                .WithMethod("VirtualMethod", Accessibility.Public))
            .Build();

        var loadContext = new CasAssemblyLoader(policy);
        loadContext.LoadFromStream(new FileStream(Path.Combine(AppContext.BaseDirectory, "Newtonsoft.Json.dll"), FileMode.Open));

        Run(testAssyNames.Select(name => {
                var pdbPath = Path.Combine(AppContext.BaseDirectory, name.Replace(".dll", ".pdb"));
                var dllStream = new FileStream(Path.Combine(AppContext.BaseDirectory, name), FileMode.Open);
                var pdbStream = File.Exists(pdbPath) ? new FileStream(pdbPath, FileMode.Open) : null;
                return loadContext.LoadFromStream(dllStream, pdbStream);
            })
            .ToArray());
    }

    public static void Run(Assembly[] assys)
    {
        var passedTests = 0;
        var totalTests = 0;

        foreach (var method in assys.SelectMany(assy => assy.GetTypes().SelectMany(x => x.GetMethods())))
        {
            var testExceptionAttr = method.GetCustomAttribute<TestExceptionAttribute>();
            if (testExceptionAttr is not null)
            {
                try
                {
                    method.Invoke(null, []);
                    throw new TargetInvocationException(new Exception("Method completed without exception."));
                }
                catch (Exception e)
                {
                    if (e.InnerException is TypeInitializationException initException)
                    {
                        e = initException;
                    }

                    if (e.InnerException!.GetType().IsAssignableTo(testExceptionAttr.ExpectedException))
                    {
                        passedTests++;
                    }
                    else
                    {
                        Console.WriteLine($"{method.DeclaringType!.Name}.{method.Name} failed: {e.InnerException}");
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
                    Console.WriteLine($"{method.DeclaringType!.Name}.{method.Name} failed: {e.InnerException}");
                }

                totalTests++;
            }
        }

        Console.WriteLine($"Test results: {passedTests} passed, {totalTests - passedTests} failed, {totalTests} total");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey();
        }
    }
}