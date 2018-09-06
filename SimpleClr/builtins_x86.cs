namespace SimpleClr
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    /// <summary>
    /// builtin-x86 native-platform runtime assembler for liulilittle
    /// </summary>
    public unsafe class builtins_x86 : Ibuiltins
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(byte* lpAddress, int dwSize, int flNewProtect, out int lpflOldProtect);

        private const int NULL = 0;
        private const int PAGE_EXECUTE_READWRITE = 64;

        // Add executable code flag over Win32k system DEP protection
        public static bool AddExecuteReadWriteFlag(byte* ptr, int size)
        {
            int outValueProtect;
            return VirtualProtect(ptr, size, PAGE_EXECUTE_READWRITE, out outValueProtect);
        }

        private byte[] instructions = null;
        private BinaryWriter emit = null;
        private IList<int> positions = null;
        private IList<Label> labels = null;

        private class Label
        {
            public int position;
            public int emit_seek;
            public Action orientation;
        }

        public builtins_x86()
        {
            this.emit = new BinaryWriter(new MemoryStream());
            this.positions = new List<int>();
            this.labels = new List<Label>();
        }

        public static bool MarkShellCode(byte[] buffer, int ofs, int count)
        {
            fixed (byte* pinned = &buffer[ofs])
            {
                return AddExecuteReadWriteFlag(pinned, count);
            }
        }

        private void SetPositionPoint()
        {
            Stream stream = emit.BaseStream;
            int position = Convert.ToInt32(stream.Position);
            this.positions.Add(position);
        }

        private int GetPositionPoint(int position)
        {
            return this.positions[position];
        }

        private long GetEmitPosition64()
        {
            return emit.BaseStream.Position;
        }

        private int GetEmitPosition()
        {
            return Convert.ToInt32(GetEmitPosition64());
        }

        private void Closure()
        {
            MemoryStream stream = (MemoryStream)emit.BaseStream;
            long position = stream.Position;
            for (int i = 0; i < labels.Count; i++)
            {
                Label label = labels[i];
                label.orientation();

                stream.Seek(0, SeekOrigin.Begin);
                stream.Seek(position, SeekOrigin.Current);
            }
            using (emit)
            {
                this.instructions = stream.ToArray();
                MarkShellCode(this.instructions, 0, this.instructions.Length);
            }
        }

        public void ret()
        {
            this.SetPositionPoint();
            // pop         eax
            emit.Write((byte)0x58);
            // pop         edi
            // pop         esi
            // pop         ebx
            // mov         esp,ebp
            // pop         ebp
            // ret
            emit.Write((byte)95);
            emit.Write((byte)94);
            emit.Write((byte)91);
            emit.Write((byte)139);
            emit.Write((byte)229);
            emit.Write((byte)93);
            emit.Write((byte)195);

            this.Closure();
        }

        public void def(int stacksize)
        {
            // push        ebp
            // mov         ebp,esp
            // sub         esp,0C0h
            // push        ebx
            // push        esi
            // push        edi
            // lea         edi,[ebp-0C0h]
            // mov         ecx,30h
            // mov         eax,0CCCCCCCCh
            // rep stos    dword ptr es:[edi]
            emit.Write((byte)85);
            emit.Write((byte)139);
            emit.Write((byte)236);
            emit.Write((byte)129);
            emit.Write((byte)236);
            emit.Write(stacksize);
            emit.Write((byte)83);
            emit.Write((byte)86);
            emit.Write((byte)87);
            emit.Write((byte)141);
            emit.Write((byte)189);
            emit.Write((byte)64);
            emit.Write((byte)255);
            emit.Write((byte)255);
            emit.Write((byte)255);
            emit.Write((byte)185);
            emit.Write((byte)48);
            emit.Write((byte)0);
            emit.Write((byte)0);
            emit.Write((byte)0);
            emit.Write((byte)184);
            emit.Write((byte)204);
            emit.Write((byte)204);
            emit.Write((byte)204);
            emit.Write((byte)204);
            emit.Write((byte)243);
            emit.Write((byte)171);
        } 

        public void stloc(int slot)
        {
            this.SetPositionPoint();
            // pop dword ptr[ebp-8]
            emit.Write((byte)0x8F);
            emit.Write((byte)0x45);
            emit.Write((byte)(0xF8 - (slot * 8)));
        }

        public void ldc(int value)
        {
            this.SetPositionPoint();
            // push 0
            emit.Write((byte)0x68);
            emit.Write(value);
        }

        public byte[] ilcode()
        {
            return this.instructions;
        }

        public void ldloc(int slot)
        {
            this.SetPositionPoint();
            // push dword ptr[esp-8]
            emit.Write((byte)0xFF);
            emit.Write((byte)0x75);
            emit.Write((byte)(0xF8 - (slot * 8)));
        }

        public void nop()
        {
            this.SetPositionPoint();
            // nop
            emit.Write((byte)0x90);
        }

        public void add()
        {
            this.SetPositionPoint();
            // pop eax
            // add dword ptr[esp],eax
            emit.Write((byte)0x58);
            emit.Write((byte)0x01);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);
        }

        public void sub()
        {
            this.SetPositionPoint();
            // pop eax
            // sub dword ptr[esp],eax
            emit.Write((byte)0x58);
            emit.Write((byte)0x29);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);
        }

        public void mul()
        {
            this.SetPositionPoint();
            // pop eax
            // imul eax,dword ptr[esp]
            // mov dword ptr[esp],eax
            emit.Write((byte)0x58);

            emit.Write((byte)0x0F);
            emit.Write((byte)0xAF);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);

            emit.Write((byte)0x89);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);
        }

        public void div()
        {
            this.SetPositionPoint();
            // mov eax,dword ptr[esp+04h]
            // cdq
            // idiv dword ptr[esp]
            // mov dword ptr[esp+04h],eax
            // pop eax
            emit.Write((byte)0x8B);
            emit.Write((byte)0x44);
            emit.Write((byte)0x24);
            emit.Write((byte)0x04);

            emit.Write((byte)0x99);

            emit.Write((byte)0xF7);
            emit.Write((byte)0x3C);
            emit.Write((byte)0x24);

            emit.Write((byte)0x89);
            emit.Write((byte)0x44);
            emit.Write((byte)0x24);
            emit.Write((byte)0x04);

            emit.Write((byte)0x58);
        }

        public void dup()
        {
            this.SetPositionPoint();
            // push dword ptr[esp]
            emit.Write((byte)0xFF);
            emit.Write((byte)0x34);
            emit.Write((byte)0x24);
        }

        public void ceq()
        {
            this.SetPositionPoint();
            // pop eax
            // cmp eax,dword ptr[esp]
            // jne 09h
            // mov dword ptr[esp],1
            // jmp 07h
            // mov dword ptr[esp],0
            emit.Write((byte)0x58);

            emit.Write((byte)0x3B);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);

            emit.Write((byte)0x75);
            emit.Write((byte)0x09);

            emit.Write((byte)0xC7);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);
            emit.Write(0x01);

            emit.Write((byte)0xEB);
            emit.Write((byte)0x07);

            emit.Write((byte)0xC7);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);
            emit.Write(0x00);
        }

        public void clt()
        {
            this.SetPositionPoint();
            // pop eax
            // cmp dword ptr[esp],eax
            // jge 09h 
            // mov dword ptr[esp],1
            // jmp 07h
            // mov dword ptr[esp],0
            emit.Write((byte)0x58);

            emit.Write((byte)0x39);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);

            emit.Write((byte)0x7D);
            emit.Write((byte)0x09);

            emit.Write((byte)0xC7);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);
            emit.Write(0x01);

            emit.Write((byte)0xEB);
            emit.Write((byte)0x07);

            emit.Write((byte)0xC7);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);
            emit.Write(0x00);
        }

        public void cgt()
        {
            this.SetPositionPoint();
            // pop eax
            // cmp dword ptr[esp],eax
            // jle 09h 
            // mov dword ptr[esp],1
            // jmp 07h
            // mov dword ptr[esp],0
            emit.Write((byte)0x58);

            emit.Write((byte)0x39);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);

            emit.Write((byte)0x7E);
            emit.Write((byte)0x09);

            emit.Write((byte)0xC7);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);
            emit.Write(0x01);

            emit.Write((byte)0xEB);
            emit.Write((byte)0x07);

            emit.Write((byte)0xC7);
            emit.Write((byte)0x04);
            emit.Write((byte)0x24);
            emit.Write(0x00);
        }

        public void inc()
        {
            this.incdec(true);
        }

        private void incdec(bool inc)
        {
            this.SetPositionPoint();
            emit.Write((byte)0xFF);
            if (inc)
            {
                // inc dword ptr[esp]
                emit.Write((byte)0x04);
            }
            else
            {
                // dec dword ptr[esp]
                emit.Write((byte)0x0C);
            }
            emit.Write((byte)0x24);
        }

        public void dec()
        {
            this.incdec(false);
        }

        public void br(int position)
        {
            this.SetPositionPoint();

            Label label = new Label();
            label.orientation = () =>
            {
                emit.Seek(label.emit_seek + 1, SeekOrigin.Begin);
                int r = this.GetPositionPoint(position);
                r -= (label.emit_seek + 0x05);
                emit.Write(r);
            };
            label.position = position;
            label.emit_seek = this.GetEmitPosition();
            labels.Add(label);

            // JMP 00000000
            emit.Write((byte)0xE9);
            emit.Write(0x00);
        }

        public void br(int position, bool condition)
        {
            this.SetPositionPoint();
            // pop eax
            // cmp eax,condition
            // je  00h
            emit.Write((byte)0x58);
            emit.Write((byte)0x83);
            emit.Write((byte)0xF8);
            emit.Write((byte)(condition ? 1 : 0));

            Label label = new Label();
            label.orientation = () =>
            {
                emit.Seek(label.emit_seek + 2, SeekOrigin.Begin);
                int r = this.GetPositionPoint(position);
                r -= (label.emit_seek + 0x06);
                emit.Write(r);
            };
            label.position = position;
            label.emit_seek = this.GetEmitPosition();
            labels.Add(label);

            emit.Write((byte)0x0F);
            emit.Write((byte)0x84);
            emit.Write(0x00);
        }

        public void brcmpc(int position, int comparison, bool condition)
        {
            this.SetPositionPoint();
            // pop edx
            // pop eax
            // cmp eax,edx
            emit.Write((byte)0x5A);
            emit.Write((byte)0x58);
            emit.Write((byte)0x3B);
            emit.Write((byte)0xC2);

            Label label = new Label();
            label.orientation = () =>
            {
                emit.Seek(label.emit_seek + 2, SeekOrigin.Begin);
                int r = this.GetPositionPoint(position);
                r -= (label.emit_seek + 0x06);
                emit.Write(r);
            };
            label.position = position;
            label.emit_seek = this.GetEmitPosition();
            labels.Add(label);

            emit.Write((byte)0x0F);
            if (comparison > 0)
            {
                if (condition)
                {
                    // jg 00h
                    emit.Write((byte)0x8F);
                }
                else
                {
                    // jle 00h
                    emit.Write((byte)0x8E);
                }
            }
            else if (comparison == 0)
            {
                // je 00h
                emit.Write((byte)0x84);
            }
            else if (comparison < 0)
            {
                if (condition)
                {
                    // jl 00h
                    emit.Write((byte)0x8C);
                }
                else
                {
                    // jge 00h
                    emit.Write((byte)0x8D);
                }
            }
            else
            {
                throw new InvalidProgramException();
            }
            emit.Write(0x00);
        }

        public void bgeorble(int position, bool greaterthanorequal)
        {
            this.SetPositionPoint();
            // pop edx
            // pop eax
            // cmp eax,edx
            emit.Write((byte)0x5A);
            emit.Write((byte)0x58);
            emit.Write((byte)0x3B);
            emit.Write((byte)0xC2);

            Label label = new Label();
            label.orientation = () =>
            {
                emit.Seek(label.emit_seek + 2, SeekOrigin.Begin);
                int r = this.GetPositionPoint(position);
                r -= (label.emit_seek + 0x06);
                emit.Write(r);
            };
            label.position = position;
            label.emit_seek = this.GetEmitPosition();
            labels.Add(label);

            emit.Write((byte)0x0F);
            if (greaterthanorequal) // >=
            {
                // jge 00h
                emit.Write((byte)0x8D);
            }
            else
            {
                // jle 00h
                emit.Write((byte)0x8E);
            }
        }
    }
}
