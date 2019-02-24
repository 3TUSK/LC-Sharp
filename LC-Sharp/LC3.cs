﻿using LC_Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LC_Sharp {
    //LC3 simulator class
    public class LC3 {
        public Control control;
        public Processing processing;
		public Memory memory;
		public short bus;
		public enum Status {
			ACTIVE, TRAP, ERROR, HALT
		}
		public Status status;

		public bool Active => status == Status.ACTIVE || status == Status.TRAP;

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
		public void Execute(short instruction) {
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
        LC3 lc3;
		AssemblerContext context;
        public Assembler(LC3 lc3) {
            this.lc3 = lc3;
        }
        public void AssembleToPC(string instruction) {
            context.line = 0;
            if (Instruction(instruction, out short u)) {
                lc3.memory.Write(lc3.control.pc, u);
            } else {
                throw new Exception($"Invalid instruction {instruction}");
            }
        }
        public string DissembleToPC() {
            return Dissemble((short)lc3.control.pc, lc3.memory.Read(lc3.control.pc));
        }
        public string DissembleIR() {
            return Dissemble((short)(lc3.control.pc - 1), lc3.control.ir);
        }


        public void Line(string line) {
            bool labeled = false;
            Parse:
            if (Instruction(line, out short u)) {
                //If this names an instruction, we treat it like one
                lc3.memory.Write((short)(lc3.control.pc - 1), u);
            } else if (line.StartsWith(".")) {
                //Period marks a directive
                context.Directive(line);
            } else {
                if (labeled) {
                    //To do: what to do if we have two labels in a row?
                }
                //This line starts with a label, but we can have a directive or op right after
                var label = line.Split(new[] { ' ' }, 1)[0];
                context.Label(label);
                labeled = true;
                line = line.Substring(label.Length).TrimStart();
                goto Parse;
            }
        }
		public string Dissemble(short pc, short instruction) {
			context.pc = pc;
			//For TRAP Subroutines, we consider their entire code to be an opcode
			if((instruction & 0xF000) == 0xF000) {
				return ops.Code(instruction).Dissemble(context, instruction);
			} else {
				return ops.Code((short)(instruction & 0xF000)).Dissemble(context, instruction);
			}
		}

		public bool Instruction(string instruction, out short result) {
			result = 0;
			string opname = instruction.Split(new[] { ' ' }, 2)[0];
			context.Print($"Op: {opname}");
			switch (opname) {
				case var br when br.StartsWith("BR"):
					//BRnzp has complicated syntax so we evaluate it here
					bool n = false, z = false, p = false;
					foreach (char c in br.Substring(2)) {
						switch (c) {
							case 'n':
								if (n) {
									context.Error($"Repeated condition code 'n'");
								} else {
									n = true;
								}
								break;
							case 'z':
								if (z) {
									context.Error($"Repeated condition code 'z'");
								} else {
									z = true;
								}
								break;
							case 'p':
								if (p) {
									context.Error($"Repeated condition code 'p'");
								} else {
									p = true;
								}
								break;
						}
					}
					result = (short)((n ? 0x0800 : 0) | (z ? 0x0400 : 0) | (p ? 0x0200 : 0));
					result |= ops.Name(br).Assemble(context, instruction);
					break;
				default:
					Op op = ops.Name(opname);
					if (op != null) {
						result = op.Assemble(context, instruction);
					} else {
						context.Print($"Unknown instruction {opname}");
						return false;
					}
					break;
			}
			context.Print($"Assembled {opname}");
			return true;
		}
		OpLookup ops = new OpLookup(
			new Instruction("BR", 0, new[] { Operands.b000, Operands.LabelOffset9 }),
			new Instruction("ADD", 1, new[] { Operands.Reg, Operands.Reg, Operands.FlagRegImm5 }),
			new Instruction("LD", 2, new[] { Operands.Reg, Operands.LabelOffset9 }),
			new Instruction("ST", 3, new[] { Operands.Reg, Operands.LabelOffset9 }),
			new Instruction("JSR", 4, new[] { Operands.b1, Operands.LabelOffset11 }),
			new Instruction("JSRR", 4, new[] { Operands.b000, Operands.Reg }),
			new Instruction("AND", 5, new[] { Operands.Reg, Operands.Reg, Operands.FlagRegImm5 }),
			new Instruction("LDR", 6, new[] { Operands.Reg, Operands.Reg, Operands.LabelOffset6 }),
			new Instruction("STR", 7, new[] { Operands.Reg, Operands.Reg, Operands.LabelOffset6 }),
			new Instruction("RTI", 8),
			new Instruction("NOT", 9, new[] { Operands.Reg, Operands.Reg, Operands.b1, Operands.b1, Operands.b1, Operands.b1, Operands.b1, Operands.b1 }),
			new Instruction("LDI", 10, new[] { Operands.Reg, Operands.LabelOffset9 }),
			new Instruction("STI", 11, new[] { Operands.Reg, Operands.LabelOffset9 }),
			new Instruction("JMP", 12, new[] { Operands.b000, Operands.Reg }),
			new Instruction("RET", 12, new[] { Operands.b000, Operands.b1, Operands.b1, Operands.b1 }),
			new Instruction("RESERVED", 13),
			new Instruction("LEA", 14, new[] { Operands.Reg, Operands.LabelOffset9 }),
			new Instruction("TRAP", 15)
		);
	};
	class OpLookup {
		List<Op> ops;
		Dictionary<string, Op> byName;
		Dictionary<short, Op> byCode;
		public OpLookup(params Op[] opset) {
			ops = new List<Op>();
			byName = new Dictionary<string, Op>();
			byCode = new Dictionary<short, Op>();
			foreach(Op op in opset) {
				Add(op);
			}
		}
		public void Add(Op op) {
			ops.Add(op);
			byName[op.name] = op;
			byCode[op.code] = op;
		}
		public Op Name(string name) => byName[name];
		public Op Code(short code) => byCode[code];
	}
	public class AssemblerContext {
		public Dictionary<string, short> labels { get; private set; } //labels
		public Dictionary<short, string> labelsReverse { get; private set; } //reverse lookup labels
		public string[] source;
		public int line;
		public short pc;
		public void Print(string message) => Console.WriteLine(message);
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
		}
		//Calculates the PC-offset from the given label and verifies that it fits within a given size
		public short Offset(string label, int size) {
			short mask = (short)(0xFFFF >> (16 - size));    //All ones
			short min = (short)(1 << (size - 1));           //Highest negative number, same as the MSB
															  //short max = (short)(0xEFFF >> (16 - size));
			short max = (short)(mask ^ min);                //Highest positive number, All ones except MSB

			if (labels.TryGetValue(label, out short destination)) {
				short offset = (short)(destination - pc);
				//Size range check
				if (offset < min || offset > max) {
					Error($"Offset at '{label}' overflows signed {size}-bit integer in {source[line]}");
				} else {
					//Truncate the result to the given size
					return (short)(offset & mask);
				}
			} else {
				//Otherwise, we don't accept this label
				Error($"Unknown label '{label}' in {source[line]}");
			}
			return 0;
		}
		public bool Register(string code, out short result) {
			if (code.Length == 2 && code[0] == 'R' && code[1] >= '0' && code[1] <= '7') {
				result = (short)(code[1] - '0');
				return true;
			}
			result = 0;
			return false;
		}
		public void Error(string message) {
			throw new Exception($"Line {line}: {message} in {source[line]}");
		}
		public void PrintLabels() {
			labels.Keys.ToList().ForEach(label => Console.WriteLine($"{labels[label]} => {label}"));
		}
		//Create a label at the current line
		public void Label(string label) {
			short location = (short)line; //pc is 1 ahead of this location
			labels[label] = location;
			labelsReverse[location] = label;
		}
		public bool Immediate(string code, short size, out short result) {
			if (code.StartsWith("#")) {
				result = (short)short.Parse(code.Substring(1)).signExtend(size);
			} else if (code.StartsWith("x")) {
				result = (short)short.Parse(code, System.Globalization.NumberStyles.HexNumber).signExtend(size);
			} else {
				result = 0;
				return false;
			}
			int max = (1 << (size - 1)) - 1;
			int min = -(1 << (size - 1));
			if (result > max) {
				Error($"Immediate value underflows {size}-bit signed integer");
			} else if (result < max) {
				Error($"Immediate value overflows {size}-bit signed integer");
			}
			return true;
		}
		public bool Lookup(short address, out string label) {
			return labelsReverse.TryGetValue(address, out label);
		}
	}
	public enum Operands {
		Reg,                //Size 3, a register
		b1,                 //Size 1, a binary digit one
		b000,               //Size 3, three binary digits zero
		nzp,				//Size 3, three binary digits zero
		FlagRegImm5,        //Size 6, a register or an immediate value
		LabelOffset6,       //Size 6, an 6-bit offset from a pc to a label
		LabelOffset9,       //Size 9, an 9-bit offset from a pc to a label
		LabelOffset11,      //Size 11, an 11-bit offset from a pc to a label
	}
	public interface Op {
		string name { get; }
		short code { get; }
		short Assemble(AssemblerContext context, string instruction);
		string Dissemble(AssemblerContext context, short instruction);
	}
	public class Trap : Op {
		public string name { get; private set; }
		public short code => (short)(0b1111000000000000 | vector);
		public short vector;
		public short Assemble(AssemblerContext context, string instruction) {
			return code;
		}
		public string Dissemble(AssemblerContext context, short instruction) {
			return name;
		}
	}
	public class Instruction : Op {
		public string name { get; private set; }
		public short code { get; private set; }
		public Operands[] operands;
		public Instruction(string name, short code, params Operands[] operands) {
			this.name = name;
			this.code = code;
			this.operands = operands;
		}
		public short Assemble(AssemblerContext context, string instruction) {
			short bitIndex = 12;
			short result = 0;
			string[] args = instruction.Split();
			result |= (short)(code << bitIndex);
			args = string.Join("", args.Skip(1)).Split(',');
			int index = 0;
			foreach (Operands operand in operands) {
				switch (operand) {
					case Operands.b1:
						bitIndex--;
						result |= (short)(1 << bitIndex);
						break;
					case Operands.b000:
					case Operands.nzp:
						bitIndex -= 3;
						break;
					case Operands.Reg: {
							if (index >= args.Length) {
								context.Error($"Missing register in {instruction}");
							}

							if (!context.Register(args[index], out short reg)) {
								context.Error($"Register expected: '{args[index]}' in {instruction}");
							}
							bitIndex -= 3;
							result |= (short)(reg << bitIndex);
							index++;
							break;
						}
					case Operands.FlagRegImm5: {
							if (index >= args.Length) {
								context.Error($"Missing operand in {instruction}");
							}
							if (!context.Register(args[index], out short reg)) {
								if (!context.Immediate(args[index], 5, out short imm5)) {
									context.Error($"Imm5 value expected: {args[index]}");
								} else {
									bitIndex--;
									result |= (short)(1 << bitIndex);
									bitIndex -= 5;
									result |= (short)((imm5 & 0b111111) << bitIndex);
								}
							} else {
								bitIndex -= 3;
								result |= (short)(reg << bitIndex);
							}
							index++;
							break;
						}
					case Operands.LabelOffset9: {
							if (index >= args.Length) {
								context.Error($"Insufficient label in {instruction}");
							}
							bitIndex -= 9;
							result |= (short)(context.Offset(args[index], 9) << bitIndex);
							index++;
							break;
						}
					case Operands.LabelOffset6: {
							if (index >= args.Length) {
								context.Error($"Missing label in {instruction}");
							}
							bitIndex -= 6;
							result |= (short)(context.Offset(args[index], 6) << bitIndex);
							index++;
							break;
						}
					case Operands.LabelOffset11: {
							if (index >= args.Length) {
								context.Error($"Missing label in {instruction}");
							}
							bitIndex -= 11;
							result |= (short)(context.Offset(args[index], 11) << bitIndex);
							index++;
							break;
						}
				}
			}
			return result;
		}
		public string Dissemble(AssemblerContext context, short instruction) {
			short bitIndex = 12;
			List<string> args = new List<string> { name };
			foreach (Operands operand in operands) {
				switch (operand) {
					case Operands.b1:
						bitIndex--;
						break;
					case Operands.b000:
						bitIndex -= 3;
						break;
					case Operands.nzp:
						bitIndex--;
						if((instruction & (1 << bitIndex)) != 0) {
							args[0] += 'n';
						}
						bitIndex--;
						if ((instruction & (1 << bitIndex)) != 0) {
							args[0] += 'z';
						}
						bitIndex--;
						if ((instruction & (1 << bitIndex)) != 0) {
							args[0] += 'p';
						}
						break;
					case Operands.Reg: {
							bitIndex -= 3;
							args.Add($"R{(instruction >> bitIndex) & 0b111}");
							break;
						}
					case Operands.FlagRegImm5: {
							bitIndex--;
							if ((instruction & (1 << bitIndex)) != 0) {
								bitIndex -= 5;
								short imm5 = ((short) ((instruction >> bitIndex) & 0b11111)).signExtend(16);
								args.Add($"#imm5.ToString()");
							} else {
								bitIndex -= 2;
								args.Add($"R{(instruction >> bitIndex) & 0b111}");
							}
							break;
						}
					case Operands.LabelOffset9: {
							bitIndex -= 9;
							short offset9 = ((short)((instruction >> bitIndex) & 0b111111111)).signExtend(16);
							short dest = (short)(context.pc + offset9);
							if (context.Lookup(dest, out string label)) {
								args.Add(label);
							} else {
								args.Add(dest.ToString());
							}
							break;
						}
					case Operands.LabelOffset6: {
							bitIndex -= 6;
							short offset6 = ((short)((instruction >> bitIndex) & 0b111111)).signExtend(16);
							short dest = (short)(context.pc + offset6);
							if (context.Lookup(dest, out string label)) {
								args.Add(label);
							} else {
								args.Add(dest.ToString());
							}
							break;
						}
					case Operands.LabelOffset11: {
							bitIndex -= 11;
							short offset11 = ((short)((instruction >> bitIndex) & 0b11111111111)).signExtend(16);
							short dest = (short)(context.pc + offset11);
							if (context.Lookup(dest, out string label)) {
								args.Add(label);
							} else {
								args.Add(dest.ToString());
							}
							break;
						}
				}
			}
			return string.Join(" ", args);
	}
	}
}