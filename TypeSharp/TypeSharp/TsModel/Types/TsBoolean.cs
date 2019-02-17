using System;

namespace TypeSharp.TsModel.Types
{
    public sealed class TsBoolean : TsDefaultType
    {
        public override bool IsObject { get; protected set; }

        public TsBoolean(Type cSharpType, bool isObject = false) : base(cSharpType)
        {
            IsObject = isObject;
        }

        public void SetIsObject(bool isObject)
        {
            IsObject = isObject;
        }

        public override string Name => IsObject ? "Boolean" : "boolean";
    }
}