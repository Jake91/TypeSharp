using System;
using System.Collections.Generic;
using TypeSharp.Common;

namespace TypeSharp.TsModel.Types
{
    public sealed class TsInterface : TsTypeDefinitionBase
    {
        public override string Name { get; }

        public TsTypeBase BaseType { get; internal set; }

        public bool IsExport { get; set; }

        public ICollection<TsInterfaceProperty> Properties { get; }

        public bool IsGeneric => !GenericArguments.IsNullOrEmpty();

        public ICollection<TsGenericArgument> GenericArguments { get; }

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
    }
}