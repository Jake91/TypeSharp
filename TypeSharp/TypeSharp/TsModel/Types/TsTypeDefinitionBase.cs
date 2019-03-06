using System;

namespace TypeSharp.TsModel.Types
{
    /// <inheritdoc />
    /// <summary>
    /// Represents something that needs to be defined. Enum, Class, Interface...
    /// </summary>
    public abstract class TsTypeDefinitionBase : TsTypeBase
    {
        protected TsTypeDefinitionBase(Type cSharpType) : base(cSharpType)
        {
        }
    }
}