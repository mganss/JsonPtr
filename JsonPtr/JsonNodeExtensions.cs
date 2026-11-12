using System.Text.Json.Nodes;

namespace Ganss.Text.Json;

/// <summary>
/// Provides extension methods for working with <see cref="JsonNode"/> using JSON Pointers (RFC 6901).
/// </summary>
/// <remarks>
/// A pointer starting with '/' is resolved from the document root (<see cref="JsonNode.Root"/>),
/// even when called on a nested node. A pointer without a leading '/' is resolved relative to the given node.
/// This differs from <see cref="JsonElementExtensions"/>, where every pointer is resolved relative to the given element
/// because a <see cref="System.Text.Json.JsonElement"/> has no reference to its root.
/// </remarks>
public static class JsonNodeExtensions
{
    /// <summary>
    /// Attempts to retrieve the JSON node at the specified JSON Pointer.
    /// Returns null if the pointer is valid; otherwise, returns an instance of <see cref="JsonPointerNodeError"/> describing the error.
    /// </summary>
    /// <param name="node">The root JSON node.</param>
    /// <param name="pointer">The JSON Pointer string (RFC 6901).</param>
    /// <param name="result">
    /// When the method returns, contains the JSON node referenced by the pointer if successful.
    /// </param>
    /// <returns>
    /// Null if the pointer was successfully resolved; otherwise, a <see cref="JsonPointerNodeError"/> instance describing the error.
    /// </returns>
    private static JsonPointerNodeError? Resolve(JsonNode node, string pointer, out JsonNode? result)
    {
        result = node;

        if (string.IsNullOrEmpty(pointer))
            return null;

        // Empty tokens are significant (RFC 6901: "/" refers to the key ""), so they must not be removed.
        var tokens = pointer.Split('/');
        var isAbsolute = pointer[0] == '/';
        var current = isAbsolute ? node.Root : node;

        // Skip the first token if it is empty because the pointer starts with '/'.
        for (var i = isAbsolute ? 1 : 0; i < tokens.Length; i++)
        {
            var token = JsonPointerParser.Unescape(tokens[i]);

            if (current is JsonObject jsonObject)
            {
                if (!jsonObject.TryGetPropertyValue(token, out JsonNode? property))
                    return new JsonPointerNodeError(
                        JsonPointerErrorKind.PropertyNotFound,
                        $"Property '{token}' not found.",
                        token,
                        jsonObject);
                current = property;
            }
            else if (current is JsonArray jsonArray)
            {
                var indexError = JsonPointerParser.ParseArrayIndex(token, jsonArray.Count, out int index);

                if (indexError == JsonPointerErrorKind.InvalidArrayIndex)
                    return new JsonPointerNodeError(
                        JsonPointerErrorKind.InvalidArrayIndex,
                        $"Invalid array index: {token}.",
                        token,
                        jsonArray);

                if (indexError == JsonPointerErrorKind.ArrayIndexOutOfRange)
                    return new JsonPointerNodeError(
                        JsonPointerErrorKind.ArrayIndexOutOfRange,
                        $"Array index {token} is out of range.",
                        token,
                        jsonArray);

                current = jsonArray[index];
            }
            else
            {
                // A JSON value or a JSON null (represented as a null node) cannot be traversed further.
                return new JsonPointerNodeError(
                    JsonPointerErrorKind.NonTraversableElement,
                    "The current JSON element is not an object or array.",
                    token,
                    current);
            }
        }

        result = current;
        return null;
    }

    /// <summary>
    /// Retrieves the JSON node at the specified JSON Pointer.
    /// Throws an <see cref="JsonPointerException"/> if resolution fails.
    /// </summary>
    /// <param name="node">The root JSON node.</param>
    /// <param name="pointer">The JSON Pointer string (RFC 6901).</param>
    /// <returns>The JSON node referenced by the pointer.</returns>
    /// <exception cref="JsonPointerException">
    /// Thrown when the pointer is invalid or does not resolve to a valid JSON node.
    /// </exception>
    public static JsonNode? GetNode(this JsonNode node, string pointer)
    {
        var error = Resolve(node, pointer, out JsonNode? result);
        if (error != null)
            throw new JsonPointerException(error, $"Error resolving JSON pointer '{pointer}': {error.Message}");
        return result;
    }

    /// <summary>
    /// Retrieves the JSON node at the specified JSON Pointer.
    /// Returns null if the pointer cannot be resolved.
    /// </summary>
    /// <param name="node">The root JSON node.</param>
    /// <param name="pointer">The JSON Pointer string (RFC 6901).</param>
    /// <returns>
    /// The JSON node referenced by the pointer, or null if an error occurred.
    /// </returns>
    public static JsonNode? GetNodeOrNull(this JsonNode node, string pointer)
    {
        var error = Resolve(node, pointer, out JsonNode? result);
        return error == null ? result : null;
    }

    /// <summary>
    /// Attempts to retrieve the JSON node at the specified JSON Pointer.
    /// Returns true if the pointer is successfully resolved; otherwise, returns false and sets the error parameter.
    /// </summary>
    /// <param name="node">The root JSON node.</param>
    /// <param name="pointer">The JSON Pointer string (RFC 6901).</param>
    /// <param name="result">
    /// When the method returns, contains the JSON node referenced by the pointer if successful; otherwise, null.
    /// </param>
    /// <param name="error">
    /// When the method returns, contains a <see cref="JsonPointerNodeError"/> instance describing the error
    /// if the pointer cannot be resolved; otherwise, null.
    /// </param>
    /// <returns>
    /// True if the pointer was successfully resolved; otherwise, false.
    /// </returns>
    public static bool TryGetNode(this JsonNode node, string pointer, out JsonNode? result, out JsonPointerNodeError? error)
    {
        error = Resolve(node, pointer, out var r);
        result = error == null ? r : null;
        return error == null;
    }
}
