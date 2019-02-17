using System;

namespace TypeSharp.TsModel.Types
{
    public sealed class TsNumber : TsDefaultType
    {
        public override bool IsObject { get; protected set; }

        public TsNumber(Type cSharpType, bool isObject = false) : base(cSharpType)
        {
            IsObject = isObject;
        }

        public void SetIsObject(bool isObject)
        {
            IsObject = isObject;
        }

        public override string Name => IsObject ? "Number" : "number";
    }
}