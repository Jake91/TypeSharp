using TypeSharp.Tests.TestData.SimpleClasses;

namespace TypeSharp.Tests.TestData.NamespaceClasses.FirstSpace
{
    public class ClassWithPropertyReferenceToAnotherNamespace
    {
        public ClassWithAllSupportedTypes ClassWithAllSupportedTypes { get; set; }

        public ClassWithPropertyReferenceToAnotherNamespace(ClassWithAllSupportedTypes classWithAllSupportedTypes)
        {
            ClassWithAllSupportedTypes = classWithAllSupportedTypes;
        }
    }
}
