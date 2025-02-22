using System.Text.Json;
using Ganss.Text.Json;
using NUnit.Framework;

namespace JsonPtr.Tests;

[TestFixture]
public class JsonElementExtensionsTests
{
    private static JsonElement CreateJsonElement(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }

    private static IEnumerable<object?[]> ValidPointerTestData()
    {
        // Simple object
        yield return new object?[] { @"{""name"":""John""}", "/name", JsonValueKind.String, "John" };
        yield return new object?[] { @"{""age"":30}", "/age", JsonValueKind.Number, 30 };
        yield return new object?[] { @"{""isStudent"":true}", "/isStudent", JsonValueKind.True, true };
        yield return new object?[] { @"{""data"":null}", "/data", JsonValueKind.Null, null };

        // Array
        yield return new object?[] { @"[1,2,3]", "/0", JsonValueKind.Number, 1 };
        yield return new object?[] { @"[""a"",""b""]", "/1", JsonValueKind.String, "b" };

        // Nested structures
        yield return new object?[] { @"{""person"":{""name"":""John""}}", "/person/name", JsonValueKind.String, "John" };
        yield return new object?[] { @"{""array"":[{""id"":1},{""id"":2}]}", "/array/1/id", JsonValueKind.Number, 2 };

        // Pointers with escaped characters
        yield return new object?[] { @"{""foo/bar"":""baz""}", "/foo~1bar", JsonValueKind.String, "baz" };
        yield return new object?[] { @"{""~foo"":""tilde""}", "/~0foo", JsonValueKind.String, "tilde" };

        // Object with numeric key
        yield return new object?[] { @"{""0"":""zero""}", "/0", JsonValueKind.String, "zero" };
    }

    private static IEnumerable<object?[]> InvalidPointerTestData()
    {
        // Non-existent properties
        yield return new object?[] { @"{""name"":""John""}", "/age", "Property 'age' not found" };
        yield return new object?[] { @"{""person"":{""name"":""John""}}", "/person/age", "Property 'age' not found" };

        // Out-of-range array indices
        yield return new object?[] { @"[1,2,3]", "/3", "Array index 3 is out of range." };
        yield return new object?[] { @"[]", "/0", "Array index 0 is out of range." };

        // Invalid navigation
        yield return new object?[] { @"{""data"":""hello""}", "/data/foo", "The current JSON element is not an object or array." };
        yield return new object?[] { @"{""data"":[{""id"":1}]}", "/data/id", "Invalid array index: id." };
        yield return new object?[] { @"{""data"":{""id"":1}}", "/data/0", "Property '0' not found." };

        // Invalid array indices
        yield return new object?[] { @"[1,2,3]", "/a", "Invalid array index: a." };
        yield return new object?[] { @"[1,2,3]", "/-", "Invalid array index: -." };

        // Unescaped pointer does not match
        yield return new object?[] { @"{""foo/bar"":""baz""}", "/foo/bar", "Property 'foo' not found" };
    }

