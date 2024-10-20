using DouglasDwyer.CasCore.Tests.Shared;
using Newtonsoft.Json;
using System.Security;

namespace DouglasDwyer.CasCore.Tests;

public static class TestNewtonsoftJson
{
    [TestException(typeof(SecurityException))]
    public static string SerializeObjectWithDeniedProperties()
    {
        try
        {
            var writer = new StringWriter();
            JsonSerializer.CreateDefault().Serialize(writer, new SharedClass());
            return writer.ToString();
        }
        catch (JsonSerializationException e)
        {
            throw e.InnerException!;
        }
    }

    [TestSuccessful]
    public static string SerializeSerializableClass()
    {
        var writer = new StringWriter();
        JsonSerializer.CreateDefault().Serialize(writer, new SerializableClass());
        return writer.ToString();
    }

    [TestSuccessful]
    public static string SerializeJsonDictionary()
    {
        var dict = new Dictionary<string, object>();
        dict["spaghett"] = new object();

        var writer = new StringWriter();
        JsonSerializer.CreateDefault().Serialize(writer, dict);
        return writer.ToString();
    }

    public class SerializableClass
    {
        public int AllowedProperty { get; } = 20;
    }
}