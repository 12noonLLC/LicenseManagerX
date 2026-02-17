using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared;
using System;

namespace LicenseManagerX.UnitTests;

[TestClass]
public class MultilineTextToDictionaryTests
{
	[TestMethod]
	public void ConvertTextToDictionary_NullInput_ReturnsEmptyDictionary()
	{
		// Arrange
		string? text = null;

		// Act
		Dictionary<string, string> result = MultilineTextToDictionary.ConvertTextToDictionary(text!);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsEmpty(result);
	}

	[TestMethod]
	public void ConvertTextToDictionary_EmptyInput_ReturnsEmptyDictionary()
	{
		// Arrange
		string text = string.Empty;

		// Act
		Dictionary<string, string> result = MultilineTextToDictionary.ConvertTextToDictionary(text);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsEmpty(result);
	}

	[TestMethod]
	public void ConvertTextToDictionary_WhitespaceInput_ReturnsEmptyDictionary()
	{
		// Arrange
		string text = "   \t   \r\n   ";

		// Act
		Dictionary<string, string> result = MultilineTextToDictionary.ConvertTextToDictionary(text);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsEmpty(result);
	}

	[TestMethod]
	public void ConvertTextToDictionary_SingleKeyValue_ReturnsDictionaryWithOneEntry()
	{
		// Arrange
		string text = "key=value";

		// Act
		Dictionary<string, string> result = MultilineTextToDictionary.ConvertTextToDictionary(text);

		// Assert
		Assert.IsNotNull(result);
		Assert.HasCount(1, result);
		Assert.AreEqual("value", result["key"]);
	}

	[TestMethod]
	public void ConvertTextToDictionary_MultipleKeyValues_ReturnsDictionaryWithMultipleEntries()
	{
		// Arrange
		string text = "key1=value1" + Environment.NewLine + "key2=value2" + Environment.NewLine + "key3=value3";

		// Act
		Dictionary<string, string> result = MultilineTextToDictionary.ConvertTextToDictionary(text);

		// Assert
		Assert.IsNotNull(result);
		Assert.HasCount(3, result);
		Assert.AreEqual("value1", result["key1"]);
		Assert.AreEqual("value2", result["key2"]);
		Assert.AreEqual("value3", result["key3"]);
	}

	[TestMethod]
	public void ConvertTextToDictionary_EmptyValues_ReturnsDictionaryWithEmptyValues()
	{
		// Arrange
		string text = "key1=" + Environment.NewLine + "key2=";

		// Act
		Dictionary<string, string> result = MultilineTextToDictionary.ConvertTextToDictionary(text);

		// Assert
		Assert.IsNotNull(result);
		Assert.HasCount(2, result);
		Assert.AreEqual(string.Empty, result["key1"]);
		Assert.AreEqual(string.Empty, result["key2"]);
	}

	[TestMethod]
	public void ConvertTextToDictionary_TrimmedKeyValues_ReturnsTrimmedEntries()
	{
		// Arrange
		string text = "   key1   =   value1   " + Environment.NewLine + "   key2   =   value2   ";

		// Act
		Dictionary<string, string> result = MultilineTextToDictionary.ConvertTextToDictionary(text);

		// Assert
		Assert.IsNotNull(result);
		Assert.HasCount(2, result);
		Assert.AreEqual("value1", result["key1"]);
		Assert.AreEqual("value2", result["key2"]);
	}

	[TestMethod]
	public void ConvertTextToDictionary_ValuesWithEqualSign_ReturnsDictionaryWithCorrectValues()
	{
		// Arrange
		string text = "key1=value=with=equals" + Environment.NewLine + "key2=another=value";

		// Act
		Dictionary<string, string> result = MultilineTextToDictionary.ConvertTextToDictionary(text);

		// Assert
		Assert.IsNotNull(result);
		Assert.HasCount(2, result);
		Assert.AreEqual("value=with=equals", result["key1"]);
		Assert.AreEqual("another=value", result["key2"]);
	}

	[TestMethod]
	public void ConvertTextToDictionary_InvalidLines_SkipsInvalidLines()
	{
		// Arrange
		string text = "key1=value1" + Environment.NewLine +
							"no value" + Environment.NewLine +
							"key2=value2" + Environment.NewLine +
							"=nokey";

		// Act
		Dictionary<string, string> result = MultilineTextToDictionary.ConvertTextToDictionary(text);

		// Assert
		Assert.IsNotNull(result);
		Assert.HasCount(3, result);
		Assert.AreEqual("value1", result["key1"]);
		Assert.AreEqual("value2", result["key2"]);
		Assert.IsFalse(result.ContainsKey(string.Empty));
	}

	[TestMethod]
	public void ConvertTextToDictionary_DuplicateKeys_LastValueWins()
	{
		// Arrange
		string text = "key=value1" + Environment.NewLine + "key=value2";

		// Act
		Dictionary<string, string> result = MultilineTextToDictionary.ConvertTextToDictionary(text);

		// Assert
		Assert.IsNotNull(result);
		Assert.HasCount(1, result);
		Assert.AreEqual("value2", result["key"]);
	}

	[TestMethod]
	public void ConvertTextToDictionary_EmptyLines_IgnoresEmptyLines()
	{
		// Arrange
		string text = "key1=value1" + Environment.NewLine +
							Environment.NewLine +
							"key2=value2" + Environment.NewLine +
							Environment.NewLine;

		// Act
		Dictionary<string, string> result = MultilineTextToDictionary.ConvertTextToDictionary(text);

		// Assert
		Assert.IsNotNull(result);
		Assert.HasCount(2, result);
		Assert.AreEqual("value1", result["key1"]);
		Assert.AreEqual("value2", result["key2"]);
	}
}
