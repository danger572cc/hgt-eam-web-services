using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace HGT.EAM.WebServices.Conector.Architecture.Extensions;

public static class ObjectExtensions
{
    public static bool CanBeConverted<T>(this object value) where T : class
    {
        var jsonData = JsonConvert.SerializeObject(value);
        var generator = new JSchemaGenerator();
        var parsedSchema = generator.Generate(typeof(T));
        var jObject = JObject.Parse(jsonData);

        return jObject.IsValid(parsedSchema);
    }

    public static T ConvertToType<T>(this object value) where T : class
    {
        var jsonData = JsonConvert.SerializeObject(value);
        return JsonConvert.DeserializeObject<T>(jsonData);
    }
}