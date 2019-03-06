using System.Collections.Generic;
using TypeSharp.TsModel.Types;

namespace TypeSharp.TsModel.Modules
{
    public sealed class TsModuleImport
    {
        public TsModule Module { get; }
        public IList<TsTypeDefinitionBase> Types { get; }

        public TsModuleImport(TsModule module, IList<TsTypeDefinitionBase> types)
        {
            Module = module;
            Types = types;
        }
    }
}