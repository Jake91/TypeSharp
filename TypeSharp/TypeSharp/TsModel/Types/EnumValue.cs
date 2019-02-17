namespace TypeSharp.TsModel.Types
{
    public sealed class EnumValue
    {
        public string Name { get; }
        public int Value { get; }

        public EnumValue(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }
}