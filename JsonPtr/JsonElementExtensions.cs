using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Ganss.Text.Json;

/// <summary>
/// Provides extension methods for working with <see cref="JsonElement"/> using JSON Pointers (RFC 6901).
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Attempts to retrieve the JSON element at the specified JSON Pointer.
    /// Returns null if the pointer is valid; otherwise, returns an instance of <see cref="JsonPointerError"/> describing the error.
    /// </summary>
    /// <param name="element">The root JSON element.</param>
    /// <param name="pointer">The JSON Pointer string (RFC 6901).</param>
    /// <param name="result">
    /// When the method returns, contains the JSON element referenced by the pointer if successful.
    /// </param>
    /// <returns>
    /// Null if the pointer was successfully resolved; otherwise, a <see cref="JsonPointerError"/> instance describing the error.
    /// </returns>
    private static JsonPointerError Resolve(JsonElement element, string pointer, out JsonElement result)
    {
        result = element;

        // An empty pointer returns the root element.
        if (string.IsNullOrEmpty(pointer))
            return null;

        // A valid JSON pointer must start with '/'.
        if (pointer[0] != '/')
            return new JsonPointerError(
                JsonPointerErrorKind.PointerMustStartWithSlash,
                "A JSON pointer must start with a '/'",
                pointer,
                element);

        var tokens = pointer.Split('/');
        var current = element;

        // Process each token (the first token is empty because the pointer starts with '/').
        for (var i = 1; i < tokens.Length; i++)
        {
            // Unescape per RFC 6901: "~1" becomes "/" and "~0" becomes "~".
            var token = tokens[i].Replace("~1", "/").Replace("~0", "~");

            if (current.ValueKind == JsonValueKind.Object)
            {
                if (!current.TryGetProperty(token, out JsonElement property))
                    return new JsonPointerError(
                        JsonPointerErrorKind.PropertyNotFound,
                        $"Property '{token}' not found.",
                        token,
                        current);
                current = property;
            }
            else if (current.ValueKind == JsonValueKind.Array)
            {
                if (!int.TryParse(token, out int index))
                    return new JsonPointerError(
                        JsonPointerErrorKind.InvalidArrayIndex,
                        $"Invalid array index: {token}.",
                        token,
                        current);

                if (index < 0 || index >= current.GetArrayLength())
                    return new JsonPointerError(
                        JsonPointerErrorKind.ArrayIndexOutOfRange,
                        $"Array index {index} is out of range.",
                        token,
                        current);

                current = current[index];
            }
            else
            {
                // If the current element is neither an object nor an array, traversal cannot continue.
                return new JsonPointerError(
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
    /// Retrieves the JSON element at the specified JSON Pointer.
    /// Throws an <see cref="JsonPointerException"/> if resolution fails.
    /// </summary>
    /// <param name="element">The root JSON element.</param>
    /// <param name="pointer">The JSON Pointer string (RFC 6901).</param>
    /// <returns>The JSON element referenced by the pointer.</returns>
    /// <exception cref="JsonPointerException">
    /// Thrown when the pointer is invalid or does not resolve to a valid JSON element.
    /// </exception>
    public static JsonElement GetElement(this JsonElement element, string pointer)
    {
        var error = Resolve(element, pointer, out JsonElement result);
        if (error != null)
            throw new JsonPointerException(error, $"Error resolving JSON pointer '{pointer}': {error.Message}");
        return result;
    }

    /// <summary>
    /// Retrieves the JSON element at the specified JSON Pointer.
    /// Returns null if the pointer cannot be resolved.
    /// </summary>
    /// <param name="element">The root JSON element.</param>
    /// <param name="pointer">The JSON Pointer string (RFC 6901).</param>
    /// <returns>
    /// The JSON element referenced by the pointer, or null if an error occurred.
    /// </returns>
    public static JsonElement? GetElementOrNull(this JsonElement element, string pointer)
    {
        var error = Resolve(element, pointer, out JsonElement result);
        return error == null ? result : null;
    }

    /// <summary>
    /// Attempts to retrieve the JSON element at the specified JSON Pointer.
    /// Returns true if the pointer is successfully resolved; otherwise, returns false and sets the error parameter.
    /// </summary>
    /// <param name="element">The root JSON element.</param>
    /// <param name="pointer">The JSON Pointer string (RFC 6901).</param>
    /// <param name="result">
    /// When the method returns, contains the JSON element referenced by the pointer if successful; otherwise, null.
    /// </param>
    /// <param name="error">
    /// When the method returns, contains a <see cref="JsonPointerError"/> instance describing the error 
    /// if the pointer cannot be resolved; otherwise, null.
    /// </param>
    /// <returns>
    /// True if the pointer was successfully resolved; otherwise, false.
    /// </returns>
    public static bool TryGetElement(this JsonElement element, string pointer, out JsonElement? result,
        out JsonPointerError error)
    {
        error = Resolve(element, pointer, out var r);
        result = error == null ? r : null;
        return error == null;
    }
}
