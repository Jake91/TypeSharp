using System;

namespace TypeSharp.TsModel.Types
{
    public sealed class TsDate : TsDefaultTypeBase
    {
        public override bool IsObject { get; protected set; }

        public TsDate(Type cSharpType) : base(cSharpType)
        {
            IsObject = true;
        }

        public override string Name => "Date";
    }
}