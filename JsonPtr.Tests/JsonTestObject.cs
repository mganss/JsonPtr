using System.Text.Json;
using System.Text.Json.Nodes;
using Ganss.Text.Json;

namespace JsonPtr.Tests;

public abstract class JsonTestObject
{
    public abstract JsonValueKind ValueKind { get; }
    public abstract JsonTestObject? GetElement(string pointer);
    public abstract JsonTestObject? GetElementOrNull(string pointer);
    public abstract bool TryGetElement(string pointer, out JsonTestObject? result, out JsonPointerError? error);
    public abstract string? GetString();
    public abstract int GetInt32();
    public abstract bool GetBoolean();
    public abstract bool IsEqual(JsonTestObject other);
}

public class JsonTestElement(JsonElement jsonElement) : JsonTestObject
{
    private readonly JsonElement _jsonElement = jsonElement;

    public override JsonValueKind ValueKind => _jsonElement.ValueKind;

    public override JsonTestObject? GetElement(string pointer) => _jsonElement.GetElement(pointer) is JsonElement element ? new JsonTestElement(element) : null;

    public override JsonTestObject? GetElementOrNull(string pointer) => _jsonElement.GetElementOrNull(pointer) is JsonElement element ? new JsonTestElement(element) : null;

    public override bool TryGetElement(string pointer, out JsonTestObject? result, out JsonPointerError? error)
    {
        var success = _jsonElement.TryGetElement(pointer, out var element, out var err);
        error = err;
        result = success && element != null ? new JsonTestElement(element.Value) : null;
        return success;
    }

    public override string? GetString() => _jsonElement.GetString();

    public override int GetInt32() => _jsonElement.GetInt32();

    public override bool GetBoolean() => _jsonElement.GetBoolean();

    public override bool IsEqual(JsonTestObject other) => other is JsonTestElement otherElement && _jsonElement.Equals(otherElement._jsonElement);
}

public class JsonTestNode(JsonNode? jsonNode) : JsonTestObject
{
    private readonly JsonNode? _jsonNode = jsonNode;

    public override JsonValueKind ValueKind => _jsonNode == null ? JsonValueKind.Null : _jsonNode.GetValueKind();

    public override JsonTestObject? GetElement(string pointer) => _jsonNode!.GetNode(pointer) is JsonNode node ? new JsonTestNode(node) : null;

    public override JsonTestObject? GetElementOrNull(string pointer) => _jsonNode?.GetNodeOrNull(pointer) is JsonNode node ? new JsonTestNode(node) : null;

    public override bool TryGetElement(string pointer, out JsonTestObject? result, out JsonPointerError? error)
    {
        var success = _jsonNode!.TryGetNode(pointer, out var node, out var err);
        error = err;
        result = success && node != null ? new JsonTestNode(node) : null;
        return success;
    }

    public override string? GetString() => _jsonNode?.GetValue<string>();

    public override int GetInt32() => _jsonNode!.GetValue<int>();

    public override bool GetBoolean() => _jsonNode!.GetValue<bool>();

    public override bool IsEqual(JsonTestObject other) => other is JsonTestNode otherNode && _jsonNode == otherNode._jsonNode;
}
