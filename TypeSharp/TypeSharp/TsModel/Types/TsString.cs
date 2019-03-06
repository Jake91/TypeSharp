using System;

namespace TypeSharp.TsModel.Types
{
    public sealed class TsString : TsDefaultTypeBase
    {
        public override bool IsObject { get; protected set; }

        public TsString(Type cSharpType, bool isObject = false) : base(cSharpType)
        {
            IsObject = isObject;
        }

        public void SetIsObject(bool isObject)
        {
            IsObject = isObject;
        }

        public override string Name => IsObject ? "String" : "string";
    }
}