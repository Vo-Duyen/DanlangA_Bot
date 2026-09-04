using System.Text.Json.Serialization;

namespace DanlangA_Bot.Core.Models;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(PetManifest))]
[JsonSerializable(typeof(FsmConfig))]
[JsonSerializable(typeof(IpcMessage))]
public sealed partial class AppJsonContext : JsonSerializerContext
{
}
