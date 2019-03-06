using System;

namespace TypeSharp.TsModel.Types
{
    /// <summary>
    /// Represents a type. Class, Interface, Enum, number, Date, [] (Arrays), T (generic), ClassX<string>...
    /// </summary>
    public abstract class TsTypeBase
    {
        public Type CSharpType { get; }
        public abstract string Name { get; }

        protected TsTypeBase(Type cSharpType)
        {
            CSharpType = cSharpType;
        }
    }
}
