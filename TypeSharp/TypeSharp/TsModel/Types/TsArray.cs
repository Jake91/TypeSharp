using System;

namespace TypeSharp.TsModel.Types
{
    public sealed class TsArray : TsCollection
    {
        public TsArray(Type cSharpType, TsTypeBase elementType) : base(cSharpType, elementType)
        {
        }

        public override string Name => ElementType.Name + "[]";
    }
}