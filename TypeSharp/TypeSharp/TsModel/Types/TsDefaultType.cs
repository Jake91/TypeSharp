using System;

namespace TypeSharp.TsModel.Types
{
    public abstract class TsDefaultType : TsTypeBase
    {
        public abstract bool IsObject { get; protected set; } // todo remove?

        protected TsDefaultType(Type cSharpType) : base(cSharpType)
        {
        }
    }
}