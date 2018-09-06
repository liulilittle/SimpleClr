namespace SimpleClr
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    public static class program
    {
        public delegate int Bootloader();

        public unsafe static void Main(string[] args)
        {
            byte[] il =
            {
                clr.Nop,
                clr.Ldc_0, // int i = 0;
                clr.Stloc_0,
                clr.Ldloc_0, // IL_003
                clr.Inc,
                clr.Dup,
                clr.Stloc_0,
                clr.Ldc,
                0, 0, 0, 0, // constant
                clr.Clt,
                clr.Brtrue_s, // i < 1000 then goto IL_003
                3, 0, 0, 0, // label
                clr.Ldloc_0,
                clr.Ret,
            };
            *(int*)Marshal.UnsafeAddrOfPinnedArrayElement(il, 8) = 100000000; // 循环一亿次
            byte[] instructions = clr.Build(il);
            // 固定托管内存请求不要被移动设为非托管内存
            IntPtr address = GCHandle.Alloc(instructions, GCHandleType.Pinned).AddrOfPinnedObject();
            Bootloader bootloader = (Bootloader)Marshal.GetDelegateForFunctionPointer(address, typeof(Bootloader));
            // 把即时编译的函数转换成 bootloader
            int eax = 0;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            {
                eax = bootloader(); // 引导即时编译的函数执行
            }
            stopwatch.Stop();
            Console.WriteLine("ticks={0}, eax={1}", stopwatch.ElapsedMilliseconds, eax);
            Console.ReadKey(false);
        }
    }
}
