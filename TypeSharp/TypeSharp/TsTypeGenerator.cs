using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TypeSharp
{
    public class TsTypeGenerator
    {
        public TsType Generate(Type type)
        {
            return Generate(new List<Type>() {type}).Single();
        }

        public IList<TsType> Generate(ICollection<Type> types)
        {
            var typeDict = types.Select(x => new TsType(x, x.Name, true, true, x.IsEnum, new List<TsProperty>())).ToDictionary(x => x.CSharpType, x => x);

            foreach (var tsType in typeDict.Values)
            {
                PopulateProperties(tsType, typeDict);
            }

            return typeDict.Values.ToList();
        }

        private void PopulateProperties(TsType type, IReadOnlyDictionary<Type, TsType> typeDict)
        {
            var properties = type.CSharpType.GetProperties(BindingFlags.Instance | BindingFlags.Public); // todo improve
            foreach (var propertyInfo in properties)
            {
                if (IsDefaultType(propertyInfo.PropertyType))
                {
                    type.Properties.Add(
                        new TsProperty(
                            name: propertyInfo.Name,
                            propertyType: new TsDefaultType(
                                propertyInfo.PropertyType,
                                propertyInfo.PropertyType.Name,
                                GetDefaultType(propertyInfo.PropertyType)),
                            accessModifier: TsAccessModifier.Public,
                            hasGetter: false,
                            hasSetter: false));
                }
                else if (typeDict.TryGetValue(propertyInfo.PropertyType, out var tsType))
                {
                    type.Properties.Add(new TsProperty(propertyInfo.Name, tsType, TsAccessModifier.Public, false, false));
                }
                else
                {
                    throw new ArgumentException($"Type ({type.Name}) is not a default type, and it is not part of the types to generate");
                }
            }
        }

        private TsDefault GetDefaultType(Type type)
        {
            if (type == typeof(bool))
            {
                return TsDefault.Boolean;
            }
            else if (type == typeof(DateTime) ||
                     type == typeof(DateTimeOffset))
            {
                return TsDefault.Date;
            }
            else if (type == typeof(string))
            {
                return TsDefault.String;
            }
            else if (type == typeof(long) ||
                     type == typeof(int) ||
                     type == typeof(decimal) ||
                     type == typeof(double))
            {
                return TsDefault.Number;
            }
            throw new ArgumentException("Type is not a default ts type");
        }

        private static bool IsDefaultType(Type type) // todo location?
        {
            return
                type == typeof(bool) ||
                type == typeof(string) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(long) ||
                type == typeof(int) ||
                type == typeof(decimal) ||
                type == typeof(double);
        }
    }
    public class TsType
    {
        public Type CSharpType { get; set; }
        public string Name { get; set; }
        public bool IsExport { get; set; }
        public bool IsInterface { get; set; }
        //public bool IsClass { get; set; }
        //public bool IsAbstract { get; set; }
        public bool IsEnum { get; set; }
        //public bool IsPrimitive { get; set; }
        public ICollection<TsProperty> Properties { get; set; }

        public TsType(Type cSharpType, string name, bool isExport, bool isInterface, bool isEnum, ICollection<TsProperty> properties)
        {
            CSharpType = cSharpType;
            Name = name;
            IsExport = isExport;
            IsInterface = isInterface;
            IsEnum = isEnum;
            Properties = properties;
        }
    }

    public enum TsDefault
    {
        Number,
        Boolean,
        Date,
        String
    }

    public class TsDefaultType : TsType
    {
        public TsDefault DefaultType { get; }

        public TsDefaultType(Type cSharpType, string name, TsDefault defaultType) : base(cSharpType, name, false, false, false, new List<TsProperty>())
        {
            DefaultType = defaultType;
        }
    }

    public class TsProperty
    {
        public string Name { get; set; }
        public TsType PropertyType { get; set; }
        public TsAccessModifier AccessModifier { get; set; }
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }

        public TsProperty(string name, TsType propertyType, TsAccessModifier accessModifier, bool hasGetter, bool hasSetter)
        {
            Name = name;
            PropertyType = propertyType;
            AccessModifier = accessModifier;
            HasGetter = hasGetter;
            HasSetter = hasSetter;
        }
    }

    public enum TsAccessModifier
    {
        Private = 1,
        Protected = 2,
        Public = 3
    }
}
