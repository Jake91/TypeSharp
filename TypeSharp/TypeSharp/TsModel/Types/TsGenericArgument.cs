using System;

namespace TypeSharp.TsModel.Types
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a generic argument. Ex. TResult
    /// </summary>
    public class TsGenericArgument : TsTypeBase
    {
        public override string Name { get; }

        public TsGenericArgument(Type cSharType, string name) : base(cSharType)
        {
            Name = name;
        }
    }
}