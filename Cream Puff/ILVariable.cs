using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;

// TODO: fix basically every integer type
namespace CreamPuff {
    /// <summary>
    /// Value writable to CIL.
    /// </summary>
    public interface ILValue {
        /// <summary>
        /// Whether value can be assigned to.
        /// </summary>
        /// <returns></returns>
        bool Assignable();

        /// <summary>
        /// Generate IL to push value onto stack.
        /// </summary>
        /// <param name="s">Current scope.</param>
        /// <param name="g">IL generator.</param>
        /// <param name="m">Current module.</param>
        void ToIL(ILScope s, ILGenerator g, ModuleBuilder m);
    }

    /// <summary>
    /// Constant.
    /// </summary>
    public interface ILConstant : ILValue { }

    /// <summary>
    /// Number.
    /// </summary>
    public interface ILNumber : ILConstant { }

    /// <summary>
    /// Integer.
    /// </summary>
    public interface ILInteger : ILNumber { }

    /// <summary>
    /// Signed integer.
    /// </summary>
    public interface ILInt : ILInteger { }

    /// <summary>
    /// Unsigned integer.
    /// </summary>
    public interface ILUint : ILInteger { }

    /// <summary>
    /// Floating point number.
    /// </summary>
    public interface ILFloat : ILNumber { }

