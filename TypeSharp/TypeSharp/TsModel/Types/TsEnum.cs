using System;
using System.Collections.Generic;

namespace TypeSharp.TsModel.Types
{
    public sealed class TsEnum : TsTypeDefinitionBase
    {
        public bool IsExport { get; set; }
        public ICollection<TsEnumValue> Values { get; }

        public TsEnum(Type cSharpType, string name, bool isExport, ICollection<TsEnumValue> values) : base(cSharpType)
        {
            IsExport = isExport;
            Values = values;
            Name = name;
        }

        public override string Name { get; }
    }
}