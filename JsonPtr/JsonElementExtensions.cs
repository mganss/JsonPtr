using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Ganss.Text.Json;

/// <summary>
/// Provides extension methods for working with <see cref="JsonElement"/> using JSON Pointers (RFC 6901).
/// </summary>
/// <remarks>
/// Pointers are always resolved relative to the given element, because a <see cref="JsonElement"/> has no reference
/// to its document root. A leading '/' is optional: "/a/b" and "a/b" refer to the same element.
/// This differs from <see cref="JsonNodeExtensions"/>, where a leading '/' resolves from the document root.
/// </remarks>
public static class JsonElementExtensions
{
    /// <summary>
    /// Attempts to retrieve the JSON element at the specified JSON Pointer.
    /// Returns null if the pointer is valid; otherwise, returns an instance of <see cref="JsonPointerElementError"/> describing the error.
    /// </summary>
    /// <param name="element">The root JSON element.</param>
    /// <param name="pointer">The JSON Pointer string (RFC 6901).</param>
    /// <param name="result">
    /// When the method returns, contains the JSON element referenced by the pointer if successful.
    /// </param>
    /// <returns>
    /// Null if the pointer was successfully resolved; otherwise, a <see cref="JsonPointerElementError"/> instance describing the error.
    /// </returns>
    private static JsonPointerElementError? Resolve(JsonElement element, string pointer, out JsonElement result)
    {
        result = element;

        // An empty pointer returns the root element.
        if (string.IsNullOrEmpty(pointer))
            return null;

        // Empty tokens are significant (RFC 6901: "/" refers to the key ""), so they must not be removed.
        var tokens = pointer.Split('/');
        var current = element;

        // Skip the first token if it is empty because the pointer starts with '/'.
        for (var i = pointer[0] == '/' ? 1 : 0; i < tokens.Length; i++)
        {
            var token = JsonPointerParser.Unescape(tokens[i]);

            if (current.ValueKind == JsonValueKind.Object)
            {
                if (!current.TryGetProperty(token, out JsonElement property))
                    return new JsonPointerElementError(
                        JsonPointerErrorKind.PropertyNotFound,
                        $"Property '{token}' not found.",
                        token,
                        current);
                current = property;
            }
            else if (current.ValueKind == JsonValueKind.Array)
            {
                var indexError = JsonPointerParser.ParseArrayIndex(token, current.GetArrayLength(), out int index);

                if (indexError == JsonPointerErrorKind.InvalidArrayIndex)
                    return new JsonPointerElementError(
                        JsonPointerErrorKind.InvalidArrayIndex,
                        $"Invalid array index: {token}.",
                        token,
                        current);

                if (indexError == JsonPointerErrorKind.ArrayIndexOutOfRange)
                    return new JsonPointerElementError(
                        JsonPointerErrorKind.ArrayIndexOutOfRange,
                        $"Array index {token} is out of range.",
                        token,
                        current);

                current = current[index];
            }
            else
            {
                // If the current element is neither an object nor an array, traversal cannot continue.
                return new JsonPointerElementError(
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
    /// When the method returns, contains a <see cref="JsonPointerElementError"/> instance describing the error
    /// if the pointer cannot be resolved; otherwise, null.
    /// </param>
    /// <returns>
    /// True if the pointer was successfully resolved; otherwise, false.
    /// </returns>
    public static bool TryGetElement(this JsonElement element, string pointer, out JsonElement? result,
        out JsonPointerElementError? error)
    {
        error = Resolve(element, pointer, out var r);
        result = error == null ? r : null;
        return error == null;
    }
}
