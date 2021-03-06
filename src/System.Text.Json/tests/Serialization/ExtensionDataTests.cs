﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class ExtensionDataTests
    {
        [Fact]
        public static void ExtensionPropertyNotUsed()
        {
            string json = @"{""MyNestedClass"":" + SimpleTestClass.s_json + "}";
            ClassWithExtensionProperty obj = JsonSerializer.Parse<ClassWithExtensionProperty>(json);
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public static void ExtensionPropertyRoundTrip()
        {
            ClassWithExtensionProperty obj;

            {
                string json = @"{""MyIntMissing"":2, ""MyInt"":1, ""MyNestedClassMissing"":" + SimpleTestClass.s_json + "}";
                obj = JsonSerializer.Parse<ClassWithExtensionProperty>(json);
                Verify();
            }

            // Round-trip the json.
            {
                string json = JsonSerializer.ToString(obj);
                obj = JsonSerializer.Parse<ClassWithExtensionProperty>(json);
                Verify();
            }

            void Verify()
            {
                Assert.NotNull(obj.MyOverflow);
                Assert.NotNull(obj.MyOverflow["MyIntMissing"]);
                Assert.Equal(1, obj.MyInt);
                Assert.Equal(2, obj.MyOverflow["MyIntMissing"].GetInt32());

                JsonProperty[] properties = obj.MyOverflow["MyNestedClassMissing"].EnumerateObject().ToArray();

                // Verify a couple properties
                Assert.Equal(1, properties.Where(prop => prop.Name == "MyInt16").First().Value.GetInt32());
                Assert.Equal(true, properties.Where(prop => prop.Name == "MyBooleanTrue").First().Value.GetBoolean());
            }
        }

        [Fact]
        public static void ExtensionPropertyAlreadyInstantiated()
        {
            Assert.NotNull(new ClassWithExtensionPropertyAlreadyInstantiated().MyOverflow);

            string json = @"{""MyIntMissing"":2}";

            ClassWithExtensionProperty obj = JsonSerializer.Parse<ClassWithExtensionProperty>(json);
            Assert.Equal(2, obj.MyOverflow["MyIntMissing"].GetInt32());
        }

        [Fact]
        public static void ExtensionPropertyAsObject()
        {
            string json = @"{""MyIntMissing"":2}";

            ClassWithExtensionPropertyAsObject obj = JsonSerializer.Parse<ClassWithExtensionPropertyAsObject>(json);
            Assert.IsType<JsonElement>(obj.MyOverflow["MyIntMissing"]);
            Assert.Equal(2, ((JsonElement)obj.MyOverflow["MyIntMissing"]).GetInt32());
        }

        [Fact]
        public static void ExtensionPropertyCamelCasing()
        {
            // Currently we apply no naming policy. If we do (such as a ExtensionPropertyNamingPolicy), we'd also have to add functionality to the JsonDocument.

            ClassWithExtensionProperty obj;
            const string jsonWithProperty = @"{""MyIntMissing"":1}";
            const string jsonWithPropertyCamelCased = @"{""myIntMissing"":1}";

            {
                // Baseline Pascal-cased json + no casing option.
                obj = JsonSerializer.Parse<ClassWithExtensionProperty>(jsonWithProperty);
                Assert.Equal(1, obj.MyOverflow["MyIntMissing"].GetInt32());
                string json = JsonSerializer.ToString(obj);
                Assert.Contains(@"""MyIntMissing"":1", json);
            }

            {
                // Pascal-cased json + camel casing option.
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                obj = JsonSerializer.Parse<ClassWithExtensionProperty>(jsonWithProperty, options);
                Assert.Equal(1, obj.MyOverflow["MyIntMissing"].GetInt32());
                string json = JsonSerializer.ToString(obj);
                Assert.Contains(@"""MyIntMissing"":1", json);
            }

            {
                // Baseline camel-cased json + no casing option.
                obj = JsonSerializer.Parse<ClassWithExtensionProperty>(jsonWithPropertyCamelCased);
                Assert.Equal(1, obj.MyOverflow["myIntMissing"].GetInt32());
                string json = JsonSerializer.ToString(obj);
                Assert.Contains(@"""myIntMissing"":1", json);
            }

            {
                // Baseline camel-cased json + camel casing option.
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                obj = JsonSerializer.Parse<ClassWithExtensionProperty>(jsonWithPropertyCamelCased, options);
                Assert.Equal(1, obj.MyOverflow["myIntMissing"].GetInt32());
                string json = JsonSerializer.ToString(obj);
                Assert.Contains(@"""myIntMissing"":1", json);
            }
        }

        [Fact]
        public static void NullValuesIgnored()
        {
            const string json = @"{""MyNestedClass"":null}";
            const string jsonMissing = @"{ ""MyNestedClassMissing"":null}";

            {
                // Baseline with no missing.
                ClassWithExtensionProperty obj = JsonSerializer.Parse<ClassWithExtensionProperty>(json);
                Assert.Null(obj.MyOverflow);

                string outJson = JsonSerializer.ToString(obj);
                Assert.Contains(@"""MyNestedClass"":null", outJson);
            }

            {
                // Baseline with missing.
                ClassWithExtensionProperty obj = JsonSerializer.Parse<ClassWithExtensionProperty>(jsonMissing);
                Assert.Equal(1, obj.MyOverflow.Count);
                Assert.Equal(JsonValueType.Null, obj.MyOverflow["MyNestedClassMissing"].Type);
            }

            {
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.IgnoreNullValues = true;

                ClassWithExtensionProperty obj = JsonSerializer.Parse<ClassWithExtensionProperty>(jsonMissing, options);

                // Currently we do not ignore nulls in the extension data. The JsonDocument would also need to support this mode
                // for any lower-level nulls.
                Assert.Equal(1, obj.MyOverflow.Count);
                Assert.Equal(JsonValueType.Null, obj.MyOverflow["MyNestedClassMissing"].Type);
            }
        }

        [Fact]
        public static void InvalidExtensionPropertyFail()
        {
            // Baseline
            JsonSerializer.Parse<ClassWithExtensionProperty>(@"{}");
            JsonSerializer.Parse<ClassWithExtensionPropertyAsObject>(@"{}");

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Parse<ClassWithInvalidExtensionProperty>(@"{}"));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Parse<ClassWithTwoExtensionPropertys>(@"{}"));
        }

        [Fact]
        public static void IgnoredDataShouldNotBeExtensionData()
        {
            ClassWithIgnoredData obj = JsonSerializer.Parse<ClassWithIgnoredData>(@"{""MyInt"":1}");

            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public static void ExtensionPropertyObjectValue_Empty()
        {
            // Baseline
            ClassWithExtensionPropertyAlreadyInstantiated obj = JsonSerializer.Parse<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}");
            Assert.Equal(@"{""MyOverflow"":{}}", JsonSerializer.ToString(obj));
        }

        [Fact]
        public static void ExtensionPropertyObjectValue()
        {
            // Baseline
            ClassWithExtensionPropertyAlreadyInstantiated obj = JsonSerializer.Parse<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}");
            obj.MyOverflow.Add("test", new object());
            obj.MyOverflow.Add("test1", 1);

            Assert.Equal(@"{""MyOverflow"":{""test"":{},""test1"":1}}", JsonSerializer.ToString(obj));
        }

        [Fact]
        public static void ExtensionPropertyObjectValue_RoundTrip()
        {
            // Baseline
            ClassWithExtensionPropertyAlreadyInstantiated obj = JsonSerializer.Parse<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}");
            obj.MyOverflow.Add("test", new object());
            obj.MyOverflow.Add("test1", 1);
            obj.MyOverflow.Add("test2", "text");
            obj.MyOverflow.Add("test3", new DummyObj() { Prop = "ObjectProp" });
            obj.MyOverflow.Add("test4", new DummyStruct() { Prop = "StructProp" });
            obj.MyOverflow.Add("test5", new Dictionary<string, object>() { { "Key", "Value" }, { "Key1", "Value1" }, });

            ClassWithExtensionPropertyAlreadyInstantiated roundTripObj = JsonSerializer.Parse<ClassWithExtensionPropertyAlreadyInstantiated>(JsonSerializer.ToString(obj));

            Assert.Equal(6, roundTripObj.MyOverflow.Count);

            Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test"]);
            Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test1"]);
            Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test2"]);
            Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test3"]);

            Assert.Equal(JsonValueType.Object, ((JsonElement)roundTripObj.MyOverflow["test"]).Type);

            Assert.Equal(JsonValueType.Number, ((JsonElement)roundTripObj.MyOverflow["test1"]).Type);
            Assert.Equal(1, ((JsonElement)roundTripObj.MyOverflow["test1"]).GetInt32());
            Assert.Equal(1, ((JsonElement)roundTripObj.MyOverflow["test1"]).GetInt64());

            Assert.Equal(JsonValueType.String, ((JsonElement)roundTripObj.MyOverflow["test2"]).Type);
            Assert.Equal("text", ((JsonElement)roundTripObj.MyOverflow["test2"]).GetString());

            Assert.Equal(JsonValueType.Object, ((JsonElement)roundTripObj.MyOverflow["test3"]).Type);
            Assert.Equal("ObjectProp", ((JsonElement)roundTripObj.MyOverflow["test3"]).GetProperty("Prop").GetString());

            Assert.Equal(JsonValueType.Object, ((JsonElement)roundTripObj.MyOverflow["test4"]).Type);
            Assert.Equal("StructProp", ((JsonElement)roundTripObj.MyOverflow["test4"]).GetProperty("Prop").GetString());

            Assert.Equal(JsonValueType.Object, ((JsonElement)roundTripObj.MyOverflow["test5"]).Type);
            Assert.Equal("Value", ((JsonElement)roundTripObj.MyOverflow["test5"]).GetProperty("Key").GetString());
            Assert.Equal("Value1", ((JsonElement)roundTripObj.MyOverflow["test5"]).GetProperty("Key1").GetString());
        }

        [Fact]
        public static void ObjectTree()
        {
            string json = @"{""MyIntMissing"":2, ""MyReference"":{""MyIntMissingChild"":3}}";

            ClassWithReference obj = JsonSerializer.Parse<ClassWithReference>(json);
            Assert.IsType<JsonElement>(obj.MyOverflow["MyIntMissing"]);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(2, obj.MyOverflow["MyIntMissing"].GetInt32());

            ClassWithExtensionProperty child = obj.MyReference;
            Assert.IsType<JsonElement>(child.MyOverflow["MyIntMissingChild"]);
            Assert.IsType<JsonElement>(child.MyOverflow["MyIntMissingChild"]);
            Assert.Equal(1, child.MyOverflow.Count);
            Assert.Equal(3, child.MyOverflow["MyIntMissingChild"].GetInt32());
        }

        [Fact]
        public static void ExtensionProperty_InvalidDictionary()
        {
            ClassWithInvalidExtensionPropertyStringString obj1 = new ClassWithInvalidExtensionPropertyStringString();
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.ToString(obj1));

            ClassWithInvalidExtensionPropertyObjectString obj2 = new ClassWithInvalidExtensionPropertyObjectString();
            Assert.Throws<NotSupportedException>(() => JsonSerializer.ToString(obj2));
        }

        public class DummyObj
        {
            public string Prop { get; set; }
        }

        public struct DummyStruct
        {
            public string Prop { get; set; }
        }

        public class ClassWithExtensionPropertyAlreadyInstantiated
        {
            public ClassWithExtensionPropertyAlreadyInstantiated()
            {
                MyOverflow = new Dictionary<string, object>();
            }

            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyAsObject
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        public class ClassWithIgnoredData
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }

            [JsonIgnore]
            public int MyInt { get; set; }
        }

        public class ClassWithInvalidExtensionProperty
        {
            [JsonExtensionData]
            public Dictionary<string, int> MyOverflow { get; set; }
        }

        public class ClassWithInvalidExtensionPropertyStringString
        {
            [JsonExtensionData]
            public Dictionary<string, string> MyOverflow { get; set; }
        }

        public class ClassWithInvalidExtensionPropertyObjectString
        {
            [JsonExtensionData]
            public Dictionary<DummyObj, string> MyOverflow { get; set; }
        }

        public class ClassWithTwoExtensionPropertys
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow1 { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow2 { get; set; }
        }

        public class ClassWithReference
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> MyOverflow { get; set; }

            public ClassWithExtensionProperty MyReference { get; set; }
        }
    }
}
