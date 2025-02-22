using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Ganss.Text.Json;

/// <summary>
/// Exception thrown when a JSON Pointer (RFC 6901) resolution fails.
/// </summary>
public class JsonPointerException(JsonPointerError error, string message) : Exception(message)
{
    /// <summary>
    /// The type of error that occurred during JSON Pointer resolution.
    /// </summary>
    public JsonPointerError Error { get; } = error;
}
