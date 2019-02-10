using System;
using System.Collections.Generic;
using Castle.DynamicProxy.Generators;
using NUnit.Framework;
using TypeSharp.Tests.TestData.SimpleClasses;

namespace TypeSharp.Tests
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        public void TestMethod1()
        {
            var generator = new Generator();
            var contentGenerator = new ContentGenerator();

            var m = generator.StructureModules(new List<Type>() {typeof(ClassWithAllSupportedTypes)});
            var m2 = generator.Convert(m);
            var result = contentGenerator.GenerateContent(m2[0]);


            var expectedResult = "export interface ClassWithAllSupportedTypes {    Abool: boolean;    Astring: string;    ADateTime: Date;    ADateTimeOffset: Date;    Along: number;    Aint: number;    Adecimal: number;    Adouble: number;}";
        }
    }
}
