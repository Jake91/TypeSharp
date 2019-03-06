using System;

namespace TypeSharp.TsModel.Types
{
    public abstract class TsDefaultTypeBase : TsTypeBase
    {
        public abstract bool IsObject { get; protected set; } // todo remove?

        protected TsDefaultTypeBase(Type cSharpType) : base(cSharpType)
        {
        }
    }
}