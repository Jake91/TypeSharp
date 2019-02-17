using System;
using System.Collections.Generic;
using Castle.Core.Internal;

namespace TypeSharp.TsModel.Types
{
    public sealed class TsInterface : TsTypeBase
    {
        public bool IsExport { get; }

        public ICollection<TsInterfaceProperty> Properties { get; }

        public bool IsGeneric => !GenericArguments.IsNullOrEmpty();
        public ICollection<TsGenericArgument> GenericArguments { get; }

        public TsTypeBase BaseType { get; internal set; }

        public TsInterface(Type cSharpType, string name, bool isExport, ICollection<TsInterfaceProperty> properties,
            TsTypeBase baseType, ICollection<TsGenericArgument> genericArguments)
            : base(cSharpType)
        {
            IsExport = isExport;
            Properties = properties;
            BaseType = baseType;
            GenericArguments = genericArguments;
            Name = name;
        }

        public override string Name { get; }
    }
}