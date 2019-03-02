using LC_Sharp;
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
		private short trapReturn;

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
			switch (instruction >> 12) {
				case 0b0000:
					//BR
					processing.addr1mux = Processing.ADDR1MUX.pc;
					processing.addr2mux = Processing.ADDR2MUX.ir9;
					processing.pcmux = Processing.PCMUX.addrAdd;
					processing.PrintMux();
					if ((processing.N && (instruction & 0x0800) > 0) ||
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
					if ((instruction & 0x0800) != 0) {
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
					if ((instruction & 0x0020) != 0) {
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
					//If we returned to where we entered the TRAP subroutine from, then we set status back to normal
					if (control.pc == trapReturn) {
						status = Status.ACTIVE;
					}
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
					//We set status to TRAP until we return to this pc
					if(status != Status.TRAP) {
						//Nested calls don't affect the return address
						status = Status.TRAP;
						trapReturn = control.pc;
					}

					control.gatePC();
					processing.drmux = Processing.DRMUX.b111;
					processing.ldReg();
					processing.marmux = Processing.MARMUX.ir8;
					processing.gateMARMUX();
					memory.ldMAR();
					memory.memEnR();
					memory.gateMDR();
					processing.pcmux = Processing.PCMUX.bus;
					control.ldPC();
					break;
			}
		}
	}


	public class Assembler {
		LC3 lc3;
		AssemblerContext context;
		public List<InstructionPass> secondPass;
		OpLookup ops = new OpLookup(
			new Instruction("BR", 0, new[] { Operands.b000, Operands.LabelOffset9 }),
			new Instruction("ADD", 1, new[] { Operands.Reg, Operands.Reg, Operands.FlagRegImm5 }),
			new Instruction("LD", 2, new[] { Operands.Reg, Operands.LabelOffset9 }),
			new Instruction("ST", 3, new[] { Operands.Reg, Operands.LabelOffset9 }),
			new Instruction("JSR", 4, new[] { Operands.b1, Operands.LabelOffset11 }),
			new Instruction("JSRR", 4, new[] { Operands.b000, Operands.Reg }),
			new Instruction("AND", 5, new[] { Operands.Reg, Operands.Reg, Operands.FlagRegImm5 }),
			new Instruction("LDR", 6, new[] { Operands.Reg, Operands.Reg, Operands.Imm6 }),
			new Instruction("STR", 7, new[] { Operands.Reg, Operands.Reg, Operands.Imm6 }),
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
		short trapVectorIndex = 0x20;

		public Assembler(LC3 lc3) {
			this.lc3 = lc3;
			context = new AssemblerContext();
			secondPass = new List<InstructionPass>();
		}
		//Assemble the given lines as new source to an existing program
		public void AssembleToPC(params string[] lines) {
			context.pc = lc3.control.pc;
			context.pc++;
			AssembleLines(lines);
		}
		public string DissembleToPC() {
			return Dissemble((short)lc3.control.pc, lc3.memory.Read(lc3.control.pc));
		}
		public string DissembleIR() {
			return Dissemble((short)(lc3.control.pc - 1), lc3.control.ir);
		}

		public void AssembleLines(params string[] lines) {
			context.lineIndex = 0;
			context.source = lines;
			secondPass.Clear();
			//First Pass
			foreach(var line in lines) {
				FirstPass(line);
			}
			SecondPass();
		}
		public void FirstPass(string line) {
			line = line.Trim();

			bool apostrophe = false;
			bool quote = false;
			string code = new string(line.TakeWhile(c => {
				if (c == '"') {
					if(quote) {
						apostrophe = false;
						quote = false;
					} else {
						quote = true;
					}
				} else if(c == '\'') {
					if (apostrophe)
						apostrophe = false;
					else
						apostrophe = true;
				}
				return (c != ';' || apostrophe || quote);
			}).ToArray());
			string comment = line.Substring(code.Length);


			line = code;

			bool labeled = false;
			Parse:
			if (string.IsNullOrEmpty(line)) {
				context.lineIndex++;
				return;
			} else if (InstructionPass(line, out InstructionPass passed)) {
				//If this names an instruction, we treat it like one
				//Check if we have to do a second pass for this instruction. Otherwise, we assemble it now.
				if(passed.op.twoPass) {
					secondPass.Add(passed);
				} else {
					//We don't need a second pass so assemble it
					lc3.memory.Write((short) (context.pc - 1), passed.Assemble(context));
				}
				context.pc++;
				context.lineIndex++;
			} else if (line.StartsWith(".")) {
				//Period marks a directive
				Directive(line);
			} else {
				var a = context.labels.LastOrDefault();

				var parts = line.Split(new[] { ' ', '\r', '\n', '\t' }, 2);
				var label = parts[0];
				if (labeled) {
					//To do: what to do if we have two labels in a row?
					throw new Exception($"Unidentified token {label}");
				}
				//This line starts with a label, but we can have a directive or op right after
				context.Label(label);
				labeled = true;
				line = parts.Length == 1 ? "" : parts[1].Trim();
				goto Parse;
			}
		}
		public void SecondPass() {
			short pc = context.pc;
			int lineIndex = context.lineIndex;
			//Second Pass
			foreach (var pass in secondPass) {
				context.pc = pass.pc;
				context.lineIndex = pass.line;
				var i = pass.instruction;
				lc3.memory.Write((short)(context.pc - 1), pass.Assemble(context));
			}
			context.pc = pc;
			context.lineIndex = lineIndex;
			//Remember to clear secondPass.
			//There was a bug where second passes from future .ENDs would attempt to recompile old instruction passes
			secondPass.Clear();
		}
		public string Dissemble(short pc, short instruction) {
			context.pc = pc;
			//For TRAP Subroutines, we consider their entire code to be an opcode
			if ((instruction & 0xF000) == 0xF000) {
				return ops.Code(instruction).Dissemble(context, instruction);
			} else if ((instruction & 0xFE00) == 0) {
				return "NOP";
			} else {
				return ops.Code((short)((instruction & 0xF000) >> 12)).Dissemble(context, instruction);
			}
		}
		public void Directive(string line) {
			string[] parts = line.Split(new[] { ' ' }, 2);
			switch (parts[0]) {
				case ".BLKW":
					short length = short.Parse(parts[1]);
					for (int i = 0; i < length; i++) {
						context.pc++;
					}
					context.lineIndex++;
					break;
				case ".BREAK":

					context.lineIndex++;
					break;
				case ".FILL":
					lc3.memory.Write((short)(context.pc - 1), Fill(parts[1]));
					context.pc++;
					context.lineIndex++;
					break;
				case ".END":
					//End of file, indicates that we should second-pass and clear out labels now
					SecondPass();
					context.ClearLabels();
					context.lineIndex++;
					break;
				case ".ORIG":
					string orig = parts[1];
					if(!orig.StartsWith("X", true, null)) {
						context.Error($"Invalid .ORIG location {orig} must be a hexadecimal number");
					}
					orig = orig.Substring(1);
					context.pc = short.Parse(orig, System.Globalization.NumberStyles.HexNumber);
					context.lineIndex++;
					break;
				case ".STRINGZ":
					string s = parts[1];
					if (s.StartsWith("\"") && s.EndsWith("\"")) {
						for (int i = 1; i < s.Length - 1; i++) {
							lc3.memory.Write((short)(context.pc - 1), (short)s[i]);
							context.pc++;
						}
						lc3.memory.Write((short)(context.pc - 1), 0);
						context.pc++;
					}
					context.lineIndex++;
					break;
				case ".TRAP":
					//Write the current location to the TRAP vector table
					lc3.memory.Write(trapVectorIndex, (short) (context.pc - 1));

					//The operand is the name of the TRAP Subroutine
					string name = parts[1];
					ops.Add(new Trap(name, trapVectorIndex));
					
					//Increment the TRAP vector index
					trapVectorIndex++;
					context.lineIndex++;
					break;
			}
			short Fill(string code) {
				if (code.StartsWith("b")) {
					short result = 0;
					foreach (char digit in code.Skip(1)) {
						if (digit == '1') {
							result++;
						} else if (digit != '0') {
							context.Error($"Invalid binary digit {code}");
						}
						result <<= 1;
					}
					return result;
				} else if (code.StartsWith("#")) {
					return short.Parse(code.Substring(1));
				} else if (code.StartsWith("X")) {
					return short.Parse(code.Substring(1), System.Globalization.NumberStyles.HexNumber);
				} else if (code.StartsWith("'")) {
					if (code.Length == 3) {
						return (short)code[1];
					} else {
						context.Error($"Invalid character literal {code}");
					}
				}
				return 0;
			}
		}
		/*
		public bool Instruction(string instruction, out short result) {
			result = 0;
			string opname = instruction.Split(new[] { ' ' }, 2)[0];
			context.Print($"Op: {opname}");
			switch (opname) {
				case var br when br.StartsWith("BR"): {
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
					}
				default: {
						if (ops.TryName(opname, out Op op)) {
							result = op.Assemble(context, instruction);
						} else {
							context.Print($"Unknown instruction {opname}");
							return false;
						}
						break;
					}
			}
			context.Print($"Assembled {opname}");
			return true;
		}
		*/
		public bool InstructionPass(string instruction, out InstructionPass passed) {
			passed = null;
			string opname = instruction.Split(new[] { ' ', '\r', '\n', '\t' }, 2)[0];
			context.Print($"Op: {opname}");
			switch (opname) {
				case var br when br.StartsWith("BR"): {
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
						short precode = (short)((n ? 0x0800 : 0) | (z ? 0x0400 : 0) | (p ? 0x0200 : 0));
						passed = new InstructionPass(context, ops.Name("BR"), instruction, precode);
						break;
					}
				default: {
						if (ops.TryName(opname, out Op op)) {

							//To do: Check the number of operands
							IEnumerable<string> args = instruction.Split(' ', '\r', '\n', '\t');
							args = string.Join("", args.Skip(1)).Split(',').Where(s => !string.IsNullOrEmpty(s));
							if(op.operandCount != args.Count()) {
								context.Error($"Op {opname} requires exactly {op.operandCount} operands");
							}


							passed = new InstructionPass(context, op, instruction);
						} else {
							context.Print($"Unknown instruction: {opname}");
							return false;
						}
						break;
					}
			}
			context.Print($"Pre-Assembled {opname}");
			return true;
		}
	};
	class OpLookup {
		List<Op> ops;
		Dictionary<string, Op> byName;
		Dictionary<short, Op> byCode;
		public OpLookup(params Op[] opset) {
			ops = new List<Op>();
			byName = new Dictionary<string, Op>();
			byCode = new Dictionary<short, Op>();
			foreach (Op op in opset) {
				Add(op);
			}
		}
		public void Add(Op op) {
			ops.Add(op);
			byName[op.name] = op;
			byCode[op.code] = op;
		}
		public bool TryName(string name, out Op result) => byName.TryGetValue(name, out result);
		public bool TryCode(short code, out Op result) => byCode.TryGetValue(code, out result);
		public Op Name(string name) => byName[name];
		public Op Code(short code) => byCode[code];
	}
	public class AssemblerContext {
		public Dictionary<string, short> labels { get; private set; } //labels
		public Dictionary<short, string> labelsReverse { get; private set; } //reverse lookup labels
		public string[] source;
		public int lineIndex;
		public short pc;
		public AssemblerContext() {
			labels = new Dictionary<string, short>();
			labelsReverse = new Dictionary<short, string>();
			source = new string[0];
			lineIndex = 0;
			pc = 0x3000;
		}
		public AssemblerContext localContext() => new AssemblerContext() {
			labels = labels,                //Pass by reference, so it updates with our map
			labelsReverse = labelsReverse,  //Pass by reference, so it updates with our map
			source = source,                //Pass by reference, but we don't set any values by index
			lineIndex = lineIndex,                    //Pass by value, so it doesn't change
			pc = pc                         //Pass by value, so it doesn't change
		};
		public void ClearLabels() {
			labels.Clear();
			labelsReverse.Clear();
		}
		public void Print(string message) => Console.WriteLine(message);
		//Calculates the PC-offset from the given label and verifies that it fits within a given size
		public short Offset(string label, int size) {
			ushort mask = (ushort)(0xFFFF >> (16 - size));    //All ones
			short min = (short)-(1 << (size - 1));           //Highest negative number, same as the MSB
															//short max = (short)(0xEFFF >> (16 - size));
			short max = (short)(-min - 1);                //Highest positive number, All ones except MSB

			if (labels.TryGetValue(label, out short destination)) {
				short offset = (short)(destination - pc);
				//Size range check
				if (offset < min || offset > max) {
					Error($"Offset at '{label}' overflows signed {size}-bit integer in {source[lineIndex]}");
				} else {
					//Truncate the result to the given size
					return (short)(offset & mask);
				}
			} else {
				foreach (var l in labels.Keys)
					Console.WriteLine(l);
				//Otherwise, we don't accept this label
				Error($"Unknown label '{label}'");
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
			throw new Exception($"Line {lineIndex}: {message} in {source?[lineIndex] ?? "code"}");
		}
		public void PrintLabels() {
			labels.Keys.ToList().ForEach(label => Console.WriteLine($"{labels[label]} => {label}"));
		}
		//Create a label at the current line
		public void Label(string label) {
			short location = (short) (pc - 1);
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
			} else if (result < min) {
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
		nzp,                //Size 3, three binary digits zero
		FlagRegImm5,        //Size 6, a register or an immediate value
		Imm6,				//Size 6, an immediate value
		LabelOffset6,       //Size 6, an 6-bit offset from a pc to a label
		LabelOffset9,       //Size 9, an 9-bit offset from a pc to a label
		LabelOffset11,      //Size 11, an 11-bit offset from a pc to a label
	}
	public class InstructionPass {
		//Stores info for assembling an instruction in the second pass
		public short pc { get; private set; }
		public int line { get; private set; }
		public short precode { get; private set; }
		public Op op { get; private set; }
		public string instruction { get; private set; }
		public InstructionPass(AssemblerContext context, Op op, string instruction, short precode = 0) {
			pc = context.pc;
			line = context.lineIndex;
			this.precode = precode;
			this.op = op;
			this.instruction = instruction;
		}
		public short Assemble(AssemblerContext context) {
			return (short) (precode | op.Assemble(context, instruction));
		}
	}
	public interface Op {
		string name { get; }
		short code { get; }
		bool twoPass { get; }
		int operandCount { get; }
		short Assemble(AssemblerContext context, string instruction);
		string Dissemble(AssemblerContext context, short instruction);
	}
	public class Trap : Op {
		public string name { get; private set; }
		public short code => (short)(0b1111000000000000 | vector);
		public bool twoPass => false;
		public short vector;
		public int operandCount => 0;
		public Trap(string name, short vector) {
			this.name = name;
			this.vector = vector;
		}
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
		public bool twoPass => operands.Any(o => o == Operands.LabelOffset6 || o == Operands.LabelOffset9 || o == Operands.LabelOffset11);
		public int operandCount => operands.Count(o => o != Operands.b000 && o != Operands.b1 && o != Operands.nzp);
		public Operands[] operands { get; private set; }
		public Instruction(string name, short code, params Operands[] operands) {
			this.name = name;
			this.code = code;
			this.operands = operands;
		}
		public short Assemble(AssemblerContext context, string instruction) {
			short bitIndex = 12;
			short result = 0;
			string[] args = instruction.Split(' ', '\r', '\n', '\t');
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
					case Operands.Imm6: {
							if (index >= args.Length) {
								context.Error($"Missing operand in {instruction}");
							}
							if (!context.Immediate(args[index], 5, out short imm5)) {
								context.Error($"Imm5 value expected: {args[index]}");
							} else {
								bitIndex--;
								result |= (short)(1 << bitIndex);
								bitIndex -= 5;
								result |= (short)((imm5 & 0b111111) << bitIndex);
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
								args.Add($"#{imm5.ToString()}");
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
								args.Add(offset9.ToString());
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
								args.Add(offset6.ToString());
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
								args.Add(offset11.ToString());
							}
							break;
						}
				}
			}
			return string.Join(" ", args);
	}
	}
}