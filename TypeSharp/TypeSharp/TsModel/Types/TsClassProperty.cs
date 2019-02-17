namespace TypeSharp.TsModel.Types
{
    public class TsClassProperty
    {
        public string Name { get; set; }
        public TsTypeBase PropertyType { get; }
        public TsAccessModifier AccessModifier { get; set; }
        public bool HasGetter { get; set; } // todo
        public bool HasSetter { get; set; } // todo

        public TsClassProperty(string name, TsTypeBase propertyType, TsAccessModifier accessModifier, bool hasGetter, bool hasSetter)
        {
            Name = name;
            PropertyType = propertyType;
            AccessModifier = accessModifier;
            HasGetter = hasGetter;
            HasSetter = hasSetter;
        }
    }
}