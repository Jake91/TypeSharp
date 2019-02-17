using System;

namespace TypeSharp.TsModel.Types
{
    public sealed class TsArray : TsTypeBase
    {
        public TsTypeBase ElementType { get; }

        public TsArray(Type cSharpType, TsTypeBase elementType) : base(cSharpType)
        {
            ElementType = elementType;
        }

        public override string Name => ElementType.Name + "[]";
    }
}