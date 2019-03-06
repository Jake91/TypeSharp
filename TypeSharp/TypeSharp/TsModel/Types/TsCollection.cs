using System;

namespace TypeSharp.TsModel.Types
{
    public abstract class TsCollection : TsTypeBase
    {
        public TsTypeBase ElementType { get; }

        protected TsCollection(Type cSharpType, TsTypeBase elementType) : base(cSharpType)
        {
            ElementType = elementType;
        }
    }
}