using System;


namespace LC_Sharp {
    //LC3 simulator class
    public class LC3 {
        public Control control;
        public Processing processing;
		public Memory memory;
		public ushort bus;
		public LC3() {
            control = new Control(this);
            memory = new Memory(this);
            processing = new Processing(this);
		}
        public void DebugPrint() {
            processing.DebugPrint();
            control.DebugPrint();
        }
        public void Fetch() {
            control.gatePC();
            memory.ldMAR();
            memory.memEnR();
            memory.gateMDR();
            control.ldIR();
            processing.pcmux = Processing.PCMUX.inc;
            control.ldPC();
        }
        public void Execute() => Execute(control.ir);
		public void Execute(ushort instruction) {
			//Get the opcode;
			switch(instruction >> 12) {
				case 0b0000:
                    //BR
                    //Fun fact: by default, all unused memory locations are 0x0000, which happens to represent instruction 
                    processing.addr1mux = Processing.ADDR1MUX.pc;
                    processing.addr2mux = Processing.ADDR2MUX.ir9;
                    processing.pcmux = Processing.PCMUX.addrAdd;
                    processing.PrintMux();
                    if((processing.N && (instruction & 0x0800) > 0) ||
                        (processing.Z && (instruction & 0x0400) > 0) ||
                        (processing.P && (instruction & 0x0200) > 0)) {
                        control.ldPC();
                    }
                    Console.WriteLine("Executed BR");
					break;
				case 0b0001:
                    //ADD
                    //Register mode
                    if ((instruction & 0b100000) == 0) {
                        processing.sr1mux = Processing.SR1MUX.ir8_6;
                        processing.sr2mux = Processing.SR2MUX.sr2out;
                        processing.aluk = Processing.ALUK.add;
                        Console.WriteLine($"ALUA: {processing.aluA}");
                        Console.WriteLine($"ALUB: {processing.aluB}");
                        Console.WriteLine($"ALU: {processing.alu}");
                        processing.gateALU();
                        processing.drmux = Processing.DRMUX.ir11_9;
                        processing.ldReg();
                        Console.WriteLine("Executed ADD Register");
                    } else {
                        //Immediate mode
                        processing.sr1mux = Processing.SR1MUX.ir8_6;
                        processing.sr2mux = Processing.SR2MUX.ir5;
                        processing.aluk = Processing.ALUK.add;
                        Console.WriteLine($"ALUA: {processing.aluA}");
                        Console.WriteLine($"ALUB: {processing.aluB}");
                        Console.WriteLine($"ALU: {processing.alu}");
                        processing.gateALU();
                        processing.drmux = Processing.DRMUX.ir11_9;
                        processing.ldReg();
                        Console.WriteLine("Executed ADD Immediate");
                    }
                    break;
                case 0b0010:
                    //LD
                    processing.addr1mux = Processing.ADDR1MUX.pc;
                    processing.addr2mux = Processing.ADDR2MUX.ir9;
                    processing.marmux = Processing.MARMUX.addradd;
                    processing.gateMARMUX();
                    memory.ldMAR();
                    memory.memEnR();
                    memory.gateMDR();
                    processing.drmux = Processing.DRMUX.ir11_9;
                    processing.ldReg();

                    break;
                case 0b0011:
                    //ST
                    processing.addr1mux = Processing.ADDR1MUX.pc;
                    processing.addr2mux = Processing.ADDR2MUX.ir9;
                    processing.marmux = Processing.MARMUX.addradd;
                    processing.gateMARMUX();
                    memory.ldMAR();
                    processing.sr1mux = Processing.SR1MUX.ir11_9;
                    processing.aluk = Processing.ALUK.passthrough;
                    processing.gateALU();
                    memory.ldMDR();
                    memory.memEnW();
                    break;
                case 0b0100:
                    if((instruction & 0x0800) > 0) {
                        control.gatePC();
                        processing.drmux = Processing.DRMUX.b111;
                        processing.ldReg();
                        processing.addr1mux = Processing.ADDR1MUX.pc;
                        processing.addr2mux = Processing.ADDR2MUX.ir11;
                        processing.pcmux = Processing.PCMUX.addrAdd;
                        control.ldPC();
                    } else {
                        control.gatePC();
                        processing.drmux = Processing.DRMUX.b111;
                        processing.ldReg();
                        processing.sr1mux = Processing.SR1MUX.ir8_6;
                        processing.addr1mux = Processing.ADDR1MUX.sr1out;
                        processing.addr2mux = Processing.ADDR2MUX.b0;
                        processing.pcmux = Processing.PCMUX.addrAdd;
                        control.ldPC();
                    }
                    break;
                case 0b0101:
                    //AND
                    if((instruction & 0x0020) != 0) {
                        processing.sr1mux = Processing.SR1MUX.ir8_6;
                        processing.sr2mux = Processing.SR2MUX.ir5;
                        processing.aluk = Processing.ALUK.and;
                        processing.gateALU();
                        processing.drmux = Processing.DRMUX.ir11_9;
                        processing.ldReg();
                    } else {
                        //Register mode
                        processing.sr1mux = Processing.SR1MUX.ir8_6;
                        processing.sr2mux = Processing.SR2MUX.sr2out;
                        processing.aluk = Processing.ALUK.and;
                        processing.gateALU();
                        processing.drmux = Processing.DRMUX.ir11_9;
                        processing.ldReg();
                    }
                    break;
                case 0b0110:
                    //LDR
                    processing.sr1mux = Processing.SR1MUX.ir8_6;
                    processing.addr1mux = Processing.ADDR1MUX.sr1out;
                    processing.addr2mux = Processing.ADDR2MUX.ir6;
                    processing.marmux = Processing.MARMUX.addradd;
                    processing.gateMARMUX();
                    memory.ldMAR();
                    memory.memEnR();
                    memory.gateMDR();
                    processing.ldReg();
                    break;
                case 0b0111:
                    //STR
                    processing.sr1mux = Processing.SR1MUX.ir8_6;
                    processing.addr1mux = Processing.ADDR1MUX.sr1out;
                    processing.addr2mux = Processing.ADDR2MUX.ir6;
                    processing.marmux = Processing.MARMUX.addradd;
                    processing.gateMARMUX();
                    memory.ldMAR();
                    processing.sr1mux = Processing.SR1MUX.ir11_9;
                    processing.aluk = Processing.ALUK.passthrough;
                    processing.gateALU();
                    memory.ldMDR();
                    memory.memEnW();
                    break;
                case 0b1000:
                    //RTI
                    break;
                case 0b1001:
                    //NOT
                    processing.sr1mux = Processing.SR1MUX.ir8_6;
                    processing.aluk = Processing.ALUK.not;
                    processing.gateALU();
                    processing.drmux = Processing.DRMUX.ir11_9;
                    processing.ldReg();
                    break;
                case 0b1010:
                    //LDI
                    processing.addr1mux = Processing.ADDR1MUX.pc;
                    processing.addr2mux = Processing.ADDR2MUX.ir9;
                    processing.marmux = Processing.MARMUX.addradd;
                    processing.gateMARMUX();
                    memory.ldMAR();
                    memory.memEnR();
                    memory.gateMDR();
                    memory.ldMAR();
                    memory.memEnR();
                    memory.gateMDR();
                    processing.drmux = Processing.DRMUX.ir11_9;
                    processing.ldReg();
                    break;
                case 0b1011:
                    //STI
                    processing.addr1mux = Processing.ADDR1MUX.pc;
                    processing.addr2mux = Processing.ADDR2MUX.ir9;
                    processing.marmux = Processing.MARMUX.addradd;
                    processing.gateMARMUX();
                    memory.ldMAR();
                    memory.memEnR();
                    memory.gateMDR();
                    memory.ldMAR();
                    processing.sr1mux = Processing.SR1MUX.ir11_9;
                    processing.aluk = Processing.ALUK.passthrough;
                    processing.gateALU();
                    memory.ldMDR();
                    memory.memEnW();
                    break;
                case 0b1100:
                    //JMP
                    processing.sr1mux = Processing.SR1MUX.ir8_6;
                    processing.addr1mux = Processing.ADDR1MUX.sr1out;
                    processing.addr2mux = Processing.ADDR2MUX.b0;
                    processing.pcmux = Processing.PCMUX.addrAdd;
                    control.ldPC();
                    break;
                case 0b1101:
                    //RESERVED
                    break;
                case 0b1110:
                    //LEA
                    processing.addr1mux = Processing.ADDR1MUX.pc;
                    processing.addr2mux = Processing.ADDR2MUX.ir9;
                    processing.marmux = Processing.MARMUX.addradd;
                    processing.gateMARMUX();
                    processing.drmux = Processing.DRMUX.ir11_9;
                    processing.ldReg();
                    break;
                case 0b1111:
                    //TRAP
                    break;
			}

		}
	}
    
}