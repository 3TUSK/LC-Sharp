using System;

namespace LC_Sharp {
    public class Processing {
        private LC3 lc3;
        private ushort pc => lc3.control.pc;
        private ushort ir => lc3.control.ir;

        public enum MARMUX {
            addradd,
            ir8
        }
        public MARMUX marmux;
        public ushort marmuxout => (ushort)(
            marmux == MARMUX.addradd ? lc3.processing.addradd :
            lc3.bus
            );
        public void gateMARMUX() => lc3.bus = marmuxout;

        public bool N { get; private set; }
        public bool Z { get; private set; } = true;
        public bool P { get; private set; }

        public short[] registers = new short[8];

        public Processing(LC3 lc3) {
            this.lc3 = lc3;
        }
        public enum PCMUX {
            bus,
            addrAdd,
            inc,
        }
        public PCMUX pcmux;
        public ushort pcmuxout => (ushort)(
            pcmux == PCMUX.bus ? lc3.bus :
            pcmux == PCMUX.addrAdd ? addradd :
            pc + 1);
        public enum SR1MUX {
            ir8_6,
            ir11_9
        }
        public ushort sr1 => (ushort) (
            sr1mux == SR1MUX.ir8_6 ? ((ir & 0b111000000) >> 6) :
            ((ir & 0b0000111000000000) >> 9));
        public SR1MUX sr1mux;
        public short sr1out => registers[sr1];
        public short sr2out => registers[ir & 0x0007]; //always last 3 bits
        public enum DRMUX {
            ir11_9,
            b111
        }
        public DRMUX drmux;
        private ushort dr => (ushort)(drmux == DRMUX.ir11_9 ? ((ir & 0x0E00) >> 8) : 7);
        public enum ADDR1MUX {
            sr1out,
            pc
        }
        public ADDR1MUX addr1mux;
        private short addr1 => addr1mux == ADDR1MUX.sr1out ? sr1out : (short) pc;
        public enum ADDR2MUX {
            ir11,
            ir9,
            ir6,
            b0
        }
        public ADDR2MUX addr2mux;
        private short addr2 =>
            (short)(
                addr2mux == ADDR2MUX.ir11 ? ((short)(ir & 0b111_1111_1111)).signExtend(11) :
                addr2mux == ADDR2MUX.ir9 ? ((short)(ir & 0b1_1111_1111)).signExtend(9) :
                addr2mux == ADDR2MUX.ir6 ? ((short)(ir & 0b11_1111)).signExtend(6) :
                0);
        public ushort addradd => (ushort)(addr1 + addr2);
        public short aluA => sr1out;
        public enum SR2MUX {
            ir5,
            sr2out
        }
        public SR2MUX sr2mux;
        public short aluB => (short)(
            sr2mux == SR2MUX.ir5 ? ((short)(ir & 0x1F)).signExtend(5) :
            sr2out);
        public enum ALUK {
            add, and, not, passthrough
        }
        public ALUK aluk;
        public ushort alu => (ushort)(
            aluk == ALUK.add ? aluA + aluB :
            aluk == ALUK.and ? aluA & aluB :
            aluk == ALUK.not ? ~aluA :
            aluA
            );
        public void PrintMux() {
            Console.WriteLine($"Addr1Mux: {addr1mux.ToString()}");
            Console.WriteLine($"Addr1: 0x{addr1.ToString("X")}");
            Console.WriteLine($"Addr2Mux: {addr2mux.ToString()}");
            Console.WriteLine($"Addr2: 0x{addr2.ToString("X")}");
            Console.WriteLine($"AddrAdd: 0x{addradd.ToString("X")}");
            Console.WriteLine($"PCMux: 0x{pcmuxout.ToString("X")}");
        }
        public void ldReg() {
            short n = (short) lc3.bus;
            registers[dr] = n;
            N = n < 0;
            Z = n == 0;
            P = n > 0;
        }
        public void gateALU() => lc3.bus = alu;

        public void DebugPrint() {
            Console.WriteLine($"R0: {registers[0]}");
            Console.WriteLine($"R1: {registers[1]}");
            Console.WriteLine($"R2: {registers[2]}");
            Console.WriteLine($"R3: {registers[3]}");
            Console.WriteLine($"R4: {registers[4]}");
            Console.WriteLine($"R5: {registers[5]}");
            Console.WriteLine($"R6: {registers[6]}");
            Console.WriteLine($"R7: {registers[7]}");
            Console.WriteLine($"N: {N}");
            Console.WriteLine($"Z: {Z}");
            Console.WriteLine($"P: {P}");
        }
    }
    
}