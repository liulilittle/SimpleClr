namespace SimpleClr
{
    public interface Ibuiltins
    {
        void ret();

        void def(int stacksize);

        void ldc(int value);

        void ldloc(int slot);

        void stloc(int slot);

        byte[] ilcode();

        void nop();

        void add();

        void sub();

        void mul();

        void div();

        void dup();

        void ceq();

        void clt();

        void cgt();

        void inc();

        void dec();

        void br(int position);

        void br(int position, bool condition);

        void brcmpc(int position, int comparison, bool condition);

        void bgeorble(int position, bool greaterthanorequal);

        void starg(int slot);

        void ldarg(int slot);

        void localloc();
    }
}
