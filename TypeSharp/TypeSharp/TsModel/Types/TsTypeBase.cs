using System;

namespace TypeSharp.TsModel.Types
{
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
