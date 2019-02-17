using System;

namespace TypeSharp.TsModel.Types
{
    public class TsGenericArgument : TsTypeBase // ex. T1
    {
        public override string Name { get; }

        public TsGenericArgument(Type cSharType, string name) : base(cSharType)
        {
            Name = name;
        }
    }
}