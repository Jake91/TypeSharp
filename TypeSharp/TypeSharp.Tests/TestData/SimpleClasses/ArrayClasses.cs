using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeSharp.Tests.TestData.SimpleClasses
{
    public class ArrayClass
    {
        public string[] StringArray { get; set; }
        public List<string> StringList { get; set; }
        public IList<string> StringIList { get; set; }
        public ICollection<string> StringCollection { get; set; }
        public IEnumerable<string> StringEnumerable { get; set; }
        public HashSet<string> StringHashSet { get; set; }
        public ISet<string> StringSet { get; set; }
    }

    public class GenericClass<T>
    {
        public T Prop { get; set; }
    }

    public class GenericClassWithGenericArrayProperties<T>
    {
        public T[] GenericArray { get; set; }
        public List<T> GenericList { get; set; }
        public IList<T> GenericIList { get; set; }
    }

    public class ClassWithGenericBaseTypeList<T> : GenericClass<List<T>>
    {
        
    }

    public class ClassWithGenericParameterPassedToBaseTypeAsArray<T> : GenericClass<T[]>
    {

    }

    public class MyList<T> : List<T>
    {

    }

    public class ClassWithOwnGenericListProperty
    {
        public MyList<string> List { get; set; }
    }
}
