using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeSharp.TsModel.Modules
{
    public class TsModuleLocation : IEquatable<TsModuleLocation>
    {
        public string Name { get; }

        public IReadOnlyCollection<string> Path { get; } // Order is important

        public TsModuleLocation(string name, IReadOnlyCollection<string> path)
        {
            Name = name;
            Path = path;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TsModuleLocation);
        }

        public bool Equals(TsModuleLocation other)
        {
            return other != null &&
                   Name == other.Name &&
                   Path != null &&
                   Path.SequenceEqual(other.Path);
        }

        public override int GetHashCode()
        {
            var hashCode = 555465246;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            foreach (var p in Path)
            {
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(p);
            }

            return hashCode;
        }
    }
}