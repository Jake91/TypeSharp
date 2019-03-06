using System;

namespace TypeSharp.Tests.TestData.SimpleClasses
{
    public class ClassWithAllSupportedTypes
    {
        public bool Abool { get; set; }
        public string Astring { get; set; }
        public DateTime ADatetime { get; set; }
        public DateTimeOffset ADatetimeOffset { get; set; }
        public long Along { get; set; }
        public int Aint { get; set; }
        public decimal Adecimal { get; set; }
        public double Adouble { get; set; }

        //public byte Abyte { get; set; } todo
        //public sbyte Asbyte { get; set; }
        //public char Achar { get; set; }
        //public float Afloat { get; set; }
        //public uint Auint { get; set; }
        //public ulong Aulong { get; set; }
        //public object Aobject { get; set; }
        //public short Ashort { get; set; }
        //public ushort Aushort { get; set; }
    }
}
