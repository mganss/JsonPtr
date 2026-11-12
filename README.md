# JsonPtr

[![NuGet version](https://badge.fury.io/nu/JsonPtr.svg)](http://badge.fury.io/nu/JsonPtr)
[![Build status](https://github.com/mganss/JsonPtr/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/mganss/JsonPtr/actions/workflows/build.yml)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=mganss_JsonPtr&metric=coverage)](https://sonarcloud.io/dashboard?id=mganss_JsonPtr)
[![Sonarcloud Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=mganss_JsonPtr&metric=alert_status)](https://sonarcloud.io/dashboard?id=mganss_JsonPtr)
[![netstandard2.0](https://img.shields.io/badge/netstandard-2.0-brightgreen.svg)](https://img.shields.io/badge/netstandard-2.0-brightgreen.svg)
[![net462](https://img.shields.io/badge/net-462-brightgreen.svg)](https://img.shields.io/badge/net-462-brightgreen.svg)

Extension methods for [`System.Text.Json.JsonElement`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement) and [`System.Text.Json.Nodes.JsonNode`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.nodes.jsonnode) to support JSON Pointer ([RFC 6901](https://datatracker.ietf.org/doc/html/rfc6901)) resolution.

## Features

- **JSON Pointer Support**: Resolve JSON elements using RFC 6901-compliant JSON Pointer strings.
- **Flexible APIs**:
  - `GetElement` / `GetNode`: Retrieves a JSON element or node, or throws a `JsonPointerException` on failure.
  - `GetElementOrNull` / `GetNodeOrNull`: Retrieves a JSON element or node, or returns null on failure.
  - `TryGetElement` / `TryGetNode`: Attempts to retrieve a JSON element or node, returning a success flag and error details if applicable.
- Relative pointers (without a leading `/`) are supported, see [Relative and absolute pointers](#relative-and-absolute-pointers).

## Usage

### `GetElement`

Retrieves a JSON element at the specified pointer, throwing an exception if resolution fails.

```csharp
var json = @"{""person"":{""name"":""John""}}";
var element = JsonDocument.Parse(json).RootElement;

try
{
    var nameElement = element.GetElement("/person/name");
    Console.WriteLine(nameElement.GetString()); // Output: John
    var invalidElement = element.GetElement("/person/age");
}
catch (JsonPointerException ex)
{
    Console.WriteLine(ex.Message); // Handles invalid pointers
}
```

### `GetElementOrNull`

Retrieves a JSON element or returns null if the pointer cannot be resolved.

```csharp
var json = @"{""person"":{""name"":""John""}}";
var element = JsonDocument.Parse(json).RootElement;

var name = element.GetElementOrNull("/person/name");
Console.WriteLine(name.Value.GetString()); // Output: John
var age = element.GetElementOrNull("/person/age");
Console.WriteLine(age.HasValue ? age.Value.GetString() : "Not found"); // Output: Not found
```

### `TryGetElement`

Attempts to retrieve a JSON element, returning success status and error details.

```csharp
var json = @"{""person"":{""name"":""John""}}";
var element = JsonDocument.Parse(json).RootElement;

if (element.TryGetElement("/person/name", out JsonElement? result, out JsonPointerElementError? error))
{
    Console.WriteLine(result.Value.GetString()); // Output: John
}
else
{
    Console.WriteLine(error.Message); // Handles failure
}
```

## JSON Pointer Syntax

- `/property` accesses an object property.
- `/0` accesses an array element by index.
- Use `~0` for `~` and `~1` for `/` in escaped keys (e.g., `/foo~1bar` for key `foo/bar`).
- Empty pointer (`""`) refers to the element or node the method is called on.
- Empty keys are significant: `/` refers to the key `""`, and `/a//b` to the key `b` inside the key `""` inside `a`.
- Array indexes must be `0` or a non-negative integer without leading zeros (`01`, `+1` and `-1` are invalid).

### `JsonNode`

The same methods exist for `JsonNode` as `GetNode`, `GetNodeOrNull` and `TryGetNode`.

```csharp
var node = JsonNode.Parse(@"{""person"":{""name"":""John""}}")!;

if (node.TryGetNode("/person/name", out JsonNode? result, out JsonPointerNodeError? error))
{
    Console.WriteLine(result!.GetValue<string>()); // Output: John
}
```

`JsonPointerNodeError.CurrentNode` is null when resolution failed because the pointer tried to traverse a JSON `null` value.

### Relative and absolute pointers

A pointer without a leading `/` is not valid under RFC 6901, but is accepted as a relative pointer:

- **`JsonElement`**: every pointer is resolved relative to the element the method is called on, because a `JsonElement` has no reference to its document root. `/id` and `id` are the same.
- **`JsonNode`**: a pointer starting with `/` is resolved from the document root (`JsonNode.Root`), even when called on a nested node. A pointer without a leading `/` is resolved relative to the node the method is called on.

```csharp
var node = JsonNode.Parse(@"{""data"":{""id"":1}}")!;
var data = node.GetNode("/data")!;
data.GetNode("id");       // 1 (relative to data)
data.GetNode("/data/id"); // 1 (absolute, from the root)
```

## Upgrading from 1.x

- `JsonPointerError` no longer has a `CurrentElement` property. `TryGetElement` returns a `JsonPointerElementError`, which has it.
- `JsonPointerErrorKind.PointerMustStartWithSlash` is obsolete and no longer returned, since relative pointers are accepted.
- Pointers with empty keys (such as `/` or `/a//b`) and array indexes such as `01` or `-1` now behave as RFC 6901 requires.
