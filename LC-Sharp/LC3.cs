using System;
using System.Collections.Generic;
using System.Linq;

namespace LC_Sharp {
    //LC3 simulator class
    public class LC3 {
        public Control control;
        public Processing processing;
		public Memory memory;
		public ushort bus;
		public enum Status {
			ACTIVE, TRAP, ERROR, HALT
		}
		public Status status;
		public LC3() {
            control = new Control(this);
            memory = new Memory(this);
            processing = new Processing(this);
			status = Status.ACTIVE;
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
                    Console.WriteLine("Executed LD");
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
                    Console.WriteLine("Executed ST");
                    break;
                case 0b0100:
                    if((instruction & 0x0800) != 0) {
                        //JSR
                        control.gatePC();
                        processing.drmux = Processing.DRMUX.b111;
                        processing.ldReg();
                        processing.addr1mux = Processing.ADDR1MUX.pc;
                        processing.addr2mux = Processing.ADDR2MUX.ir11;
                        processing.pcmux = Processing.PCMUX.addrAdd;
                        control.ldPC();
                        Console.WriteLine("Executed JSR");
                    } else {
                        //JSRR
                        control.gatePC();
                        processing.drmux = Processing.DRMUX.b111;
                        processing.ldReg();
                        processing.sr1mux = Processing.SR1MUX.ir8_6;
                        processing.addr1mux = Processing.ADDR1MUX.sr1out;
                        processing.addr2mux = Processing.ADDR2MUX.b0;
                        processing.pcmux = Processing.PCMUX.addrAdd;
                        control.ldPC();
                        Console.WriteLine("Executed JSRR");
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

					//We set trap to true and then repeatedly attempt execute this instruction until it is done. We do not fetch while trap is active. When we are done, we set trap to false.
                    break;
			}
		}
	}


    public class Assembler {
        public Dictionary<string, ushort> labels { get; private set; } //labels
        public Dictionary<ushort, string> labelsReverse { get; private set; } //reverse lookup labels
        LC3 lc3;
        ushort line;
        //ushort address;       //To do: stop using the attached lc3 pc and use our own pointer
        public Assembler(LC3 lc3) {
            this.lc3 = lc3;
            labels = new Dictionary<string, ushort>();
            labelsReverse = new Dictionary<ushort, string>();
        }
        private void Print(string message) => Console.WriteLine(message);
        public bool Imm5(string code, out ushort result) {
            if (code.StartsWith("#")) {
                result = (ushort)short.Parse(code.Substring(1)).signExtend(5);
                return true;
            } else if (code.StartsWith("x")) {
                result = (ushort)short.Parse(code, System.Globalization.NumberStyles.HexNumber).signExtend(5);
                return true;
            }
            result = 0;
            return false;
        }
        public bool Register(string code, out ushort result) {
            if (code.Length == 2 && code[0] == 'R' && code[1] >= '0' && code[1] <= '7') {
                result = (ushort)(code[1] - '0');
                return true;
            }
            result = 0;
            return false;
        }
        public void AssembleToPC(string instruction) {
            line = 1;
            if (Instruction(lc3.control.pc, instruction, out ushort u)) {
                lc3.memory.WriteToMemory(lc3.control.pc, u);
            } else {
                throw new Exception($"Invalid instruction {instruction}");
            }
        }
        public string DissembleToPC() {
            return Dissemble((ushort)lc3.control.pc, lc3.memory.Read(lc3.control.pc));
        }
        public string DissembleIR() {
            return Dissemble((ushort)(lc3.control.pc - 1), lc3.control.ir);
        }


        public void Line(string line) {
            bool labeled = false;
            Parse:
            if (Instruction(lc3.control.pc, line, out ushort u)) {
                //If this names an instruction, we treat it like one
                lc3.memory.WriteToMemory((ushort)(lc3.control.pc - 1), u);
            } else if (line.StartsWith(".")) {
                //Period marks a directive
                Directive(line);
            } else {
                if (labeled) {
                    //To do: what to do if we have two labels in a row?
                }
                //This line starts with a label, but we can have a directive or op right after
                var label = line.Split(new[] { ' ' }, 1)[0];
                Label(lc3.control.pc, label);
                labeled = true;
                line = line.Substring(label.Length).TrimStart();
                goto Parse;
            }
        }
        //Create a label at the current location
        public void Label(ushort pc, string label) {
            ushort location = (ushort)(pc - 1); //pc is 1 ahead of this location
            labels[label] = location;
            labelsReverse[location] = label;
        }
        public void Directive(string line) {
            switch (line.Split(new[] { ' ' }, 1)[0]) {
                case ".FILL":

                    break;
            }

            short Fill(string code) {
                if (code.StartsWith("b")) {
                    short result = 0;
                    foreach (char digit in code.Skip(1)) {
                        if (digit == '1') {
                            result++;
                        } else if (digit != '0') {
                            Error($"Invalid binary digit {code}");
                        }
                        result <<= 1;
                    }
                    return result;
                } else if (code.StartsWith("#")) {
                    return short.Parse(code.Substring(1));
                } else if (code.StartsWith("X")) {
                    return short.Parse(code, System.Globalization.NumberStyles.HexNumber);
                } else if (code.StartsWith("'")) {
                    if (code.Length == 3) {
                        return (short)code[1];
                    } else {
                        Error($"Invalid character literal {code}");
                    }
                }
                return 0;
            }
            void Error(string message) {
                throw new Exception($"Line {line}: {message} in {line}");
            }
        }

