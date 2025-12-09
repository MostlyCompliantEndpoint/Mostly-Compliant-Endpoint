using AssignedAccessDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AssignedAccessDesigner.Serialization
{

    // Register **every type** you plan to serialize/deserialize.
    [JsonSerializable(typeof(AssignedAccessPolicy))]
    [JsonSerializable(typeof(List<AssignedAccessPolicy>))]  // <— add
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(decimal))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>))]


    public partial class AppJsonContext : JsonSerializerContext
    {
        // The generator will produce AppJsonContext.g.cs during build.
        // Access via AppJsonContext        // Access via AppJsonContext.Default.<TypeInfo>
    }

}
