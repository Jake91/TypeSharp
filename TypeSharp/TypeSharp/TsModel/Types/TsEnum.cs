using System;
using System.Collections.Generic;

namespace TypeSharp.TsModel.Types
{
    public sealed class TsEnum : TsTypeBase
    {
        public bool IsExport { get; set; }
        public ICollection<EnumValue> Values { get; }

        public TsEnum(Type cSharpType, string name, bool isExport, ICollection<EnumValue> values) : base(cSharpType)
        {
            IsExport = isExport;
            Values = values;
            Name = name;
        }

        public override string Name { get; }
    }
}