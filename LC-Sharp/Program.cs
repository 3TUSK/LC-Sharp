using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC_Sharp {
	class Program {
		public static void Main(string[] args) {
			
		}
	}

	//GUI handler class
	class Window {

	}

	//LC3 simulator class
	public class LC3 {

        public Control control;
		public Memory memory;
		public ushort bus;
		public LC3() {

		}
        public void Fetch() {
            control.gatePC();
            memory.ldMAR();
            memory.memEnR();
            memory.gateMDR();
            control.ldIR();
        }
        public void Run() {
            Fetch();
            control.pcmux = Control.PCMUX.inc;
            control.ldPC();
            Execute(control.ir);
        }
		public void Execute(ushort instruction) {
			//Get the opcode;
			switch(instruction >> 12) {
				case 0b0000:
                    control.addr1mux = Control.ADDR1MUX.pc;
                    control.addr2mux = Control.ADDR2MUX.ir9;
                    control.pcmux = Control.PCMUX.addrAdd;
                    if((control.N && (instruction & 0x0800) > 0) ||
                        (control.Z && (instruction & 0x0400) > 0) ||
                        (control.P && (instruction & 0x0200) > 0)) {
                        control.ldPC();
                    }
					break;
				case 0b0001:
					break;
			}
		}
		//We move data by simulating the assertion of control signals
		public void Assert(Signals s) {
			switch(s) {
				case Signals.LD_MAR:
					memory.ldMAR();
					break;
				case Signals.LD_MDR:
					memory.ldMDR();
					break;

				case Signals.GATE_MAR:
					memory.gateMAR();
					break;
				case Signals.GATE_MDR:
					memory.gateMDR();
					break;
			}
		}
	}
    public class Control {
        public enum ADDR1MUX {
            sr1out,
            pc
        }
        public ADDR1MUX addr1mux;
        public ushort addr1 => addr1mux == ADDR1MUX.sr1out ? sr1out : pc;
        public enum ADDR2MUX {
            ir11,
            ir9,
            ir6,
            b0
        }
        public ADDR2MUX addr2mux;
        public ushort addr2 =>
            (ushort)(
                addr2mux == ADDR2MUX.ir11 ? (ir << 5) >> 5 :
                addr2mux == ADDR2MUX.ir9 ? (ir << 7) >> 7 :
                addr2mux == ADDR2MUX.ir6 ? (ir << 10) >> 10 :
                0);
        public ushort addradd => (ushort) (addr1 + addr2);

        public enum PCMUX {
            bus,
            addrAdd,
            inc,
        }
        public PCMUX pcmux;
        public ushort pcmuxout => (ushort) (
            pcmux == PCMUX.bus ? lc3.bus :
            pcmux == PCMUX.addrAdd ? addradd :
            pc + 1);
            

        public enum SR1MUX {
            ir8_6,
            ir11_9
        }
        public SR1MUX sr1mux;
        public ushort sr1out => registers[
            sr1mux == SR1MUX.ir8_6 ? (ir >> 6) << 6 :
            (ir >> 9) << 9
            ];

        private ushort[] registers;
        private LC3 lc3;
        private ushort pc;
        public ushort ir { get; private set; }
        public bool N { get; private set; }
        public bool Z { get; private set; }
        public bool P { get; private set; }
        public Control(LC3 lc3) {
            this.lc3 = lc3;
            registers = new ushort[8];
        }
        public void UpdateCC(ushort i) {
            N = i < 0;
            Z = i == 0;
            P = i > 0;
        }
        public void ldPC() => pc = pcmuxout;
        public void gatePC() => lc3.bus = pc;
        public void ldIR() => ir = lc3.bus;
    }
	enum Signals {
		LD_MAR, LD_MDR,
		GATE_MAR, GATE_MDR,
	}
	class Memory {
		private LC3 lc3;
		private ushort mar, mdr;
		private Dictionary<ushort, ushort> mem;    //Lazily initialized memory (we only create entries right when we set or get)

		public Memory(LC3 lc3) {
			this.lc3 = lc3;
			mem = new Dictionary<ushort, ushort>();
		}
		//Control signal methods
		public void ldMAR() => mar = lc3.bus;
		public void ldMDR() => mdr = lc3.bus;
		public void gateMAR() => lc3.bus = mar;
		public void gateMDR() => lc3.bus = mdr;
		public void memEnR() {
			if(mem.ContainsKey(mar)) {
				mdr = mem[mar];
			} else {
				//Lazily initialize the address
				mdr = mem[mar] = 0;
			}
		}
		public void memEnW() {
			mem[mar] = mdr;
		}
	}
    
}