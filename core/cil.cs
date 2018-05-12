using CreamPuff;
using System.Reflection.Emit;

#pragma warning disable IDE1006 // Naming Styles

namespace core {
    public class cil {
        // TODO: normal execution - we'll need a stack and dynammic assembly loading

        public class ToIL {
            // TODO: some of these can be folded, but this probably shouldn't be default behavior

            public void add(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Add);

            public void add(ILValue l, ILValue r, ILScope s, ILGenerator g, ModuleBuilder m) {
                l.ToIL(s, g, m);
                r.ToIL(s, g, m);
                g.Emit(OpCodes.Add);
            }

            public void sub(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Sub);

            public void sub(ILValue l, ILValue r, ILScope s, ILGenerator g, ModuleBuilder m) {
                l.ToIL(s, g, m);
                r.ToIL(s, g, m);
                g.Emit(OpCodes.Sub);
            }

            public void mul(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Mul);

            public void mul(ILValue l, ILValue r, ILScope s, ILGenerator g, ModuleBuilder m) {
                l.ToIL(s, g, m);
                r.ToIL(s, g, m);
                g.Emit(OpCodes.Mul);
            }

            public void div(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Div);

            public void div(ILValue l, ILValue r, ILScope s, ILGenerator g, ModuleBuilder m) {
                l.ToIL(s, g, m);
                r.ToIL(s, g, m);
                g.Emit(OpCodes.Div);
            }

            public void and(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.And);

            public void and(ILValue l, ILValue r, ILScope s, ILGenerator g, ModuleBuilder m) {
                l.ToIL(s, g, m);
                r.ToIL(s, g, m);
                g.Emit(OpCodes.And);
            }

            public void or(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Or);

            public void or(ILValue l, ILValue r, ILScope s, ILGenerator g, ModuleBuilder m) {
                l.ToIL(s, g, m);
                r.ToIL(s, g, m);
                g.Emit(OpCodes.Or);
            }

            public void xor(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Xor);

            public void xor(ILValue l, ILValue r, ILScope s, ILGenerator g, ModuleBuilder m) {
                l.ToIL(s, g, m);
                r.ToIL(s, g, m);
                g.Emit(OpCodes.Xor);
            }

            public void ceq(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ceq);

            public void ceq(ILValue l, ILValue r, ILScope s, ILGenerator g, ModuleBuilder m) {
                l.ToIL(s, g, m);
                r.ToIL(s, g, m);
                g.Emit(OpCodes.Ceq);
            }

            public void clt(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Clt);

            public void clt(ILValue l, ILValue r, ILScope s, ILGenerator g, ModuleBuilder m) {
                l.ToIL(s, g, m);
                r.ToIL(s, g, m);
                g.Emit(OpCodes.Clt);
            }

            public void cgt(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Cgt);

            public void cgt(ILValue l, ILValue r, ILScope s, ILGenerator g, ModuleBuilder m) {
                l.ToIL(s, g, m);
                r.ToIL(s, g, m);
                g.Emit(OpCodes.Cgt);
            }

            public void not(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Not);

            public void not(ILValue i, ILScope s, ILGenerator g, ModuleBuilder m) {
                i.ToIL(s, g, m);
                g.Emit(OpCodes.Not);
            }

            public void neg(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Neg);

            public void neg(ILValue i, ILScope s, ILGenerator g, ModuleBuilder m) {
                i.ToIL(s, g, m);
                g.Emit(OpCodes.Neg);
            }

            // TODO: allow arbitrary precision ints as well
            public void ldloc(ILInt16 i, ILScope s, ILGenerator g, ModuleBuilder m) {
                switch (i.Value) {
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
                    case var n when n <= 255:
                        g.Emit(OpCodes.Ldloc_S, i.Value);
                        break;
                    default:
                        g.Emit(OpCodes.Ldloc, i.Value);
                        break;
                }
            }

            public void ldarg(ILInt16 i, ILScope s, ILGenerator g, ModuleBuilder m) {
                switch (i.Value) {
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
                    case var n when n <= 255:
                        g.Emit(OpCodes.Ldarg_S, i.Value);
                        break;
                    default:
                        g.Emit(OpCodes.Ldarg, i.Value);
                        break;
                }
            }

            public void ldstr(ILString s, ILScope sc, ILGenerator g, ModuleBuilder m) => s.ToIL(sc, g, m);

            public void ldind(ILNumber i, ILScope s, ILGenerator g, ModuleBuilder m) => i.ToIL(s, g, m);

            public void ldnull(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldnull);

            public void ldlen(ILScope s, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldlen);

            public void ldfld(ILString s, ILScope sc, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldfld, s.Value);

            public void ldsfld(ILString s, ILScope sc, ILGenerator g, ModuleBuilder m) => g.Emit(OpCodes.Ldsfld, s.Value);
        }
    }
}