    [Test]
    [TestCaseSource(nameof(ValidPointerTestData))]
    public void GetElement_ValidPointer_ReturnsCorrectValue(string json, string pointer, JsonValueKind expectedKind, object expectedValue)
    {
        var element = CreateJsonElement(json);
        var result = element.GetElement(pointer);

        Assert.That(result.ValueKind, Is.EqualTo(expectedKind));
        if (expectedValue == null)
        {
            Assert.That(result.ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
        else if (expectedValue is string s)
        {
            Assert.That(result.GetString(), Is.EqualTo(s));
        }
        else if (expectedValue is int i)
        {
            Assert.That(result.GetInt32(), Is.EqualTo(i));
        }
        else if (expectedValue is bool b)
        {
            Assert.That(result.ValueKind, Is.EqualTo(b ? JsonValueKind.True : JsonValueKind.False));
        }
    }

    [Test]
    [TestCaseSource(nameof(InvalidPointerTestData))]
    public void GetElement_InvalidPointer_ThrowsJsonPointerException(string json, string pointer, string expectedErrorMessage)
    {
        var element = CreateJsonElement(json);
        var ex = Assert.Throws<JsonPointerException>(() => element.GetElement(pointer));
        Assert.That(ex.Message, Does.Contain(expectedErrorMessage));
    }

    [Test]
    public void GetElement_EmptyPointer_ReturnsRootElement()
    {
        var json = @"{""name"":""John""}";
        var element = CreateJsonElement(json);
        var result = element.GetElement("");
        Assert.That(result, Is.EqualTo(element));
    }

    [Test]
    public void GetElement_CaseSensitiveKey_ThrowsException()
    {
        var json = @"{""Name"":""John""}";
        var element = CreateJsonElement(json);
        var ex = Assert.Throws<JsonPointerException>(() => element.GetElement("/name"));
        Assert.That(ex.Message, Does.Contain("Property 'name' not found"));
    }

    [Test]
    [TestCaseSource(nameof(ValidPointerTestData))]
    public void GetElementOrNull_ValidPointer_ReturnsCorrectValue(string json, string pointer, JsonValueKind expectedKind, object expectedValue)
    {
        var element = CreateJsonElement(json);
        var result = element.GetElementOrNull(pointer);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value.ValueKind, Is.EqualTo(expectedKind));
        if (expectedValue == null)
        {
            Assert.That(result.Value.ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
        else if (expectedValue is string s)
        {
            Assert.That(result.Value.GetString(), Is.EqualTo(s));
        }
        else if (expectedValue is int i)
        {
            Assert.That(result.Value.GetInt32(), Is.EqualTo(i));
        }
        else if (expectedValue is bool b)
        {
            Assert.That(result.Value.ValueKind, Is.EqualTo(b ? JsonValueKind.True : JsonValueKind.False));
        }
    }

    [Test]
    [TestCaseSource(nameof(InvalidPointerTestData))]
    public void GetElementOrNull_InvalidPointer_ReturnsNull(string json, string pointer, string _)
    {
        var element = CreateJsonElement(json);
        var result = element.GetElementOrNull(pointer);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetElementOrNull_EmptyPointer_ReturnsRootElement()
    {
        var json = @"{""name"":""John""}";
        var element = CreateJsonElement(json);
        var result = element.GetElementOrNull("");
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(element));
    }

    [Test]
    [TestCaseSource(nameof(ValidPointerTestData))]
    public void TryGetElement_ValidPointer_Succeeds(string json, string pointer, JsonValueKind expectedKind, object expectedValue)
    {
        var element = CreateJsonElement(json);
        var success = element.TryGetElement(pointer, out var result, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(error, Is.Null);
        });
        Assert.That(result.Value.ValueKind, Is.EqualTo(expectedKind));
        if (expectedValue == null)
        {
            Assert.That(result.Value.ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
        else if (expectedValue is string s)
        {
            Assert.That(result.Value.GetString(), Is.EqualTo(s));
        }
        else if (expectedValue is int i)
        {
            Assert.That(result.Value.GetInt32(), Is.EqualTo(i));
        }
        else if (expectedValue is bool b)
        {
            Assert.That(result.Value.ValueKind, Is.EqualTo(b ? JsonValueKind.True : JsonValueKind.False));
        }
    }

    [Test]
    [TestCaseSource(nameof(InvalidPointerTestData))]
    public void TryGetElement_InvalidPointer_FailsWithError(string json, string pointer, string expectedErrorMessage)
    {
        var element = CreateJsonElement(json);
        var success = element.TryGetElement(pointer, out var result, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Is.Not.Null);
        });
        Assert.That(error.Message, Does.Contain(expectedErrorMessage));
    }

    [Test]
    public void TryGetElement_EmptyPointer_SucceedsWithRootElement()
    {
        var json = @"{""name"":""John""}";
        var element = CreateJsonElement(json);
        var success = element.TryGetElement("", out var result, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(element));
            Assert.That(error, Is.Null);
        });
    }

    [Test]
    public void GetElement_NestedArrayAndObject_ReturnsCorrectValue()
    {
        var json = @"{""data"":[{""id"":1},{""id"":2}]}";
        var element = CreateJsonElement(json);
        var result = element.GetElement("/data/1/id");
        Assert.Multiple(() =>
        {
            Assert.That(result.ValueKind, Is.EqualTo(JsonValueKind.Number));
            Assert.That(result.GetInt32(), Is.EqualTo(2));
        });
    }

    [Test]
    public void GetElement_InvalidNavigationThroughArray_ThrowsException()
    {
        var json = @"{""data"":[{""id"":1}]}";
        var element = CreateJsonElement(json);
        var ex = Assert.Throws<JsonPointerException>(() => element.GetElement("/data/id"));
        Assert.That(ex.Message, Does.Contain("Error resolving JSON pointer '/data/id': Invalid array index: id."));
    }

    [Test]
    public void GetElement_InvalidArrayIndexOnObject_ThrowsException()
    {
        var json = @"{""data"":{""id"":1}}";
        var element = CreateJsonElement(json);
        var ex = Assert.Throws<JsonPointerException>(() => element.GetElement("/data/0"));
        Assert.That(ex.Message, Does.Contain("Error resolving JSON pointer '/data/0': Property '0' not found."));
    }

    [Test]
    public void GetElement_PointerWithEscapedCharacters_ReturnsCorrectValue()
    {
        var json = @"{""foo/bar"":{""baz~1qux"":""value""}}";
        var element = CreateJsonElement(json);
        var result = element.GetElement("/foo~1bar/baz~01qux");
        Assert.Multiple(() =>
        {
            Assert.That(result.ValueKind, Is.EqualTo(JsonValueKind.String));
            Assert.That(result.GetString(), Is.EqualTo("value"));
        });
    }
}
