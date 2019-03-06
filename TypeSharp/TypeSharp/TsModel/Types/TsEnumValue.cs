namespace TypeSharp.TsModel.Types
{
    public sealed class TsEnumValue
    {
        public string Name { get; }
        public int Value { get; }

        public TsEnumValue(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }
}