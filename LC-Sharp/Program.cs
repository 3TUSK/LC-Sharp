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
	class LC3 {

		Memory memory;
		public ushort bus;
		public LC3() {

		}

		public void Execute(ushort instruction) {
			//Get the opcode;
			switch(instruction >> 12) {
				case 0b0000:
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
				mem[mar] = 0;
				mdr = 0;
			}
		}
		public void memEnW() {
			mem[mar] = mdr;
		}
	}

}
