using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TypeSharp.Tests.TestData.NamespaceClasses.FirstSpace;
using TypeSharp.Tests.TestData.SimpleClasses;

namespace TypeSharp.Tests
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        public void TestModuleReference()
        {
            var tsTypes = new TsTypeGenerator().Generate(new List<Type>() { typeof(ClassWithAllSupportedTypes), typeof(ClassWithPropertyReferenceToAnotherNamespace) }, generateInterfaceAsDefault: true);
            var modules = new DefaultTsModuleGenerator().Generate(tsTypes);
            var tsFileContentGenerator = new TsFileContentGenerator();
            var result = modules.Select(x => tsFileContentGenerator.Generate("TestRoot", x)).ToList();
            Assert.AreEqual(actual: result[1].Content, expected: "import { ClassWithAllSupportedTypes } from \"TestRoot/TypeSharp/Tests/TestData/SimpleClasses\";\r\nexport interface ClassWithPropertyReferenceToAnotherNamespace {\r\n\tClassWithAllSupportedTypes: ClassWithAllSupportedTypes;\r\n}\r\n");
        }

        [TestCase(typeof(ClassWithAllSupportedTypes), "export interface ClassWithAllSupportedTypes {\r\n\tAbool: boolean;\r\n\tAstring: string;\r\n\tADatetime: Date;\r\n\tADatetimeOffset: Date;\r\n\tAlong: number;\r\n\tAint: number;\r\n\tAdecimal: number;\r\n\tAdouble: number;\r\n}\r\n")]
        [TestCase(typeof(TestClassChild), "export interface TestClassChild extends TestClassBase {\r\n\tTestNameChild: string;\r\n}\r\nexport interface TestClassBase {\r\n\tNameInBase: string;\r\n}\r\n")]
        [TestCase(typeof(SimpleEnum), "export enum SimpleEnum {\r\n\tOne = 3,\r\n\tTwo = 5\r\n}\r\n")]
        [TestCase(typeof(ClassWithGenericProperty), "export interface ClassWithGenericProperty {\r\n\tGenericProperty: BasicGeneric<number, string>;\r\n}\r\nexport interface BasicGeneric<T1, T2> {\r\n\tTestProp1: T1;\r\n\tTestProp2: T2;\r\n}\r\n")]
        [TestCase(typeof(ClassWithGenericBaseClassInSeveralLevels), "export interface ClassWithGenericBaseClassInSeveralLevels extends BasicGeneric<BasicGeneric<string, number>, string> {\r\n}\r\nexport interface BasicGeneric<T1, T2> {\r\n\tTestProp1: T1;\r\n\tTestProp2: T2;\r\n}\r\n")]
        [TestCase(typeof(GenericClassThatPassesGenericParamToGenericBaseClass<>), "export interface GenericClassThatPassesGenericParamToGenericBaseClass<T> extends BasicGeneric<BasicGeneric<T, number>, string> {\r\n}\r\nexport interface BasicGeneric<T1, T2> {\r\n\tTestProp1: T1;\r\n\tTestProp2: T2;\r\n}\r\n")]
        [TestCase(typeof(ClassThatPassesGenericParamToGenericProperty<>), "export interface ClassThatPassesGenericParamToGenericProperty<T> {\r\n\tGenericProperty: BasicGeneric<T, string>;\r\n}\r\nexport interface BasicGeneric<T1, T2> {\r\n\tTestProp1: T1;\r\n\tTestProp2: T2;\r\n}\r\n")]
        public void TestTypeToStringContentGenerateForSingleModule(Type type, string expected)
        {
            var tsTypes = new TsTypeGenerator().Generate(type, generateInterfaceAsDefault: true);
            var module = new DefaultTsModuleGenerator().Generate(tsTypes).Single();
            var tsFileContentGenerator = new TsFileContentGenerator();
            var result = tsFileContentGenerator.Generate("TestRoot", module);
            Assert.AreEqual(actual: result.Content, expected: expected);
        }
        
    }
}
