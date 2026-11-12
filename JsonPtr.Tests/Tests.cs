using System.Text.Json;
using System.Text.Json.Nodes;
using Ganss.Text.Json;
using NUnit.Framework;

namespace JsonPtr.Tests;

public abstract class JsonPointerExtensionsTestsBase
{
    protected abstract JsonTestObject ParseJson(string json);
    protected static JsonValueKind GetValueKind(JsonTestObject? element) => element == null ? JsonValueKind.Null : element.ValueKind;

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

        // RFC 6901 section 5 examples
        const string rfcDocument = @"{""foo"":[""bar"",""baz""],"""":0,""a/b"":1,""c%d"":2,""e^f"":3,""g|h"":4,""i\\j"":5,""k\""l"":6,"" "":7,""m~n"":8}";
        yield return new object?[] { rfcDocument, "/foo/0", JsonValueKind.String, "bar" };
        yield return new object?[] { rfcDocument, "/", JsonValueKind.Number, 0 };
        yield return new object?[] { rfcDocument, "/a~1b", JsonValueKind.Number, 1 };
        yield return new object?[] { rfcDocument, "/c%d", JsonValueKind.Number, 2 };
        yield return new object?[] { rfcDocument, "/e^f", JsonValueKind.Number, 3 };
        yield return new object?[] { rfcDocument, "/g|h", JsonValueKind.Number, 4 };
        yield return new object?[] { rfcDocument, @"/i\j", JsonValueKind.Number, 5 };
        yield return new object?[] { rfcDocument, @"/k""l", JsonValueKind.Number, 6 };
        yield return new object?[] { rfcDocument, "/ ", JsonValueKind.Number, 7 };
        yield return new object?[] { rfcDocument, "/m~0n", JsonValueKind.Number, 8 };

        // Empty keys in the middle and at the end of a pointer
        yield return new object?[] { @"{""a"":{"""":{""b"":1}}}", "/a//b", JsonValueKind.Number, 1 };
        yield return new object?[] { @"{""a"":{"""":2}}", "/a/", JsonValueKind.Number, 2 };
        yield return new object?[] { @"{"""":{"""":3}}", "//", JsonValueKind.Number, 3 };

        // Multi-digit array index
        yield return new object?[] { @"[0,1,2,3,4,5,6,7,8,9,10]", "/10", JsonValueKind.Number, 10 };
    }

    private static IEnumerable<object?[]> InvalidPointerTestData()
    {
        // Non-existent properties
        yield return new object?[] { @"{""name"":""John""}", "/age", "Property 'age' not found" };
        yield return new object?[] { @"{""person"":{""name"":""John""}}", "/person/age", "Property 'age' not found" };

        // Out-of-range array indices
        yield return new object?[] { @"[1,2,3]", "/3", "Array index 3 is out of range." };
        yield return new object?[] { @"[]", "/0", "Array index 0 is out of range." };
        yield return new object?[] { @"[1,2,3]", "/99999999999", "Array index 99999999999 is out of range." };

        // Invalid navigation
        yield return new object?[] { @"{""data"":""hello""}", "/data/foo", "The current JSON element is not an object or array." };
        yield return new object?[] { @"{""data"":[{""id"":1}]}", "/data/id", "Invalid array index: id." };
        yield return new object?[] { @"{""data"":{""id"":1}}", "/data/0", "Property '0' not found." };

        // Invalid array indices
        yield return new object?[] { @"[1,2,3]", "/a", "Invalid array index: a." };
        yield return new object?[] { @"[1,2,3]", "/-", "Invalid array index: -." };
        yield return new object?[] { @"[1,2,3]", "/-1", "Invalid array index: -1." };
        yield return new object?[] { @"[1,2,3]", "/01", "Invalid array index: 01." };
        yield return new object?[] { @"[1,2,3]", "/+1", "Invalid array index: +1." };
        yield return new object?[] { @"[1,2,3]", "/ 1", "Invalid array index:  1." };
        yield return new object?[] { @"[1,2,3]", "/1.0", "Invalid array index: 1.0." };
        yield return new object?[] { @"[1,2,3]", "/", "Invalid array index: ." };

        // Traversing a JSON null
        yield return new object?[] { @"{""a"":null}", "/a/b", "The current JSON element is not an object or array." };

        // Unescaped pointer does not match
        yield return new object?[] { @"{""foo/bar"":""baz""}", "/foo/bar", "Property 'foo' not found" };

        // Empty tokens are not skipped
        yield return new object?[] { @"{""a"":{""b"":1}}", "/a//b", "Property '' not found" };
        yield return new object?[] { @"{""a"":{""b"":1}}", "/a/", "Property '' not found" };
        yield return new object?[] { @"{""a"":1}", "/", "Property '' not found" };
    }

    [Test]
    [TestCaseSource(nameof(ValidPointerTestData))]
    public void GetElement_ValidPointer_ReturnsCorrectValue(string json, string pointer, JsonValueKind expectedKind, object expectedValue)
    {
        var element = ParseJson(json);
        var result = element.GetElement(pointer);

        Assert.That(GetValueKind(result), Is.EqualTo(expectedKind));
        if (expectedValue == null)
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.Null));
        }
        else
        {
            Assert.That(result, Is.Not.Null);

            if (expectedValue is string s)
            {
                Assert.That(result.GetString(), Is.EqualTo(s));
            }
            else if (expectedValue is int i)
            {
                Assert.That(result.GetInt32(), Is.EqualTo(i));
            }
            else if (expectedValue is bool b)
            {
                Assert.That(GetValueKind(result), Is.EqualTo(b ? JsonValueKind.True : JsonValueKind.False));
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(InvalidPointerTestData))]
    public void GetElement_InvalidPointer_ThrowsJsonPointerException(string json, string pointer, string expectedErrorMessage)
    {
        var element = ParseJson(json);
        var ex = Assert.Throws<JsonPointerException>(() => element.GetElement(pointer));
        Assert.That(ex.Message, Does.Contain(expectedErrorMessage));
    }

    [Test]
    public void GetElement_EmptyPointer_ReturnsRootElement()
    {
        var json = @"{""name"":""John""}";
        var element = ParseJson(json);
        var result = element.GetElement("");
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsEqual(element));
    }

    [Test]
    public virtual void GetElement_CaseSensitiveKey_ThrowsException()
    {
        var json = @"{""Name"":""John""}";
        var element = ParseJson(json);
        var ex = Assert.Throws<JsonPointerException>(() => element.GetElement("/name"));
        Assert.That(ex.Message, Does.Contain("Property 'name' not found"));
    }

    [Test]
    [TestCaseSource(nameof(ValidPointerTestData))]
    public void GetElementOrNull_ValidPointer_ReturnsCorrectValue(string json, string pointer, JsonValueKind expectedKind, object expectedValue)
    {
        var element = ParseJson(json);
        var result = element.GetElementOrNull(pointer);

        Assert.That(GetValueKind(result!), Is.EqualTo(expectedKind));

        if (expectedValue == null)
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.Null));
        }
        else
        {
            Assert.That(result, Is.Not.Null);

            if (expectedValue is string s)
            {
                Assert.That(result.GetString(), Is.EqualTo(s));
            }
            else if (expectedValue is int i)
            {
                Assert.That(result.GetInt32(), Is.EqualTo(i));
            }
            else if (expectedValue is bool b)
            {
                Assert.That(GetValueKind(result), Is.EqualTo(b ? JsonValueKind.True : JsonValueKind.False));
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(InvalidPointerTestData))]
    public void GetElementOrNull_InvalidPointer_ReturnsNull(string json, string pointer, string _)
    {
        var element = ParseJson(json);
        var result = element.GetElementOrNull(pointer);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetElementOrNull_EmptyPointer_ReturnsRootElement()
    {
        var json = @"{""name"":""John""}";
        var element = ParseJson(json);
        var result = element.GetElementOrNull("");
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsEqual(element));
    }

    [Test]
    [TestCaseSource(nameof(ValidPointerTestData))]
    public void TryGetElement_ValidPointer_Succeeds(string json, string pointer, JsonValueKind expectedKind, object expectedValue)
    {
        var element = ParseJson(json);
        var success = element.TryGetElement(pointer, out var result, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
        });
        Assert.That(GetValueKind(result), Is.EqualTo(expectedKind));
        if (expectedValue == null)
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.Null));
        }
        else
        {
            Assert.That(result, Is.Not.Null);

            if (expectedValue is string s)
            {
                Assert.That(result.GetString(), Is.EqualTo(s));
            }
            else if (expectedValue is int i)
            {
                Assert.That(result.GetInt32(), Is.EqualTo(i));
            }
            else if (expectedValue is bool b)
            {
                Assert.That(GetValueKind(result), Is.EqualTo(b ? JsonValueKind.True : JsonValueKind.False));
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(InvalidPointerTestData))]
    public void TryGetElement_InvalidPointer_FailsWithError(string json, string pointer, string expectedErrorMessage)
    {
        var element = ParseJson(json);
        var success = element.TryGetElement(pointer, out var result, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Is.Not.Null);
        });
        Assert.That(error!.Message, Does.Contain(expectedErrorMessage));
    }

    [Test]
    public void TryGetElement_EmptyPointer_SucceedsWithRootElement()
    {
        var json = @"{""name"":""John""}";
        var element = ParseJson(json);
        var success = element.TryGetElement("", out var result, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.IsEqual(element));
            Assert.That(error, Is.Null);
        });
    }

    [Test]
    public void GetElement_NestedArrayAndObject_ReturnsCorrectValue()
    {
        var json = @"{""data"":[{""id"":1},{""id"":2}]}";
        var element = ParseJson(json);
        var result = element.GetElement("/data/1/id");
        Assert.Multiple(() =>
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.Number));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetInt32(), Is.EqualTo(2));
        });
    }

    [Test]
    public void GetElement_InvalidNavigationThroughArray_ThrowsException()
    {
        var json = @"{""data"":[{""id"":1}]}";
        var element = ParseJson(json);
        var ex = Assert.Throws<JsonPointerException>(() => element.GetElement("/data/id"));
        Assert.That(ex.Message, Does.Contain("Error resolving JSON pointer '/data/id': Invalid array index: id."));
    }

    [Test]
    public void GetElement_InvalidArrayIndexOnObject_ThrowsException()
    {
        var json = @"{""data"":{""id"":1}}";
        var element = ParseJson(json);
        var ex = Assert.Throws<JsonPointerException>(() => element.GetElement("/data/0"));
        Assert.That(ex.Message, Does.Contain("Error resolving JSON pointer '/data/0': Property '0' not found."));
    }

    [Test]
    public void GetElement_PointerWithEscapedCharacters_ReturnsCorrectValue()
    {
        var json = @"{""foo/bar"":{""baz~1qux"":""value""}}";
        var element = ParseJson(json);
        var result = element.GetElement("/foo~1bar/baz~01qux");
        Assert.Multiple(() =>
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.String));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetString(), Is.EqualTo("value"));
        });
    }

    [Test]
    public void GetElement_RelativeResolution_ReturnsCorrectValue()
    {
        var json = @"{""data"":{""nested"":{""id"":1}}}";
        var element = ParseJson(json);
        var nestedElement = element.GetElement("/data/nested");
        Assert.That(nestedElement, Is.Not.Null);
        var result = nestedElement.GetElement("id");
        Assert.Multiple(() =>
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.Number));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetInt32(), Is.EqualTo(1));
        });
    }
}

[TestFixture]
public class JsonElementExtensionsTests : JsonPointerExtensionsTestsBase
{
    protected override JsonTestObject ParseJson(string json) => new JsonTestElement(JsonDocument.Parse(json).RootElement);

    [Test]
    public void TryGetElement_InvalidIndex_ErrorHasKindAndCurrentElement()
    {
        var element = JsonDocument.Parse(@"{""a"":[1,2]}").RootElement;
        element.TryGetElement("/a/01", out _, out var error);

        Assert.That(error, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(error!.ErrorType, Is.EqualTo(JsonPointerErrorKind.InvalidArrayIndex));
            Assert.That(error.CurrentElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
        });
    }

    [Test]
    public void JsonPointerErrorKind_NumericValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)JsonPointerErrorKind.PropertyNotFound, Is.EqualTo(1));
            Assert.That((int)JsonPointerErrorKind.InvalidArrayIndex, Is.EqualTo(2));
            Assert.That((int)JsonPointerErrorKind.ArrayIndexOutOfRange, Is.EqualTo(3));
            Assert.That((int)JsonPointerErrorKind.NonTraversableElement, Is.EqualTo(4));
        });
    }

    [Test]
    public void GetElement_AbsoluteResolution_ReturnsCorrectValue()
    {
        var json = @"{""data"":{""nested"":{""id"":1}}}";
        var element = ParseJson(json);
        var nestedElement = element.GetElement("/data/nested");
        Assert.That(nestedElement, Is.Not.Null);
        var result = nestedElement.GetElement("/id");
        Assert.Multiple(() =>
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.Number));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetInt32(), Is.EqualTo(1));
        });
        result = nestedElement.GetElement("id");
        Assert.Multiple(() =>
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.Number));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetInt32(), Is.EqualTo(1));
        });
    }
}

[TestFixture]
public class JsonNodeExtensionsTests : JsonPointerExtensionsTestsBase
{
    protected override JsonTestObject ParseJson(string json) => new JsonTestNode(JsonNode.Parse(json)!);

    [Test]
    public void TryGetNode_TraverseNull_ErrorHasNullCurrentNode()
    {
        var node = JsonNode.Parse(@"{""a"":null}")!;
        var success = node.TryGetNode("/a/b", out var result, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.ErrorType, Is.EqualTo(JsonPointerErrorKind.NonTraversableElement));
            Assert.That(error.Token, Is.EqualTo("b"));
            Assert.That(error.CurrentNode, Is.Null);
        });
    }

    [Test]
    public void TryGetNode_PropertyNotFound_ErrorHasCurrentNode()
    {
        var node = JsonNode.Parse(@"{""a"":{""b"":1}}")!;
        node.TryGetNode("/a/c", out _, out var error);

        Assert.That(error, Is.Not.Null);
        Assert.That(error!.CurrentNode, Is.SameAs(node["a"]));
    }

    [Test]
    public void GetElement_AbsoluteResolution_ReturnsCorrectValue()
    {
        var json = @"{""data"":{""nested"":{""id"":1}}}";
        var element = ParseJson(json);
        var nestedElement = element.GetElement("/data/nested");
        Assert.That(nestedElement, Is.Not.Null);
        var result = nestedElement.GetElement("/data/nested/id");
        Assert.Multiple(() =>
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.Number));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetInt32(), Is.EqualTo(1));
        });
    }
}

[TestFixture]
public class JsonNodeExtensionsCaseInsensitiveTests : JsonPointerExtensionsTestsBase
{
    protected override JsonTestObject ParseJson(string json)
    {
        var options = new JsonNodeOptions { PropertyNameCaseInsensitive = true };
        return new JsonTestNode(JsonNode.Parse(json, options)!);
    }

    [Test]
    public void GetElement_CaseInsensitiveKey_ReturnsCorrectValue()
    {
        var json = @"{""Name"":""John""}";
        var element = ParseJson(json);
        var result = element.GetElement("/name");
        Assert.Multiple(() =>
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.String));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetString(), Is.EqualTo("John"));
        });
    }

    [Test]
    public void GetElementOrNull_CaseInsensitiveKey_ReturnsCorrectValue()
    {
        var json = @"{""Name"":""John""}";
        var element = ParseJson(json);
        var result = element.GetElementOrNull("/name");
        Assert.Multiple(() =>
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.String));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetString(), Is.EqualTo("John"));
        });
    }

    [Test]
    public void TryGetElement_CaseInsensitiveKey_Succeeds()
    {
        var json = @"{""Name"":""John""}";
        var element = ParseJson(json);
        var success = element.TryGetElement("/name", out var result, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.String));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetString(), Is.EqualTo("John"));
        });
    }

    [Test]
    public override void GetElement_CaseSensitiveKey_ThrowsException()
    {
        var json = @"{""Name"":""John""}";
        var element = ParseJson(json);
        var result = element.GetElement("/name");
        Assert.Multiple(() =>
        {
            Assert.That(GetValueKind(result), Is.EqualTo(JsonValueKind.String));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetString(), Is.EqualTo("John"));
        });
    }
}
