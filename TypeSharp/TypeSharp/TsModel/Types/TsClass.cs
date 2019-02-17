using System;
using System.Collections.Generic;
using Castle.Core.Internal;

namespace TypeSharp.TsModel.Types
{
    public sealed class TsClass : TsTypeBase
    {
        public TsTypeBase BaseType { get; internal set; }

        public bool IsExport { get; set; }

        public bool IsGeneric => !GenericArguments.IsNullOrEmpty();
        public ICollection<TsGenericArgument> GenericArguments { get; }

        public ICollection<TsClassProperty> Properties { get; }

        public TsClass(Type cSharpType, string name, bool isExport, ICollection<TsClassProperty> properties,
            TsTypeBase baseType, ICollection<TsGenericArgument> genericArguments) : base(cSharpType)
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