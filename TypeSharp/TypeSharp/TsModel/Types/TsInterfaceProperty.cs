namespace TypeSharp.TsModel.Types
{
    public class TsInterfaceProperty
    {
        public string Name { get; }
        public TsTypeBase PropertyType { get; }

        public TsInterfaceProperty(string name, TsTypeBase propertyType)
        {
            Name = name;
            PropertyType = propertyType;
        }
    }
}