using System;
using System.Collections.Generic;

namespace TypeSharp.TsModel.Types
{
    public class TsGenericTypeReference : TsTypeBase // ex <ClassX<T1>>
    {
        public override string Name => Type.Name;
        public TsTypeBase Type { get; }
        public ICollection<TsTypeBase> GenericArguments { get; }

        public TsGenericTypeReference(Type cSharpType, TsTypeBase type, ICollection<TsTypeBase> genericArguments) : base(cSharpType)
        {
            Type = type;
            GenericArguments = genericArguments;
        }
    }
}