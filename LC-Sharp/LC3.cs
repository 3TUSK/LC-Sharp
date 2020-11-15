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
		private bool halted => (((memory.Read(unchecked ((short)0xFFFE))) & 0x8000) == 0);
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

			//Init the MCR
			memory.Write(-2, -1);
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
			short opcode = (short) ((instruction >> 12) & 0xF);	//Make sure it's only the 4 LSBs in case we have a negative number (which is how I broke RET when moving everything from ushort to short)
			switch (opcode) {
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
					//Console.WriteLine("Executed BR");
					break;
				case 0b0001:
					//ADD
					//Register mode
					if ((instruction & 0b100000) == 0) {
						processing.sr1mux = Processing.SR1MUX.ir8_6;
						processing.sr2mux = Processing.SR2MUX.sr2out;
						processing.aluk = Processing.ALUK.add;
						//Console.WriteLine($"ALUA: {processing.aluA}");
						//Console.WriteLine($"ALUB: {processing.aluB}");
						//Console.WriteLine($"ALU: {processing.alu}");
						processing.gateALU();
						processing.drmux = Processing.DRMUX.ir11_9;
						processing.ldReg();
						//Console.WriteLine("Executed ADD Register");
					} else {
						//Immediate mode
						processing.sr1mux = Processing.SR1MUX.ir8_6;
						processing.sr2mux = Processing.SR2MUX.ir5;
						processing.aluk = Processing.ALUK.add;
						//Console.WriteLine($"ALUA: {processing.aluA}");
						//Console.WriteLine($"ALUB: {processing.aluB}");
						//Console.WriteLine($"ALU: {processing.alu}");
						processing.gateALU();
						processing.drmux = Processing.DRMUX.ir11_9;
						processing.ldReg();
						//Console.WriteLine("Executed ADD Immediate");
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
					//Console.WriteLine("Executed LD");
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
					//Console.WriteLine("Executed ST");
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
						//Console.WriteLine("Executed JSR");
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
						//Console.WriteLine("Executed JSRR");
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
					processing.drmux = Processing.DRMUX.ir11_9;
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
			//If the Machine Control Register has been cleared after this instruction, we halt
			if (halted && status != Status.TRAP) {
				status = Status.HALT;
			}
		}
	}


	public class Assembler {
		LC3 lc3;
		Reader reader;
		public short pc { get; private set; }
		public int index => reader.index;

		private string scopeName;
		private bool passing;
		public List<InstructionPass> secondPass;
		public Dictionary<string, short> labels { get; private set; } //labels
		public Dictionary<short, string> labelsReverse { get; private set; } //reverse lookup labels
		public Dictionary<string, short> trapLookup { get; private set; }
		public Dictionary<short, string> comments { get; private set; }
		public HashSet<short> breakpoints;
		public HashSet<short> nonInstruction { get; private set; }
		/*
		public static Instruction Br(string name, short code) => new Instruction(name, (short) (0 | code), new[] { Operands.nzp, Operands.LabelOffset9 });
		*/
		OpLookup ops = new OpLookup(
			new BR("BRn", 0x0800),
			new BR("BRz", 0x0400),
			new BR("BRp", 0x0200),
			new BR("BRnz", 0x0C00),
			new BR("BRzp", 0x0600),
			new BR("BRnp", 0x0A00),
			new BR("BRnzp", 0x0E00),
			new Instruction("BR", 0, new[] { Operands.nzp, Operands.LabelOffset9 }),
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
			new Instruction("RET", 12, new[] { Operands.b000, Operands.b1, Operands.b1, Operands.b1 }),
			new Instruction("JMP", 12, new[] { Operands.b000, Operands.Reg }),
			new Instruction("RESERVED", 13),
			new Instruction("LEA", 14, new[] { Operands.Reg, Operands.LabelOffset9 }),
			new Instruction("TRAP", 15)
		);
		short trapVectorIndex = 0x0020;

		public Assembler(LC3 lc3) {
			this.lc3 = lc3;
			pc = lc3.control.pc;
			pc++;
			reader = new Reader("");
			labels = new Dictionary<string, short>();
			labelsReverse = new Dictionary<short, string>();
			trapLookup = new Dictionary<string, short>();
			comments = new Dictionary<short, string>();
			secondPass = new List<InstructionPass>();
			breakpoints = new HashSet<short>();
			nonInstruction = new HashSet<short>();
			InitDirectives();
		}
		//Assemble the given lines as new source to an existing program
		public void AssembleToPC(params string[] lines) {
			pc = lc3.control.pc;
			pc++;
			AssembleLines(lines);
		}
		public string DissembleToPC() {
			return Dissemble((short)lc3.control.pc, lc3.memory.Read(lc3.control.pc));
		}
		public string DissembleIR() {
			return Dissemble((short)(lc3.control.pc - 1), lc3.control.ir);
		}

		public void AssembleLines(params string[] lines) {
			reader = new Reader(string.Join("\n", lines));
			InitPass();
			FirstPass();
			SecondPass();
		}
		public void InitPass() {
			passing = true;
			secondPass.Clear();
			scopeName = null;
		}
		public void FirstPass() {
			Read:
			if (!passing)
				return;
			switch (reader.Read(out string token)) {
				case TokenType.Symbol:
					HandleSymbol(token);
					//Print($"Line {reader.line}: Token {token}");
					goto Read;
				case TokenType.Comma:
					Error("Unexpected Comma");
					goto Read;
				case TokenType.Directive:
					//Print($"Line {reader.line}: Token {token}");
					Directive(token);
					goto Read;
				case TokenType.String:
					Error("Unexpected String");
					goto Read;
				case TokenType.Comment:

					goto Read;
				case TokenType.End:
					break;
			}
			Print("First Passed");
		}
		void HandleSymbol(string symbol) {
			if (ops.TryName(symbol, out Op op)) {
				HandleOp(op);
			} else {
				Label(symbol);
			}
		}
		public void HandleOp(Op op) {
			//If this op has zero operands, we specifically stop parsing it here because we could incorrectly interpret a subsequent label as an argument
			List<string> args = new List<string>();
			if (op.operandCount == 0) {
				HandleInstruction(op, args);
				//Return so that we don't parse arguments anyway
				return;
			}

			bool expectComma = false;
			int argCount = 0;

			ReadArg:
			switch (reader.Read(out string arg)) {
				case TokenType.Symbol: {
						if (expectComma) {

							//We expected a comma here. There's no comma, so this is either a new instruction or new label

							//Stop parsing this instruction
							HandleInstruction(op, args);
							//Start handling the next symbol
							HandleSymbol(arg);

						} else {
							//We expected a regular argument here

							//If we get another op
							if (ops.TryName(arg, out Op next)) {
								//And we just passed a comma
								if (argCount > 0) {
									Error("Argument expected after comma");
								} else {
									HandleInstruction(op, args);
									HandleOp(next);
								}
							} else {
								//Otherwise, regular argument
								argCount++;
								args.Add(arg);
								expectComma = true;
								goto ReadArg;
							}
						}

						break;
					}
				case TokenType.Comma:
					if (expectComma) {
						expectComma = false;
						goto ReadArg;
					} else {
						Error("Unexpected comma");
					}

					break;
				case TokenType.Comment:
					goto ReadArg;
				case TokenType.Directive:
					HandleInstruction(op, args);
					Directive(arg);
					break;
				case TokenType.End:
					HandleInstruction(op, args);
					break;
				case TokenType.String:
					Error("Unexpected String");
					break;
			}
		}
		public void HandleInstruction(Op op, List<string> args) {
			if(op.twoPass) {
				Print($"Line {reader.line}: Two Pass Instruction {op.name} {string.Join(", ", args)} at {((short)(pc-1)).ToHexString()}");
				secondPass.Add(new InstructionPass(this, op, args.ToArray()));
			} else {
				Print($"Line {reader.line}: One Pass Instruction {op.name} {string.Join(", ", args)} at {((short)(pc - 1)).ToHexString()}");
				lc3.memory.Write((short)(pc - 1), op.Assemble(this, args.ToArray()));
			}
			//Don't forget to increment the PC because we just wrote an instruction
			pc++;
		}
		public void HandleComment(string comment) {
			if(comments.ContainsKey((short) (pc - 1))) {
				comments[(short)(pc - 1)] += comment;
			} else {
				comments[(short)(pc - 1)] = comment;
			}
		}
		public void SecondPass() {
			short pcSave = this.pc;
			int indexSave = reader.index;
			//Second Pass
			foreach (var pass in secondPass) {
				pc = pass.pc;
				reader.index = pass.index;
				var i = pass.args;

				Print($"Line {reader.line}: Two Pass Instruction {pass.op.name} {string.Join(", ", pass.args)} at {((short)(pc - 1)).ToHexString()}");

				lc3.memory.Write((short)(pc - 1), pass.Assemble(this));
			}
			this.pc = pcSave;
			reader.index = indexSave;
			//Remember to clear secondPass.
			//There was a bug where second passes from future .ENDs would attempt to recompile old instruction passes
			secondPass.Clear();
		}
		public string Dissemble(short pc, short instruction) {
			this.pc = pc;

			//This might be a FILLED value, but we can't tell
			if(nonInstruction.Contains((short) (pc))) {
				if (Lookup((short)(pc), out string label))
					return $"{label.PadRight(30)} {lc3.memory.Read((short) (pc)).ToHexString()}";
				else
					return $"[DATA] {lc3.memory.Read((short)(pc)).ToHexString()}";
			}

			//For TRAP Subroutines, we consider their entire code to be an opcode
			if ((instruction & 0xF000) == 0xF000) {
				//var s = Convert.ToString(instruction, 2);
				if (ops.TryCode(instruction, out Op result))
					return result.Dissemble(this, instruction);
				return instruction.ToRegisterString();
			} else if((instruction & 0xF000) == 0x4000) {
				//Special case: check JSR/JSRR flag since they have the same opcode
				if((instruction & 0x0800) != 0) {
					return ops.Name("JSR").Dissemble(this, instruction);
				} else {
					return ops.Name("JSRR").Dissemble(this, instruction);
				}
			} else if ((instruction & 0xFE00) == 0) {
				return "NOP";
			} else {
				return ops.Code((short)((instruction & 0xF000) >> 12)).Dissemble(this, instruction);
			}
		}

		public delegate void DirectiveProcessor(Assembler context, string directive);

		private Dictionary<string, DirectiveProcessor> directives = new Dictionary<string, DirectiveProcessor>();

		void InitDirectives()
		{
			directives[".BLKW"] = (context, directive) =>
			{
				short length = 0;
				Read:
				switch(reader.Read(out string token)) {
					case TokenType.Comment:
						goto Read;
					case TokenType.Symbol:
						length = Fill(token);
						break;
					default:
						Error("Expected short value");
						return;
				}
				for (int i = 0; i < length; i++) {
					nonInstruction.Add((short) (pc - 1));
					pc++;
				}
			};
			directives[".BREAK"] = (context, directive) => {
				breakpoints.Add(context.pc);
			};
			
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
				} else if (code.ToLower().StartsWith("x")) {
					return short.Parse(code.Substring(1), System.Globalization.NumberStyles.HexNumber);
				} else if (code.StartsWith("'")) {
					//Handle escape chars
					code = code.Replace("\\n", "\n");
					if (code.Length == 3) {
						return (short)code[1];
					} else {
						Error($"Invalid character literal {code}");
					}
				} else {
					Error($"Invalid short value {code}");
				}
				return 0;
			}
			
			directives[".FILL"] = (context, directive) =>
			{
				short value = 0;
				Read:
				switch (reader.Read(out string token)) {
					case TokenType.Comment:
						goto Read;
					case TokenType.Symbol:
						value = Fill(token);
						break;
					default:
						Error("Expected short value");
						return;
				}
				short location = (short)(pc - 1);
				Print($"Line {reader.line}: Passed Directive {directive} \"{token}\" / {value} with {(labelsReverse.TryGetValue(location, out string l) ? $"Label {l}" : "")} at {location.ToHexString()}");
				nonInstruction.Add(location);
				lc3.memory.Write(location, value);
				pc++;
			};
			directives[".END"] = (context, directive) =>
			{
				passing = false;
				SecondPass();
				ClearLabels();
				Print($"Line {reader.line}: End of file");
			};
			directives[".ORIG"] = (context, directive) =>
			{
				Read:
				switch (reader.Read(out string orig))
				{
					case TokenType.Comment:
						goto Read;
					case TokenType.Symbol:
						break;
					default:
						Error("Expected short value");
						return;
				}

				if (!orig.StartsWith("X", true, null))
				{
					Error($"Invalid .ORIG location {orig} must be a hexadecimal number");
				}

				Print($"Line {reader.line}: ORIG {orig}");

				orig = orig.Substring(1);
				pc = short.Parse(orig, System.Globalization.NumberStyles.HexNumber);
				pc++;
			};
			directives[".SCOPE"] = (context, directive) =>
			{
				//Declares a new scope and indicates that we should second-pass and clear out labels now
				SecondPass();
				ClearLabels();
				Print($"Line {reader.line}: Clear current scope");

				Read:
				switch (reader.Read(out scopeName)) {
					case TokenType.Comment:
						goto Read;
					case TokenType.Symbol:
						break;
					default:
						Error("Expected scope name");
						return;
				}
			};
			directives[".STRINGZ"] = (context, directive) =>
			{
				Read:
				switch (reader.Read(out string s))
				{
					case TokenType.Comment:
						goto Read;
					case TokenType.String:
						break;
					default:
						Error("Expected string value");
						return;
				}

				//Handle escape chars
				s = s.Replace("\\n", "\n");
				for (int i = 0; i < s.Length; i++)
				{
					nonInstruction.Add((short) (pc - 1));
					lc3.memory.Write((short) (pc - 1), (short) s[i]);
					pc++;
				}

				nonInstruction.Add((short) (pc - 1));
				lc3.memory.Write((short) (pc - 1), 0);
				pc++;
			};
			directives[".TRAP"] = (context, directive) =>
			{
				Read:
				switch (reader.Read(out string name))
				{
					case TokenType.Comment:
						goto Read;
					case TokenType.Symbol:
						break;
					default:
						Error("Expected symbol value");
						return;
				}

				short start = (short) (pc - 1);
				trapLookup[name] = start;
				//Write the current location to the TRAP vector table
				lc3.memory.Write(trapVectorIndex, start);

				//The operand is the name of the TRAP Subroutine
				Print($"Line {reader.line}: TRAP {name}");
				ops.Add(new Trap(name, trapVectorIndex));

				//Increment the TRAP vector index
				trapVectorIndex++;

				//We also declare a new scope with this name
				scopeName = name;
				SecondPass();
				ClearLabels();
			};
		}
		
		public void Directive(string directive) 
		{
			if (directives.ContainsKey(directive))
			{
				directives[directive](this, directive);
			}
			else
			{
				// TODO (3TUSK): Log error or warn
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

		public void ClearLabels() {
			if (scopeName != null) {
				foreach(var label in new List<string>(labels.Keys.AsEnumerable())) {
					//Do not rename already-scoped labels
					if (label.Contains(" "))
						continue;

					string renamed = $"{label} ({scopeName})";
					short location = labels[label];
					labels[renamed] = location;
					labels.Remove(label);

					labelsReverse[location] = renamed;
				}
			}
			//labels.Clear();
			//We allow limited label reuse and identification by preserving the location-to-label map
			//labelsReverse.Clear();
		}
		public static bool print = false;
		public void Print(string message) { if(print) Console.WriteLine(message); }
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
					Error($"Offset at '{label}' overflows signed {size}-bit integer");
				} else {
					//Truncate the result to the given size
					return (short)(offset & mask);
				}
			} else {
				//foreach (var l in labels.Keys)
					//Console.WriteLine(l);
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
			throw new Exception(reader.GetContextString(message));
		}
		public void PrintLabels() {
			//labels.Keys.ToList().ForEach(label => Console.WriteLine($"{labels[label]} => {label}"));
		}
		//Create a label at the current line
		public void Label(string label) {
			if(labels.ContainsKey(label)) {
				Error($"Duplicate Label {label}");
			}
			short location = (short)(pc - 1);
			Print($"Line {reader.line}: Passed Label {label} at {location.ToHexString()}");
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
			byName[op.name.ToUpper()] = op;
			byCode[op.code] = op;
		}
		public bool TryName(string name, out Op result) => byName.TryGetValue(name.ToUpper(), out result);
		public bool TryCode(short code, out Op result) => byCode.TryGetValue(code, out result);
		public Op Name(string name) => byName[name];
		public Op Code(short code) => byCode[code];
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
		public int index { get; private set; }
		public short precode { get; private set; }
		public Op op { get; private set; }
		public string[] args { get; private set; }
		public InstructionPass(Assembler context, Op op, string[] args, short precode = 0) {
			pc = context.pc;
			index = context.index;
			this.precode = precode;
			this.op = op;
			this.args = args;
		}
		public short Assemble(Assembler context) {
			return (short) (precode | op.Assemble(context, args));
		}
	}
	public interface Op {
		string name { get; }
		short code { get; }
		bool twoPass { get; }
		int operandCount { get; }
		short Assemble(Assembler context, string[] args);
		string Dissemble(Assembler context, short instruction);
	}
	public class Trap : Op {
		public string name { get; private set; }
		public short code { get; private set; }
		public bool twoPass => false;
		public int operandCount => 0;
		public Trap(string name, short vector) {
			this.name = name;
			code = (short)(0xF000 | vector);
		}
		public short Assemble(Assembler context, string[] args) {
			return code;
		}
		public string Dissemble(Assembler context, short instruction) {
			return name;
		}
	}
	public class BR : Instruction {
		private short flags;
		public BR(string name, short flags) : base(name, 0, new[] { Operands.b000, Operands.LabelOffset9 }) {
			this.flags = flags;
		}
		public override short Assemble(Assembler context, string[] args) => (short) (base.Assemble(context, args) | flags);
		public override string Dissemble(Assembler context, short instruction) => base.Dissemble(context, instruction);
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
		public virtual short Assemble(Assembler context, string[] args) {
			short bitIndex = 12;
			short result = 0;
			result |= (short)(code << bitIndex);
			int index = 0;
			foreach (Operands operand in operands) {
				string arg = index >= args.Length ? null : args[index];
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
							if (arg == null) {
								context.Error($"Missing register in {args}");
							}
							if (!context.Register(arg, out short reg)) {
								context.Error($"Register expected: [{arg}] in {args}");
							}
							bitIndex -= 3;
							result |= (short)(reg << bitIndex);
							index++;
							break;
						}
					case Operands.FlagRegImm5: {
							if (arg == null) {
								context.Error($"Missing operand in {args}");
							}
							if (!context.Register(arg, out short reg)) {
								if (!context.Immediate(arg, 5, out short imm5)) {
									context.Error($"Imm5 value expected: {arg}");
								} else {
									bitIndex--;
									result |= (short)(1 << bitIndex);
									bitIndex -= 5;
									result |= (short)((imm5 & 0b111111) << bitIndex);
								}
							} else {
								//Shift 3 more since it was being stored in the flag's place
								bitIndex -= 6;
								result |= (short)(reg << bitIndex);
							}
							index++;
							break;
						}
					case Operands.Imm6: {
							if (arg == null) {
								context.Error($"Missing operand in {args}");
							}
							if (!context.Immediate(arg, 6, out short imm6)) {
								context.Error($"Imm6 value expected: {arg}");
							} else {
								bitIndex -= 6;
								result |= (short)((imm6 & 0b111111) << bitIndex);
							}
							index++;
							break;
						}
					case Operands.LabelOffset9: {
							if (arg == null) {
								context.Error($"Insufficient label in {args}");
							}
							bitIndex -= 9;
							result |= (short)(context.Offset(arg, 9) << bitIndex);
							index++;
							break;
						}
					case Operands.LabelOffset6: {
							if (arg == null) {
								context.Error($"Missing label in {args}");
							}
							bitIndex -= 6;
							result |= (short)(context.Offset(arg, 6) << bitIndex);
							index++;
							break;
						}
					case Operands.LabelOffset11: {
							if (arg == null) {
								context.Error($"Missing label in {args}");
							}
							bitIndex -= 11;
							result |= (short)(context.Offset(arg, 11) << bitIndex);
							index++;
							break;
						}
				}
			}
			return result;
		}
		public virtual string Dissemble(Assembler context, short instruction) {
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
						if ((instruction & (1 << bitIndex)) == (1 << bitIndex)) {
							args[0] += 'n';
						}
						bitIndex--;
						if ((instruction & (1 << bitIndex)) == (1 << bitIndex)) {
							args[0] += 'z';
						}
						bitIndex--;
						if ((instruction & (1 << bitIndex)) == (1 << bitIndex)) {
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
								short imm5 = ((short) ((instruction >> bitIndex) & 0b11111)).signExtend(5);
								args.Add($"#{imm5.ToString()}");
							} else {
								//Shift 3 more since we were reading the flag's place
								bitIndex -= 5;
								args.Add($"R{(instruction >> bitIndex) & 0b111}");
							}
							break;
						}
					case Operands.Imm6: {
							bitIndex -= 6;
							args.Add($"#{((short) ((instruction >> bitIndex) & 0b111111)).signExtend(6).ToString()}");
							break;
						}
					case Operands.LabelOffset9: {
							bitIndex -= 9;
							short offset9 = ((short)((instruction >> bitIndex) & 0b111111111)).signExtend(9);
							short dest = (short)(context.pc + offset9);
							dest++;
							if (context.Lookup(dest, out string label)) {
								args.Add(label);
							} else {
								args.Add(offset9.ToString());
							}
							break;
						}
					case Operands.LabelOffset6: {
							bitIndex -= 6;
							short offset6 = ((short)((instruction >> bitIndex) & 0b111111)).signExtend(6);
							short dest = (short)(context.pc + offset6);
							dest++;
							if (context.Lookup(dest, out string label)) {
								args.Add(label);
							} else {
								args.Add(offset6.ToString());
							}
							break;
						}
					case Operands.LabelOffset11: {
							bitIndex -= 11;
							short offset11 = ((short)((instruction >> bitIndex) & 0b11111111111)).signExtend(11);
							short dest = (short)(context.pc + offset11);
							dest++;
							if (context.Lookup(dest, out string label)) {
								args.Add(label);
							} else {
								args.Add(offset11.ToString());
							}
							break;
						}
				}
			}
			return string.Join(" ", args.Select(s => s.PadRight(4)));
		}
	}
	public class Reader {
		string source;

		private int _index;
		public int index {
			get => _index;
			set {
				_index = value;
				line = source.Take(index).Count(c => c == '\n');
				int lastNewline = source.LastIndexOf('\n', index != source.Length ? index : index - 1);
				if (lastNewline == -1)
					lastNewline = 0;
				column = index - lastNewline + 3*source.Substring(lastNewline, index - lastNewline).Count(c => c == '\t');

			}
		}

		public int line;
		public int column;
		
		public Reader(string source) {
			this.source = source;
			index = 0;
			line = 0;
			column = 0;
		}
		public string GetContextString(string message) {
			return $"[{line}, {column}] {message} in {GetCurrentLine()}";
		}
		public string GetCurrentLine() {
			int start = source.LastIndexOf('\n', index - 1);
			int end = source.IndexOf('\n', index);
			if(start == -1) {
				return source;
			} else if(end == -1) {
				return source.Substring(start);
			} else {
				return source.Substring(start, end - start);
			}
		}
		public TokenType Read(out string token) {
			Read:
			if (index == source.Length) {
				token = "";
				return TokenType.End;
			}
			switch (source[index]) {
				case ' ':
					ProcessChar();
					goto Read;
				case '\t':
					ProcessChar(4);
					goto Read;
				case '\n':
					ProcessNewline();
					goto Read;
				case '\r':
					ProcessChar();
					goto Read;
				case '.':
					token = ReadSubstring();
					return TokenType.Directive;
				case ';':
					token = ReadLine();
					return TokenType.Comment;
				case '"':
					ProcessChar();
					token = ReadQuoted();
					return TokenType.String;
				case '\'':
					ProcessChar();
					token = ReadChar();
					return TokenType.Symbol;
				case ',':
					ProcessChar();
					token = ",";
					return TokenType.Comma;
				default:
					token = ReadSubstring();
					return TokenType.Symbol;
			}
		}
		string ReadQuoted() {
			string result = "";
			Read:
			if (index == source.Length) {
				throw new Exception($"[{line}, {column}] Missing close quote: {result}");
			}
			switch (source[index]) {
				case '\r':
				case '\n':
					throw new Exception($"[{line}, {column}] Missing close quote: {result}");
				case '"':
					ProcessChar();
					return result;
				default:
					result += source[index];
					ProcessChar();
					goto Read;
			}
		}
		string ReadChar() {
			string result = "'";
		Read:
			if (index == source.Length) {
				throw new Exception($"[{line}, {column}] Missing close apostrophe: {result}");
			}
			switch (source[index]) {
				case '\r':
				case '\n':
					throw new Exception($"[{line}, {column}] Missing close apostrophe: {result}");
				case '\'':
					result += source[index];
					ProcessChar();
					return result;
				default:
					result += source[index];
					ProcessChar();
					goto Read;
			}
		}
		string ReadSubstring() {
			string result = "";
			Read:
			if(index == source.Length) {
				return result;
			}
			switch(source[index]) {
				case ' ':
				case '\t':
				case '\r':
				case '\n':
				case ',':
					return result;
				case ':':
					ProcessChar();
					return result;
				default:
					result += source[index];
					ProcessChar();
					goto Read;
			}
		}
		string ReadLine() {
			string result = "";
			Read:
			if (index == source.Length) {
				return result;
			}
			switch (source[index]) {
				case '\r':
				case '\n':
					return result;
				case '\t':
					ProcessChar(4);
					goto Read;
				default:
					result += source[index];
					ProcessChar();
					goto Read;
			}
		}
		void ProcessChar(int columns = 1) {
			index++;
			column += columns;
		}
		void ProcessNewline() {
			index++;
			line++;
			column = 0;
		}
	}
	public enum TokenType {
		Symbol,
		String,
		Comma,
		Directive,
		Comment,
		End
	}
}