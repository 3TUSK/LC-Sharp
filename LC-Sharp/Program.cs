﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC_Sharp {
	class Program {
        //Note to self: do not use left shift to remove left bits since operands get converted to ints first
		public static void Main(string[] args) {

            //Console.WriteLine($"0b10000 = {(ushort)0b10000} => {((ushort) (0b10000)).ToSigned(5)}");
            //Console.ReadLine();

            //Console.WriteLine(((0b0000_111_0_0000_0111 << 99) >> 99).ToString("X"));
            //Console.ReadLine();
            var c = new LC3();
            var a = new Assembler(c);
            a.Label(0x2FF0, "TEST");
            a.AssembleToPC("BRnzp TEST");
            a.DissembleToPC();
            //c.memory.WriteToMemory(0x3000, 0b0000_010_0_0000_0111);
            c.Fetch();
            c.DebugPrint();
            //Console.WriteLine($"Assembled: {a.DissembleIR()}");

            c.Execute();
            c.DebugPrint();
            Console.ReadLine();
		}
	}

	//GUI handler class
	class Window {

	}
    public class Assembler {
        public Dictionary<string, ushort> labels { get; private set; } //labels
        public Dictionary<ushort, string> labelsReverse { get; private set; } //reverse lookup labels
        LC3 lc3;
        ushort line;
        public Assembler(LC3 lc3) {
            this.lc3 = lc3;
            labels = new Dictionary<string, ushort>();
            labelsReverse = new Dictionary<ushort, string>();
        }
        public bool Register(string code, ushort result) {
            if(code.Length == 2 && code[0] == 'R' && code[1] >= '0' && code[1] <= '7') {
                result = 8;
                return false;
            }
            return false;
        }
        public void AssembleToPC(string instruction) {
            line = 1;
            lc3.memory.WriteToMemory(lc3.control.pc, (ushort)Instruction(lc3.control.pc, instruction));
        }
        public string DissembleToPC() {
            return Dissemble((ushort)lc3.control.pc, lc3.memory.Read(lc3.control.pc));
        }
        public string DissembleIR() {
            return Dissemble((ushort) (lc3.control.pc - 1), lc3.control.ir);
        }


        public void Line(string line) {
            if(Instruction(lc3.control.pc, line) is ushort u) {
                lc3.memory.WriteToMemory((ushort) (lc3.control.pc - 1), u);
            } else {
                var parts = line.Split(' ');
                var label = parts[0];
                Label(lc3.control.pc, label);
                if(parts.Length >= 2) {
                    Pseudo(string.Join(" ", parts.Skip(1)));
                }
            }
        }
        public void Label(ushort pc, string label) {
            ushort location = (ushort)(pc - 1); //pc is 1 ahead of this location
            labels[label] = location;
            labelsReverse[location] = label;
        }
        public void Pseudo(string line) {
            throw new NotImplementedException();
        }
        /**
         * <summary>Attempts to assemble a single instruction string into ushort format.</summary>
         * <param name="pc">The expected value of the PC when this instruction should be executed</param>
         * <param name="instruction">The instruction string to assemble</param>
         * <returns>If <paramref name="instruction"/> names an instruction and is well-formed, then returns the assembled form. If <paramref name="instruction"/> does not name an instruction, returns null</returns>
         * <exception cref="Exception">Throws an exception if the string names an instruction but is not well-formed.</exception>
         * */
        public ushort? Instruction(ushort pc, string instruction) {
            var args = instruction.Split(' ');
            ushort? result = 0;
            switch (args[0]) {
                case var br when br.StartsWith("BR") && br.Length <= 5:
                    bool n = false, z = false, p = false;
                    foreach(char c in br.Substring(2)) {
                        switch(c) {
                            case 'n':
                                if(n) {
                                    throw new Exception($"Line {line}: Repeated condition code 'n' in {instruction}");
                                } else {
                                    n = true;
                                }
                                break;
                            case 'z':
                                if (z) {
                                    throw new Exception($"Line {line}: Repeated condition code 'z' in {instruction}");
                                } else {
                                    z = true;
                                }
                                break;
                            case 'p':
                                if (p) {
                                    throw new Exception($"Line {line}: Repeated condition code 'p' in {instruction}");
                                } else {
                                    p = true;
                                }
                                break;
                        }
                    }
                    result |= (ushort)(n ? 0x0800 : 0);
                    result |= (ushort)(z ? 0x0400 : 0);
                    result |= (ushort)(p ? 0x0200 : 0);
                    if (args.Length == 2) {
                        var label = args[1];
                        if(labels.TryGetValue(label, out ushort destination)) {
                            Console.WriteLine($"Destination: {destination}");

                            short offset = (short) (destination - pc);
                            if(offset < -0b100000000 || offset > 0b011111111) {
                                throw new Exception($"Line {line}: Destination offset {offset} overflows 9-bit integer in {instruction}");
                            } else {
                                result |= (ushort) (offset & 0b111111111);
                            }
                        } else {
                            throw new Exception($"Line {line}: Unknown destination label {label} in {instruction}");
                        }
                    } else {
                        throw new Exception($"Line {line}: Expected destination label in {instruction}");
                    }
                    break;
                default:
                    return null;
            }
            return result;
        }

        public string Dissemble(ushort pc, ushort instruction) {
            switch (instruction >> 12) {
                case 0b0000:
                    bool n = (instruction & 0x0800) > 0;
                    bool z = (instruction & 0x0400) > 0;
                    bool p = (instruction & 0x0200) > 0;
                    short offset9 = ((ushort)(instruction & 0b1_1111_1111)).ToSigned(9);
                    
                    return $"BR{(n ? "n" : "")}{(z ? "z" : "")}{(p ? "p" : "")} {(labelsReverse.TryGetValue((ushort)(pc + offset9), out string label) ? label : Convert.ToString(offset9, 10))}";
                default:
                    throw new NotImplementedException();
            }
        }
        public void PrintLabels() {
            labels.Keys.ToList().ForEach(label => Console.WriteLine($"{labels[label]} => {label}"));
        }
    }
    //When the MSB is zero, the other bits represent the absolute value
    //When the MSB is one, the other bits represent the value to subtract from 2^n
    //If the unsigned value is greater than the max signed value (the MSB must be one), that means we subtract the max unsigned value to get the signed value 
    public static class UShort {
        public static ushort Truncate(this ushort u, int remove) {
            return (ushort)((u << remove) >> remove);
        }
        public static short ToSigned(this ushort unsigned, int n = 16) {
            ushort maxUnsigned = (ushort) Math.Pow(2, n);
            ushort maxSigned = (ushort) Math.Pow(2, n - 1);
            Console.WriteLine("Max Unsigned: " + maxUnsigned);
            if(unsigned >= maxSigned) {
                return (short) (unsigned - maxUnsigned);
            } else {
                return (short) unsigned;
            }
        }
    }
	//LC3 simulator class
	public class LC3 {
        public Control control;
		public Memory memory;
		public ushort bus;
		public LC3() {
            control = new Control(this);
            memory = new Memory(this);
		}
        public void DebugPrint() {
            control.DebugPrint();
        }
        public void Fetch() {
            control.gatePC();
            memory.ldMAR();
            memory.memEnR();
            memory.gateMDR();
            control.ldIR();
            control.pcmux = Control.PCMUX.inc;
            control.ldPC();
        }
        public void Execute() => Execute(control.ir);
		public void Execute(ushort instruction) {
			//Get the opcode;
			switch(instruction >> 12) {
				case 0b0000:
                    //Fun fact: by default, all unused memory locations are 0x0000, which happens to represent instruction 
                    control.addr1mux = Control.ADDR1MUX.pc;
                    control.addr2mux = Control.ADDR2MUX.ir9;
                    control.pcmux = Control.PCMUX.addrAdd;
                    control.PrintMux();
                    if((control.N && (instruction & 0x0800) > 0) ||
                        (control.Z && (instruction & 0x0400) > 0) ||
                        (control.P && (instruction & 0x0200) > 0)) {
                        control.ldPC();
                    }
                    Console.WriteLine("Executed BR");
					break;
				case 0b0001:
					break;
			}
		}
	}
    public class Control {
        //Addr1Mux
        public enum ADDR1MUX {
            sr1out,
            pc
        }
        public ADDR1MUX addr1mux;
        private ushort addr1 => addr1mux == ADDR1MUX.sr1out ? sr1out : pc;
        //Addr2Mux
        public enum ADDR2MUX {
            ir11,
            ir9,
            ir6,
            b0
        }
        public ADDR2MUX addr2mux;
        private short addr2 =>
            (short) (
                addr2mux == ADDR2MUX.ir11 ? ((ushort) (ir & 0b111_1111_1111)).ToSigned(11) :
                addr2mux == ADDR2MUX.ir9 ? ((ushort)(ir & 0b1_1111_1111)).ToSigned(9) :
                addr2mux == ADDR2MUX.ir6 ? ((ushort)(ir & 0b11_1111)).ToSigned(6) :
                0);
        //AddrAdd
        public ushort addradd => (ushort) (addr1 + addr2);

        //PCMux
        public enum PCMUX {
            bus,
            addrAdd,
            inc,
        }
        public PCMUX pcmux;
        private ushort pcmuxout => (ushort) (
            pcmux == PCMUX.bus ? lc3.bus :
            pcmux == PCMUX.addrAdd ? addradd :
            pc + 1);
        //SR1
        public enum SR1MUX {
            ir8_6,
            ir11_9
        }
        public SR1MUX sr1mux;
        public ushort sr1out => registers[
            sr1mux == SR1MUX.ir8_6 ? (ir & 0b111000000) :
            (ir & 0b0000111000000000)
            ];
        private LC3 lc3;
        public ushort pc { get; private set; }
        public ushort ir { get; private set; }
        private ushort[] registers;
        public bool N { get; private set; }
        public bool Z { get; private set; }
        public bool P { get; private set; }
        public Control(LC3 lc3) {
            this.lc3 = lc3;
            pc = 0x3000;
            ir = 0;
            Z = true;
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

        public void PrintMux() {
            Console.WriteLine($"Addr1Mux: {addr1mux.ToString()}");
            Console.WriteLine($"Addr1: 0x{addr1.ToString("X")}");
            Console.WriteLine($"Addr2Mux: {addr2mux.ToString()}");
            Console.WriteLine($"Addr2: 0x{addr2.ToString("X")}");
            Console.WriteLine($"AddrAdd: 0x{addradd.ToString("X")}");
            Console.WriteLine($"PCMux: 0x{pcmuxout.ToString("X")}");
        }

        internal void DebugPrint() {
            Console.WriteLine($"PC: 0x{pc.ToString("X")}");
            Console.WriteLine($"IR: 0x{ir.ToString("X")}");
        }
    }
	public class Memory {
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
        public void WriteToMemory(ushort mar, ushort mdr) {
            mem[mar] = mdr;
        }
        public ushort Read(ushort mar) {
            return mem[mar];
        }
	}
    
}