    /// <summary>
    /// 1-byte signed integer.
    /// </summary>
    public class ILInt8 : ILInt {
        public byte Value;

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldc_I4, Value);
        public bool Assignable() => false;
    }

    /// <summary>
    /// 2-byte signed integer.
    /// </summary>
    public class ILInt16 : ILInt {
        public short Value;

        public static implicit operator ILInt16(ILInt8 i) => new ILInt16 { Value = i.Value };
        public static implicit operator ILInt16(ILUInt8 i) => new ILInt16 { Value = (short) i.Value };

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldc_I4, Value);
        public bool Assignable() => false;
    }

    /// <summary>
    /// 4-byte signed integer.
    /// </summary>
    public class ILInt32 : ILInt {
        public int Value;

        public static implicit operator ILInt32(ILUInt8 i) => new ILInt32 { Value = i.Value };
        public static implicit operator ILInt32(ILUInt16 i) => new ILInt32 { Value = i.Value };
        public static implicit operator ILInt32(ILInt8 i) => new ILInt32 { Value = i.Value };
        public static implicit operator ILInt32(ILInt16 i) => new ILInt32 { Value = i.Value };

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldc_I4, Value);
        public bool Assignable() => false;
    }

    /// <summary>
    /// 8-byte signed integer.
    /// </summary>
    public class ILInt64 : ILInt {
        public long Value;

        public static implicit operator ILInt64(ILUInt8 i) => new ILInt64 { Value = i.Value };
        public static implicit operator ILInt64(ILUInt16 i) => new ILInt64 { Value = i.Value };
        public static implicit operator ILInt64(ILUInt32 i) => new ILInt64 { Value = i.Value };
        public static implicit operator ILInt64(ILInt8 i) => new ILInt64 { Value = i.Value };
        public static implicit operator ILInt64(ILInt16 i) => new ILInt64 { Value = i.Value };
        public static implicit operator ILInt64(ILInt32 i) => new ILInt64 { Value = i.Value };

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldc_I8, Value);
        public bool Assignable() => false;
    }

    /// <summary>
    /// Arbitrary precision signed integer.
    /// </summary>
    public class ILBigInt : ILInt {
        private static readonly BinaryFormatter f = new BinaryFormatter();
        private static readonly MethodInfo ia = typeof(RuntimeHelpers).GetMethod("InitializeArray", new[] { typeof(System.Array), typeof(System.RuntimeFieldHandle) });
        private static readonly ConstructorInfo c = typeof(BigInteger).GetConstructor(new[] { typeof(byte[]) });

        public BigInteger Value;
        
        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) {
            var ms = new MemoryStream();
            var name = s.FullName + "#static" + s.Statics++;
            var a = Value.ToByteArray();
            f.Serialize(ms, a);
            m.DefineInitializedData(name, ms.GetBuffer(), FieldAttributes.Public);
            new ILInt32 { Value = Value.ToByteArray().Length }.ToIL(s, g, m);
            g.Emit(OpCodes.Newarr, typeof(byte));
            g.Emit(OpCodes.Ldfld, m.GetField(name));
            g.Emit(OpCodes.Call, ia);
            g.Emit(OpCodes.Newobj, c);
        }
        public bool Assignable() => false;
    }

    /// <summary>
    /// 1-byte unsigned integer.
    /// </summary>
    public class ILUInt8 : ILUint {
        public char Value;

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldc_I4, Value);
        public bool Assignable() => false;
    }

    /// <summary>
    /// 2-byte unsigned integer.
    /// </summary>
    public class ILUInt16 : ILUint {
        public ushort Value;

        public static implicit operator ILUInt16(ILInt8 i) => new ILUInt16 { Value = i.Value };
        public static implicit operator ILUInt16(ILUInt8 i) => new ILUInt16 { Value = i.Value };

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldc_I4, Value);
        public bool Assignable() => false;
    }

    /// <summary>
    /// 4-byte unsigned integer.
    /// </summary>
    public class ILUInt32 : ILUint {
        public uint Value;

        public static implicit operator ILUInt32(ILInt8 i) => new ILUInt32 { Value = i.Value };
        public static implicit operator ILUInt32(ILInt16 i) => new ILUInt32 { Value = (uint) i.Value };
        public static implicit operator ILUInt32(ILUInt8 i) => new ILUInt32 { Value = i.Value };
        public static implicit operator ILUInt32(ILUInt16 i) => new ILUInt32 { Value = i.Value };

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldc_I4, Value);
        public bool Assignable() => false;
    }

    /// <summary>
    /// Floating point number.
    /// </summary>
    public class ILFloat32 : ILFloat {
        public float Value;

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldc_R4, Value);
        public bool Assignable() => false;
    }

    /// <summary>
    /// Double precision floating point number.
    /// </summary>
    public class ILFloat64 : ILFloat {
        public double Value;

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldc_R8, Value);
        public bool Assignable() => false;
    }

    /// <summary>
    /// String.
    /// </summary>
    public class ILString : ILConstant {
        public string Value;

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldstr, Value);
        public bool Assignable() => false;
    }

    // TODO: this is bad idea probably?
    /// <summary>
    /// Binary expression.
    /// </summary>
    public class ILBinary : ILValue {
        public ILValue Left;
        public ILValue Right;
        public Action<ILGenerator, ModuleBuilder> ILGen;
        public bool IsAssignable;
        public void SetILToer(Func<ILBinary, Action<ILGenerator, ModuleBuilder>> ilGen) => ILGen = ilGen(this);

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) => ILGen(g, m);
        public bool Assignable() => IsAssignable;
    }

    /// <summary>
    /// Variable (argument or local variable).
    /// </summary>
    public class ILVariable : ILValue {
        public bool IsArgument;
        public short Index;

        public ILVariable(ILScope scope, string name) {
            Index = scope[name];
            IsArgument = false;
            if (Index != -1) {
                // TODO: search _this_ for classes. hopefully I can do it in Cream Puff though
                if (!scope.Arguments.Contains(name))
                    throw new Exception($"Variable {name} is neither a local variable nor an argument");
                IsArgument = true;
            }
        }

        public void ToIL(ILScope s, ILGenerator g, ModuleBuilder m) {
            if (IsArgument) {
                switch (Index) {
                    case 0:
                        g.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        g.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        g.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        g.Emit(OpCodes.Ldarg_3);
                        break;
                    case var i when i <= 255:
                        g.Emit(OpCodes.Ldarg_S, Index);
                        break;
                    default:
                        g.Emit(OpCodes.Ldarg, Index);
                        break;
                }
            }
            switch (Index) {
                case 0:
                    g.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    g.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    g.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    g.Emit(OpCodes.Ldloc_3);
                    break;
                case var i when i <= 255:
                    g.Emit(OpCodes.Ldloc_S, Index);
                    break;
                default:
                    g.Emit(OpCodes.Ldloc, Index);
                    break;
            }
        }
        public bool Assignable() => true;
    }
}
