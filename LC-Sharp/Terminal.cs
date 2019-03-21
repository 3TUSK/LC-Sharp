using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC_Sharp {
	class Terminal
	{
		private const short KBSR = unchecked((short) 0xFE00);
		private const short KBDR = unchecked((short) 0xFE02);
		private const short DSR = unchecked((short) 0xFE04);
		private const short DDR = unchecked((short) 0xFE06);

		private LC3 lc3;
		private Assembler assembly;

		string fixedInput;
		string fixedOutput;

		public Terminal(LC3 lc3, Assembler assembly, string fixedInput = null, string fixedOutput = null) {
			this.lc3 = lc3;
			this.assembly = assembly;
			this.fixedInput = fixedInput;
			this.fixedOutput = fixedOutput;
		}
		public void Start() {
			Console.CursorVisible = false;
			while (lc3.Active) {
				lc3.Fetch();
				lc3.Execute();

				//See if KBSR is waiting for input
				if (lc3.memory.Read(KBSR) == 1) {

					Console.CursorVisible = true;
					lc3.memory.Write(KBSR, unchecked((short)0xFFFF));           //Set KBSR ready
					char c;

					if(fixedInput != null) {
						//Read from fixed input
						if(fixedInput.Length > 0) {
							//We still have input left
							c = fixedInput[0];
							fixedInput = fixedInput.Substring(1);
						} else {
							Console.WriteLine();
							Console.WriteLine("Error: Program ran out of input.");
							break;
						}
					} else {
						//Read from console
						GetKey:
						ConsoleKeyInfo k = Console.ReadKey(true);
						if (k.Key == ConsoleKey.Enter) {
							c = '\n';
						} else if (k.Key == ConsoleKey.Backspace) {
							goto GetKey;
						} else {
							c = k.KeyChar;
						}
					}
					
					lc3.memory.Write(KBDR, (short)c);  //Write in the first character from input window
					Console.CursorVisible = false;
				}

				//DSR is waiting for output
				if (lc3.memory.Read(DSR) == 1) {
					char c = (char)lc3.memory.Read(DDR);        //Read char from DDR
					if (c != 0) {
						//Write to console
						if(c == '\n') {
							Console.WriteLine();
						} else {
							Console.Write(c);
						}

						//Check if we have fixed output to check against
						if(fixedOutput != null) {
							if(fixedOutput.Length > 0) {
								if(fixedOutput[0] != c) {
									Console.WriteLine();
									Console.WriteLine("Error: Program output did not match fixed output.");
								}
								//Remove the first char
								fixedOutput = fixedOutput.Substring(1);
							} else {
								Console.WriteLine();
								Console.WriteLine("Error: Program had more output than expected.");
							}
						}
					}
				}
				lc3.memory.Write(DSR, unchecked((short)0xFFFF));              //Set DSR ready

			}
			Console.WriteLine("Press Enter to exit");
			Console.ReadLine();
		}
	}
}
