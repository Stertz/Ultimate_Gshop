using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ultimate_Gshop.Gshop.Misc;

public sealed class ShopChangeStackConverter : JsonConverter<Stack<ShopChange>>
{
    public override Stack<ShopChange> Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var changes = JsonSerializer.Deserialize<List<ShopChange>>(ref reader, options)
            ?? throw new JsonException("A history stack must be a JSON array.");
        // Existing files store the top first; Stack(IEnumerable) pushes the last element last.
        return new Stack<ShopChange>(changes.AsEnumerable().Reverse());
    }

    public override void Write(Utf8JsonWriter writer, Stack<ShopChange> value,
        JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value.ToArray(), options);
}
