using System.Collections.Generic;
using System.Linq;

// TODO: lexicalscope

namespace CreamPuff {
    /// <summary>
    /// Converts arguments and local variables to/from indices.
    /// </summary>
    public class ILScope {
        public List<string> Arguments = new List<string>();
        public List<string> Locals = new List<string>();
        public ILScope Parent = null;
        public short StartIndex = 0;
        public string Name = "";
        public short Statics = 0;
        public string FullName => Parent == null ? Name : Parent.FullName + '.' + Name;

        public void SetArguments(IEnumerable<string> arguments) => Arguments = arguments.ToList();

        public short this[string name] {
            get {
                var current = this;
                while (current.Parent != null) {
                    var index = (short) (current.Locals.Count - 1 - current.Locals.ToList().IndexOf(name));
                    if (index != -1)
                        return (short) (current.StartIndex + index);
                }
                return -1;
            }

            set {
                if (!Locals.Contains(name)) Locals.Add(name);
            }
        }

        public short this[string name, int depth] {
            get {
                var current = this;
                while (depth != 0)
                    current = current.Parent;
                return (short) (current.Locals.Count - 1 - current.Locals.ToList().IndexOf(name));
            }

            set {
                if (!Locals.Contains(name)) Locals.Add(name);
            }
        }
    }
}
