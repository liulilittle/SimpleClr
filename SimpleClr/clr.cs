namespace SimpleClr
{
    public static unsafe class clr
    {
        public const byte Nop = 0;
        public const byte Add = 1;
        public const byte Sub = 2;
        public const byte Mul = 3;
        public const byte Div = 4;
        public const byte Dup = 5;
        public const byte Inc = 6;
        public const byte Dec = 7;
        public const byte Ret = 8;

        public const byte Ldc = 10;
        public const byte Ldc_0 = 11;
        public const byte Ldc_1 = 12;
        public const byte Ldc_2 = 13;
        public const byte Ldc_3 = 14;

        public const byte Stloc = 20;
        public const byte Stloc_0 = 21;
        public const byte Stloc_1 = 22;
        public const byte Stloc_2 = 23;
        public const byte Stloc_3 = 24;

        public const byte Ldloc = 30;
        public const byte Ldloc_0 = 31;
        public const byte Ldloc_1 = 32;
        public const byte Ldloc_2 = 33;
        public const byte Ldloc_3 = 34;

        public const byte Ceq = 40;
        public const byte Clt = 41;
        public const byte Cgt = 42;
        public const byte Ble_s = 43;
        public const byte Bge_s = 44;

        public const byte Br_s = 50;
        public const byte Brtrue_s = 51;
        public const byte Brfalse_s = 52;

        public const byte Ldarg = 60;
        public const byte Ldarg_0 = 61;
        public const byte Ldarg_1 = 62;
        public const byte Ldarg_2 = 63;
        public const byte Ldarg_3 = 64;

        public const byte Starg = 70;
        public const byte Starg_0 = 71;
        public const byte Starg_1 = 72;
        public const byte Starg_2 = 73;
        public const byte Starg_3 = 74;

        private static bool IsOverflowOfBoundary(byte* current, byte* ending, int ofs = 0)
        {
            byte* p = current + ofs;
            return p > ending;
        }

        public static byte[] Build(byte[] instructions, int stacksize = 0x0C0)
        {
            Ibuiltins builtins = new builtins_x86();
            builtins.def(stacksize);
            {
                fixed (byte* begin = instructions)
                {
                    int count = instructions.Length;
                    byte* il = begin;
                    byte* ending = unchecked(begin + count);
                    while (il < ending)
                    {
                        byte op = *il++;
                        if (op == Ldc)
                        {
                            int constant = *(int*)il;
                            il += 4;
                            builtins.ldc(constant);
                        }
                        else if (op == Ldc_0) builtins.ldc(0);
                        else if (op == Ldc_1) builtins.ldc(1);
                        else if (op == Ldc_2) builtins.ldc(2);
                        else if (op == Ldc_3) builtins.ldc(3);

                        else if (op == Ldloc) builtins.ldloc(*il++);
                        else if (op == Ldloc_0) builtins.ldloc(0);
                        else if (op == Ldloc_1) builtins.ldloc(1);
                        else if (op == Ldloc_2) builtins.ldloc(2);
                        else if (op == Ldloc_3) builtins.ldloc(3);

                        else if (op == Stloc) builtins.stloc(*il++);
                        else if (op == Stloc_0) builtins.stloc(0);
                        else if (op == Stloc_1) builtins.stloc(1);
                        else if (op == Stloc_2) builtins.stloc(2);
                        else if (op == Stloc_3) builtins.stloc(3);

                        else if (op == Nop) builtins.nop();
                        else if (op == Add) builtins.add();
                        else if (op == Sub) builtins.sub();
                        else if (op == Mul) builtins.mul();
                        else if (op == Div) builtins.div();
                        else if (op == Dup) builtins.dup();
                        else if (op == Inc) builtins.inc();
                        else if (op == Dec) builtins.dec();
                        else if (op == Ret) builtins.ret();

                        else if (op == Clt || op == Ceq || op == Cgt)
                        {
                            if (!IsOverflowOfBoundary(il, ending) && (*il == Brfalse_s || *il == Brtrue_s))
                            {
                                bool condition = unchecked(*il++ == Brtrue_s);
                                int comparison = 0;
                                switch (op)
                                {
                                    case Clt:
                                        comparison = -1;
                                        break;
                                    case Cgt:
                                        comparison = 1;
                                        break;
                                }
                                int position = *(int*)il;
                                il += 4;
                                builtins.brcmpc(position, comparison, condition);
                            }
                            else if (op == Ceq) builtins.ceq();
                            else if (op == Clt) builtins.clt();
                            else if (op == Cgt) builtins.cgt();
                        }
                        else if (op == Ble_s || op == Bge_s)
                        {
                            int position = *(int*)il;
                            il += 4;
                            builtins.bgeorble(position, op == Bge_s);
                        }
                        else if (op == Br_s)
                        {
                            int position = *(int*)il;
                            il += 4;
                            builtins.br(position);
                        }
                        else if (op == Brfalse_s || op == Brtrue_s)
                        {
                            int position = *(int*)il;
                            il += 4;
                            builtins.br(position, op == Brtrue_s);
                        }

                        else if (op == Ldarg) builtins.ldarg(*il++);
                        else if (op == Ldarg_0) builtins.ldarg(0);
                        else if (op == Ldarg_1) builtins.ldarg(1);
                        else if (op == Ldarg_2) builtins.ldarg(2);
                        else if (op == Ldarg_3) builtins.ldarg(3);

                        else if (op == Starg) builtins.starg(*il++);
                        else if (op == Starg_0) builtins.starg(0);
                        else if (op == Starg_1) builtins.starg(1);
                        else if (op == Starg_2) builtins.starg(2);
                        else if (op == Starg_3) builtins.starg(3);
                    }
                }
            }
            return builtins.ilcode();
        }
    }
}