        //We use this to lookup predefined operands for a given operation
        private static readonly Dictionary<string, Operands[]> operandTable = new Dictionary<string, Operands[]> {
            {"BR", new [] { Operands.b000, Operands.LabelOffset9 } },
            {"ADD", new[] { Operands.Reg, Operands.Reg, Operands.FlagRegImm5 } },
            { "LD", new[] { Operands.Reg, Operands.LabelOffset9 } },
            { "ST", new[] { Operands.Reg, Operands.LabelOffset9 } },
            { "JSR", new[] { Operands.b1, Operands.LabelOffset11 } },
            { "JSRR", new[] { Operands.b000, Operands.Reg } },
            { "AND", new[] { Operands.Reg, Operands.Reg, Operands.FlagRegImm5 } },
            { "LDR", new[] { Operands.Reg, Operands.Reg, Operands.LabelOffset6 } },
            { "STR", new[] { Operands.Reg, Operands.Reg, Operands.LabelOffset6 } },
            { "NOT", new[] { Operands.Reg, Operands.Reg, Operands.b1, Operands.b1, Operands.b1, Operands.b1, Operands.b1, Operands.b1} },
            { "LDI", new[] { Operands.Reg, Operands.LabelOffset9 } },
            { "STI", new[] { Operands.Reg, Operands.LabelOffset9 } },
            { "JMP", new[] { Operands.b000, Operands.Reg } },
            { "RET", new Operands[0] },
            { "LEA", new[] { Operands.Reg, Operands.LabelOffset9 } },
        };
        /**
         * <summary>Attempts to assemble a single instruction string into ushort format.</summary>
         * <param name="pc">The expected value of the PC when this instruction should be executed</param>
         * <param name="instruction">The instruction string to assemble</param>
         * <returns>If <paramref name="instruction"/> names an instruction and is well-formed, then returns the assembled form. If <paramref name="instruction"/> does not name an instruction, returns false</returns>
         * <exception cref="Exception">Throws an exception if the string names an instruction but is not well-formed.</exception>
         * */
        public bool Instruction(ushort pc, string instruction, out ushort result) {
            result = 0;
            string op = instruction.Split(new[] { ' ' }, 2)[0];
            Print($"Op: {op}");
            switch (op) {
                case var br when br.StartsWith("BR"):
                    //BRnzp has complicated syntax so we evaluate it here
                    bool n = false, z = false, p = false;
                    foreach (char c in br.Substring(2)) {
                        switch (c) {
                            case 'n':
                                if (n) {
                                    Error($"Repeated condition code 'n'");
                                } else {
                                    n = true;
                                }
                                break;
                            case 'z':
                                if (z) {
                                    Error($"Repeated condition code 'z'");
                                } else {
                                    z = true;
                                }
                                break;
                            case 'p':
                                if (p) {
                                    Error($"Repeated condition code 'p'");
                                } else {
                                    p = true;
                                }
                                break;
                        }
                    }
                    result |= (ushort)(n ? 0x0800 : 0);
                    result |= (ushort)(z ? 0x0400 : 0);
                    result |= (ushort)(p ? 0x0200 : 0);
                    result |= Assemble(pc, "BR" + instruction.Substring(br.Length), operandTable["BR"]);
                    break;
                case var entry when operandTable.TryGetValue(entry, out Operands[] operands):
                    //Otherwise if we already predefined the operands for this operation
                    result = Assemble(pc, instruction, operands);
                    break;
                case "RTI":
                    Error("RTI not implemented");
                    break;
                case "TRAP":

                    break;
                default:
                    Print($"Unknown instruction {op}");
                    return false;
            }
            Print($"Assembled {op}");
            return true;
            void Error(string message) {
                throw new Exception($"Line {line}: {message} in {instruction}");
            }
        }
        private enum Operands {
            Reg,                //Size 3, a register
            b1,                 //Size 1, a binary digit one
            b000,               //Size 3, three binary digits zero
            FlagRegImm5,        //Size 6, a register or an immediate value
            LabelOffset6,       //Size 6, an 6-bit offset from a pc to a label
            LabelOffset9,       //Size 9, an 9-bit offset from a pc to a label
            LabelOffset11,      //Size 11, an 11-bit offset from a pc to a label
        }
        private ushort Assemble(ushort pc, string instruction, params Operands[] operands) {
            ushort bitIndex = 12;
            ushort result = 0;
            Dictionary<string, ushort> instructions = new Dictionary<string, ushort> {
                { "BR", 0 },
                { "ADD", 1 },
                { "LD", 2 },
                { "ST", 3 },
                { "JSR", 4 },
                { "JSRR", 4 },
                { "AND", 5 },
                { "LDR", 6 },
                { "STR", 7 },
                { "RTI", 8 },
                { "NOT", 9 },
                { "LDI", 10 },
                { "STI", 11 },
                { "JMP", 12 },
                { "RET", 12 },
                { "RESERVED", 13 },
                { "LEA", 14 },
                { "TRAP", 15 }
            };
            string[] args = instruction.Split();
            result |= (ushort)(instructions[args[0]] << bitIndex);
            args = string.Join("", args.Skip(1)).Split(',');
            int index = 0;
            foreach (Operands operand in operands) {
                switch (operand) {
                    case Operands.b1:
                        bitIndex--;
                        result &= (ushort)(1 << bitIndex);
                        break;
                    case Operands.b000:
                        bitIndex -= 3;
                        break;
                    case Operands.Reg: {
                            if (index >= args.Length) {
                                Error($"Missing register in {instruction}");
                            }

                            if (!Register(args[index], out ushort reg)) {
                                Error($"Register expected: '{args[index]}' in {instruction}");
                            }
                            bitIndex -= 3;
                            result |= (ushort)(reg << bitIndex);
                            index++;
                            break;
                        }
                    case Operands.FlagRegImm5: {
                            if (index >= args.Length) {
                                Error($"Missing operand in {instruction}");
                            }
                            if (!Register(args[index], out ushort reg)) {
                                if (!Imm5(args[index], out ushort imm5)) {
                                    Error($"Imm5 value expected: {args[index]}");
                                } else {
                                    bitIndex--;
                                    result |= (ushort)(1 << bitIndex);
                                    bitIndex -= 5;
                                    result |= (ushort)((imm5 & 0b111111) << bitIndex);
                                }
                            } else {
                                bitIndex -= 3;
                                result |= (ushort)(reg << bitIndex);
                            }
                            index++;
                            break;
                        }
                    case Operands.LabelOffset9: {
                            if (index >= args.Length) {
                                Error($"Insufficient label in {instruction}");
                            }
                            bitIndex -= 9;
                            result |= (ushort)(Offset(args[index], 9) << bitIndex);
                            index++;
                            break;
                        }
                    case Operands.LabelOffset6: {
                            if (index >= args.Length) {
                                Error($"Missing label in {instruction}");
                            }
                            bitIndex -= 6;
                            result |= (ushort)(Offset(args[index], 6) << bitIndex);
                            index++;
                            break;
                        }
                    case Operands.LabelOffset11: {
                            if (index >= args.Length) {
                                Error($"Missing label in {instruction}");
                            }
                            bitIndex -= 11;
                            result |= (ushort)(Offset(args[index], 11) << bitIndex);
                            index++;
                            break;
                        }
                }
            }
            return result;
            void Error(string s) {
                throw new Exception($"Line {line}: {s}");
            }
            //Calculates the PC-offset from the given label and verifies that it fits within a given size
            short Offset(string label, int size) {
                short mask = (short)(0xFFFF >> (16 - size));    //All ones
                short min = (short)(1 << (size - 1));           //Highest negative number, same as the MSB
                //short max = (short)(0xEFFF >> (16 - size));
                short max = (short)(mask ^ min);                //Highest positive number, All ones except MSB

                if (labels.TryGetValue(label, out ushort destination)) {
                    short offset = (short)(destination - pc);
                    //Size range check
                    if (offset < min || offset > max) {
                        Error($"Offset at '{label}' overflows signed {size}-bit integer in {instruction}");
                    } else {
                        //Truncate the result to the given size
                        return (short)(offset & mask);
                    }
                } else {
                    //Otherwise, we don't accept this label
                    Error($"Unknown label '{label}' in {instruction}");
                }
                return 0;
            }
        }
        public string Dissemble(ushort pc, ushort instruction) {
            switch (instruction >> 12) {
                case 0b0000:
                    bool n = (instruction & 0x0800) > 0;
                    bool z = (instruction & 0x0400) > 0;
                    bool p = (instruction & 0x0200) > 0;
                    short offset9 = ((short)(instruction & 0b1_1111_1111)).signExtend(9);

                    return $"BR{(n ? "n" : "")}{(z ? "z" : "")}{(p ? "p" : "")} {(labelsReverse.TryGetValue((ushort)(pc + offset9), out string label) ? label : Convert.ToString(offset9, 10))}";
                default:
                    throw new NotImplementedException();
            }
        }
        public void PrintLabels() {
            labels.Keys.ToList().ForEach(label => Console.WriteLine($"{labels[label]} => {label}"));
        }
    }

}