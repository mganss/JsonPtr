using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ganss.Text.Json;

/// <summary>
/// Specifies the type of error that occurred during JSON Pointer resolution.
/// </summary>
public enum JsonPointerErrorKind
{
    /// <summary>
    /// Indicates that the JSON pointer must start with a '/' character.
    /// </summary>
    /// <remarks>
    /// No longer returned: pointers without a leading '/' are resolved relative to the given element or node.
    /// Kept so that the numeric values of the other members do not change.
    /// </remarks>
    [Obsolete("Pointers without a leading '/' are now resolved relative to the given element or node. This value is no longer returned.")]
    PointerMustStartWithSlash = 0,

    /// <summary>
    /// Indicates that the specified property was not found in the JSON object.
    /// </summary>
    PropertyNotFound = 1,

    /// <summary>
    /// Indicates that the specified array index is invalid, i.e. not "0" or a non-negative integer without leading zeros.
    /// </summary>
    InvalidArrayIndex = 2,

    /// <summary>
    /// Indicates that the specified array index is out of range.
    /// </summary>
    ArrayIndexOutOfRange = 3,

    /// <summary>
    /// Indicates that the current JSON element is not an object or array, and thus cannot be traversed further.
    /// </summary>
    NonTraversableElement = 4
}

/// <summary>
/// Represents an error that occurred during JSON Pointer resolution.
/// </summary>
/// <param name="errorType">The kind of error that occurred.</param>
/// <param name="message">A human-readable error message.</param>
/// <param name="token">The JSON Pointer token that caused the error, if applicable.</param>
public class JsonPointerError(JsonPointerErrorKind errorType, string message, string token)
{
    /// <summary>
    /// The kind of error that occurred.
    /// </summary>
    public JsonPointerErrorKind ErrorType { get; } = errorType;

    /// <summary>
    /// A human-readable error message.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// The JSON Pointer token that caused the error, if applicable.
    /// </summary>
    public string Token { get; } = token;
}

/// <summary>
/// Represents an error that occurred during JSON Pointer resolution, including the current JSON element.
/// </summary>
/// <param name="errorType">The kind of error that occurred.</param>
/// <param name="message">A human-readable error message.</param>
/// <param name="token">The JSON Pointer token that caused the error, if applicable.</param>
/// <param name="currentElement">The current JSON element at the time the error occurred.</param>
public class JsonPointerElementError(JsonPointerErrorKind errorType, string message, string token, JsonElement currentElement)
    : JsonPointerError(errorType, message, token)
{
    /// <summary>
    /// The current JSON element at the time the error occurred.
    /// </summary>
    public JsonElement CurrentElement { get; } = currentElement;
}

/// <summary>
/// Represents an error that occurred during JSON Pointer resolution, including the current JSON node.
/// </summary>
/// <param name="errorType">The kind of error that occurred.</param>
/// <param name="message">A human-readable error message.</param>
/// <param name="token">The JSON Pointer token that caused the error, if applicable.</param>
/// <param name="currentNode">The current JSON node at the time the error occurred.</param>
public class JsonPointerNodeError(JsonPointerErrorKind errorType, string message, string token, JsonNode? currentNode)
    : JsonPointerError(errorType, message, token)
{
    /// <summary>
    /// The current JSON node at the time the error occurred.
    /// Null if the pointer tried to traverse a JSON <c>null</c> value.
    /// </summary>
    public JsonNode? CurrentNode { get; } = currentNode;
}

/// <summary>
/// Shared helpers for JSON Pointer resolution.
/// </summary>
internal static class JsonPointerParser
{
    /// <summary>
    /// Unescapes a reference token per RFC 6901: "~1" becomes "/", then "~0" becomes "~".
    /// </summary>
    public static string Unescape(string token) => token.Replace("~1", "/").Replace("~0", "~");

    /// <summary>
    /// Parses an array index per RFC 6901 ("0" or a non-negative integer without leading zeros).
    /// </summary>
    /// <param name="token">The unescaped reference token.</param>
    /// <param name="length">The length of the array being indexed.</param>
    /// <param name="index">When successful, the parsed index.</param>
    /// <returns>Null on success; otherwise the kind of error.</returns>
    public static JsonPointerErrorKind? ParseArrayIndex(string token, int length, out int index)
    {
        index = -1;

        if (token.Length == 0 || (token.Length > 1 && token[0] == '0'))
            return JsonPointerErrorKind.InvalidArrayIndex;

        foreach (var c in token)
        {
            if (c < '0' || c > '9')
                return JsonPointerErrorKind.InvalidArrayIndex;
        }

        // All digits: anything that doesn't fit into an int is necessarily out of range.
        if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out index) || index >= length)
            return JsonPointerErrorKind.ArrayIndexOutOfRange;

        return null;
    }
}
