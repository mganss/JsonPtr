# JsonPtr

[![NuGet version](https://badge.fury.io/nu/JsonPtr.svg)](http://badge.fury.io/nu/JsonPtr)
[![Build status](https://ci.appveyor.com/api/projects/status/tyyg8905i24qv9pg/branch/master?svg=true)](https://ci.appveyor.com/project/mganss/JsonPtr/branch/master)
[![codecov.io](https://codecov.io/github/mganss/JsonPtr/coverage.svg?branch=master)](https://codecov.io/github/mganss/JsonPtr?branch=master)
[![netstandard2.0](https://img.shields.io/badge/netstandard-2.0-brightgreen.svg)](https://img.shields.io/badge/netstandard-2.0-brightgreen.svg)
[![net462](https://img.shields.io/badge/net-462-brightgreen.svg)](https://img.shields.io/badge/net-462-brightgreen.svg)

This project provides extension methods for the `System.Text.Json.JsonElement` type to support JSON Pointer (RFC 6901) resolution.

## Features

- **JSON Pointer Support**: Resolve JSON elements using RFC 6901-compliant JSON Pointer strings.
- **Flexible APIs**:
  - `GetElement`: Retrieves a JSON element or throws a `JsonPointerException` on failure.
  - `GetElementOrNull`: Retrieves a JSON element or returns null on failure.
  - `TryGetElement`: Attempts to retrieve a JSON element, returning a success flag and error details if applicable.

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
using System.Text.Json;

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
using System.Text.Json;

var json = @"{""person"":{""name"":""John""}}";
var element = JsonDocument.Parse(json).RootElement;

if (element.TryGetElement("/person/name", out JsonElement? result, out JsonPointerError? error))
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
- Empty pointer (`""`) refers to the root element.
