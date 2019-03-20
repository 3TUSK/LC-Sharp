using System;

namespace LC_Sharp {
    public class Control {
        private LC3 lc3;
        public short pc { get; private set; }
        public short ir { get; private set; }
        public Control(LC3 lc3) {
            this.lc3 = lc3;
            pc = 0x3000;
            ir = 0;
        }
		public void setPC(short pc) => this.pc = pc;
        public void ldPC() => pc = lc3.processing.pcmuxout;
        public void gatePC() => lc3.bus = pc;
        public void ldIR() => ir = lc3.bus;
        
        internal void DebugPrint() {
            Console.WriteLine($"PC: 0x{pc.ToString("X")}");
            Console.WriteLine($"IR: 0x{ir.ToString("X")}");
        }
    }
    
}