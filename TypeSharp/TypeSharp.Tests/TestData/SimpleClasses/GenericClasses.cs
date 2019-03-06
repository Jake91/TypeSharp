namespace TypeSharp.Tests.TestData.SimpleClasses
{
    public class BasicGeneric<T1, T2>
    {
        public T1 TestProp1 { get; set; }
        public T2 TestProp2 { get; set; }
    }

    public class ClassWithGenericProperty
    {
        public BasicGeneric<int, string> GenericProperty { get; set; }
    }

    public class ClassThatPassesGenericParamToGenericProperty<T>
    {
        public BasicGeneric<T, string> GenericProperty { get; set; }
    }

    public class ClassWithGenericBaseClassInSeveralLevels : BasicGeneric<BasicGeneric<string, int>, string>
    {

    }

    public class GenericClassThatPassesGenericParamToGenericBaseClass<T> : BasicGeneric<BasicGeneric<T, int>, string>
    {

    }
}
