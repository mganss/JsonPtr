using System.Text.Json;

namespace Ganss.Text.Json;

/// <summary>
/// Specifies the type of error that occurred during JSON Pointer resolution.
/// </summary>
public enum JsonPointerErrorKind
{
    /// <summary>
    /// Indicates that the JSON pointer must start with a '/' character.
    /// </summary>
    PointerMustStartWithSlash,

    /// <summary>
    /// Indicates that the specified property was not found in the JSON object.
    /// </summary>
    PropertyNotFound,

    /// <summary>
    /// Indicates that the specified array index is invalid (not an integer).
    /// </summary>
    InvalidArrayIndex,

    /// <summary>
    /// Indicates that the specified array index is out of range.
    /// </summary>
    ArrayIndexOutOfRange,

    /// <summary>
    /// Indicates that the current JSON element is not an object or array, and thus cannot be traversed further.
    /// </summary>
    NonTraversableElement
}

/// <summary>
/// Represents an error that occurred during JSON Pointer resolution.
/// </summary>
public class JsonPointerError(JsonPointerErrorKind errorType, string message, string token, JsonElement currentElement)
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

    /// <summary>
    /// The current JSON element at the time the error occurred.
    /// </summary>
    public JsonElement CurrentElement { get; } = currentElement;
}
