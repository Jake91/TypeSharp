using System.Collections.Generic;
using TypeSharp.TsModel.Types;

namespace TypeSharp.TsModel.Modules
{
    public sealed class TsModuleReference
    {
        public TsModule Module { get; }
        public IList<TsTypeBase> Types { get; }

        public TsModuleReference(TsModule module, IList<TsTypeBase> types)
        {
            Module = module;
            Types = types;
        }
    }
}