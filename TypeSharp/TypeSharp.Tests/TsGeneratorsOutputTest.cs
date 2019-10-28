using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TypeSharp.Tests.TestData.NamespaceClasses.FirstSpace;
using TypeSharp.Tests.TestData.SimpleClasses;
using TypeSharp.TsGenerators;

namespace TypeSharp.Tests
{
    [TestFixture]
    public class TsGeneratorsOutputTest
    {
        [Test]
        public void TestModuleReference()
        {
            var tsTypes = new TsTypeGenerator().Generate(new List<Type>() { typeof(ClassWithAllSupportedTypes), typeof(ClassWithPropertyReferenceToAnotherNamespace) }, generateInterfaceAsDefault: true);
            var modules = new TsModuleGenerator().Generate(tsTypes);
            var tsFileContentGenerator = new TsFileContentGenerator();
            var result = modules.Select(x => tsFileContentGenerator.Generate("TestRoot", x)).ToList();
            Assert.AreEqual(actual: result[1].Content, expected: "import { ClassWithAllSupportedTypes } from \"TestRoot/TypeSharp/Tests/TestData/SimpleClasses\";\r\nexport interface ClassWithPropertyReferenceToAnotherNamespace {\r\n\tClassWithAllSupportedTypes: ClassWithAllSupportedTypes;\r\n}\r\n");
        }
        
        [TestCase(typeof(ArrayClass), "export interface ArrayClass {\r\n\tStringArray: string[];\r\n\tStringList: string[];\r\n\tStringIList: string[];\r\n\tStringCollection: string[];\r\n\tStringEnumerable: string[];\r\n\tStringHashSet: string[];\r\n\tStringSet: string[];\r\n}\r\n")]
        [TestCase(typeof(GenericClassWithGenericArrayProperties<>), "export interface GenericClassWithGenericArrayProperties<T> {\r\n\tGenericArray: T[];\r\n\tGenericList: T[];\r\n\tGenericIList: T[];\r\n}\r\n")]
        [TestCase(typeof(ClassWithGenericBaseTypeList<>), "export interface ClassWithGenericBaseTypeList<T> extends GenericClass<T[]> {\r\n}\r\nexport interface GenericClass<T> {\r\n\tProp: T;\r\n}\r\n")]
        [TestCase(typeof(ClassWithGenericParameterPassedToBaseTypeAsArray<>), "export interface ClassWithGenericParameterPassedToBaseTypeAsArray<T> extends GenericClass<T[]> {\r\n}\r\nexport interface GenericClass<T> {\r\n\tProp: T;\r\n}\r\n")]
        [TestCase(typeof(ClassWithOwnGenericListProperty), "export interface ClassWithOwnGenericListProperty {\r\n\tList: string[];\r\n}\r\n")]
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
            var module = new TsModuleGenerator().Generate(tsTypes).Single();
            var tsFileContentGenerator = new TsFileContentGenerator();
            var result = tsFileContentGenerator.Generate("TestRoot", module);
            Assert.AreEqual(actual: result.Content, expected: expected);
        }

        //public void TestStuff()
        //{
        //      code for typescriptproxy generatpr Executor class
        //    var typeProvider = new CombinedTypeProvider(TypeProvidersFactory.CreateAllTypeProviders(arguments));

        //    var tsTypes = new TsTypeGenerator().Generate(typeProvider.FindTypes(), generateInterfaceAsDefault: true);
        //    var modules = new TsModuleGenerator().Generate(tsTypes);
        //    var tsFileContentGenerator = new TsFileContentGenerator();
        //    foreach (var tsModule in modules)
        //    {
        //        var result = tsFileContentGenerator.Generate("JakeJS", tsModule);
        //        var filePath = arguments.OutputDirectory + "/" + string.Join("/", result.FilePath);
        //        WriteGeneratedFile(filePath, result.Content, false, false);
        //    }
        //}
    }
}
