using NUnit.Framework;
using System.Collections.Generic;
using TypeSharp.TsModel.Modules;

namespace TypeSharp.Tests
{
    [TestFixture]
    public class TsModuleLocationTest
    {
        [Test]
        public void Equals_True_Test()
        {
            var location1 = new TsModuleLocation("Test", new List<string> { "path1", "path2", "path3" });
            var location2 = new TsModuleLocation("Test", new List<string> { "path1", "path2", "path3" });
            Assert.IsTrue(location1.Equals(location2));
        }

        [Test]
        public void Equals_False_Test()
        {
            var location1 = new TsModuleLocation("Test", new List<string> { "path1", "path2", "path3" });
            var location2 = new TsModuleLocation("Test", new List<string> { "path1", "path2" });
            Assert.IsFalse(location1.Equals(location2));
        }

        [Test]
        public void GetHashCode_Equal_Test()
        {
            var location1 = new TsModuleLocation("Test", new List<string> { "path1", "path2", "path3" });
            var location2 = new TsModuleLocation("Test", new List<string> { "path1", "path2", "path3" });
            Assert.AreEqual(location1.GetHashCode(), location2.GetHashCode());
        }

        [Test]
        public void GetHashCode_NotEqual_PathOrder_Test()
        {
            var location1 = new TsModuleLocation("Test", new List<string> { "path1", "path2", "path3" });
            var location2 = new TsModuleLocation("Test", new List<string> { "path1", "path3", "path2" });
            Assert.AreNotEqual(location1.GetHashCode(), location2.GetHashCode());
        }

        [Test]
        public void GetHashCode_NotEqual_Name_Test()
        {
            var location1 = new TsModuleLocation("Test", new List<string> { "path1", "path2", "path3" });
            var location2 = new TsModuleLocation("Test1", new List<string> { "path1", "path2", "path3" });
            Assert.AreNotEqual(location1.GetHashCode(), location2.GetHashCode());
        }

        [Test]
        public void GetHashCode_NotEqual_PathLenght_Test()
        {
            var location1 = new TsModuleLocation("Test", new List<string> { "path1", "path2", "path3" });
            var location2 = new TsModuleLocation("Test1", new List<string> { "path1", "path2"});
            Assert.AreNotEqual(location1.GetHashCode(), location2.GetHashCode());
        }

        [Test]
        public void Dictionary_Lookup_Test()
        {
            var location1 = new TsModuleLocation("Test", new List<string> { "path1", "path2", "path3" });
            var location2 = new TsModuleLocation("Test", new List<string> { "path1", "path2", "path3" });
            var dictionary = new Dictionary<TsModuleLocation, string> {{location1, "whatever"}};
            Assert.IsTrue(dictionary.TryGetValue(location2, out _));
        }
    }
}
