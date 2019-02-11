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
        public void TestSingleClassWithDefaultProperties()
        {
            var tsType = new TsTypeGenerator().Generate(typeof(ClassWithAllSupportedTypes));
            var module = new DefaultTsModuleGenerator().Generate(tsType);
            var tsFileContent = new TsFileContentGenerator().Generate("TestRoot", module);

            Assert.AreEqual(actual: tsFileContent.Content, expected: "export interface ClassWithAllSupportedTypes {\r\n\tAbool: boolean;\r\n\tAstring: string;\r\n\tADatetime: Date;\r\n\tADatetimeOffset: Date;\r\n\tAlong: number;\r\n\tAint: number;\r\n\tAdecimal: number;\r\n\tAdouble: number;\r\n}\r\n");
        }

        [Test]
        public void TestModuleReference()
        {
            var tsTypes = new TsTypeGenerator().Generate(new List<Type>() { typeof(ClassWithAllSupportedTypes), typeof(ClassWithPropertyReferenceToAnotherNamespace) });
            var modules = new DefaultTsModuleGenerator().Generate(tsTypes);
            var tsFileContentGenerator = new TsFileContentGenerator();
            var result = modules.Select(x => tsFileContentGenerator.Generate("TestRoot", x)).ToList();
        }
    }
}
