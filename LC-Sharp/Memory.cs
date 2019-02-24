using System.Collections.Generic;

namespace LC_Sharp {
    public class Memory {
		private LC3 lc3;
		private short mar, mdr;
		private Dictionary<short, short> mem;    //Lazily initialized memory (we only create entries right when we set or get)
		public Memory(LC3 lc3) {
			this.lc3 = lc3;
			mem = new Dictionary<short, short>();
		}
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
		public void memEnW() => mem[mar] = mdr;
        public void Write(short mar, short mdr) => mem[mar] = mdr;
		public short Read(short mar) {
			if(!mem.ContainsKey(mar)) {
				mem[mar] = 0;
			}
			return mem[mar];
		}
	}
    
}