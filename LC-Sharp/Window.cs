using NStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace LC_Sharp {
    class Emulator {
        LC3 lc3;
		Assembler assembly;
		Window window;
        ScrollView instructions;
		private Button setPCbutton;
		private TextField setPCfield;

		Label status;
        Label[] registerLabels;
		Label ccLabel;
        Label pcLabel, irLabel;
		TextView labels;
		//Label dsrLabel, ddrLabel, kbsrLabel, kbdrLabel;
        private Button runAllButton;
		private Button runStepOnceButton;
		private Button runStepOverButton;
		ushort instructionPC;

		TextView input, output, console;

		//Note: Still need Run Step Over
		bool running;

        public Emulator() {
			lc3 = new LC3();
			assembly = new Assembler(lc3);
			assembly.AssembleLines(File.ReadAllLines("../../trap.asm"));
			assembly.AssembleLines(
				".ORIG x3000",
				"GETC",
				"OUT",
				"LD R0, VALUE",
				"JSR SUBROUTINE",
				"JSR SUBROUTINE",
				"JSR SUBROUTINE",
				"JSR SUBROUTINE",
				"HALT",
				"VALUE .FILL #10",
				"SUBROUTINE ADD R0, R0, R0",
				"RET"
				);

			Console.WriteLine("Program loaded");
			Console.ReadLine();
			Console.SetWindowSize(96, 48);
            window = new Window(new Rect(0, 0, 96, 48), "LC-Sharp");
            instructions = new ScrollView(new Rect(0, 0, 30, 40), new Rect(0, 0, 30, 0xFFFF));
            instructions.ShowVerticalScrollIndicator = true;
			instructions.Scrolled += _ => UpdateInstructionsView();
			UpdateInstructionsView();
			{
                Window w = new Window(new Rect(0, 0, 32, 42), "Instructions");
                w.Add(instructions);
                window.Add(w);
            }
			setPCfield = new TextField(2, 42, 9, "");
			setPCbutton = new Button(20, 42, "Set PC", () => {
				try {
					string s = setPCfield.Text.ToString().Trim().ToUpper();
					short pc;
					if(s.StartsWith("X")) {
						pc = short.Parse(s, System.Globalization.NumberStyles.HexNumber);
					} else {
						pc = short.Parse(s);
					}
					lc3.control.setPC(pc);
					console.Text = $"{console.Text}PC set to {pc.ToHexString()}";
					UpdateRegisterView();
					UpdateInstructionsView();
				} catch(Exception e) {
					console.Text = console.Text + e.Message;
				}
				
			});
			window.Add(setPCfield, setPCbutton);

			registerLabels = new Label[8];
            for(int i = 0; i < 8; i++) {
                registerLabels[i] = new Label(1, i, $"R{i}");
            }

			ccLabel = new Label(1, 9, "CC");

            pcLabel = new Label(1, 11, "PC");
            irLabel = new Label(1, 12, "IR");
            {
                Window w = new Window(new Rect(32, 0, 32, 16), "Registers");
                w.Add(registerLabels);
				w.Add(ccLabel);
                w.Add(pcLabel, irLabel);
                window.Add(w);
            }
            Window labelsView = new Window(new Rect(32, 16, 32, 16), "Labels");
			labels = new TextView(new Rect(0, 0, 30, 14)) { Text = "", ReadOnly = true };
			labelsView.Add(labels);
            window.Add(labelsView);

			status = new Label(new Rect(32, 32, 16, 4), "");
			ResetStatus();
			window.Add(status);

            runAllButton = new Button(32, 33, "Run All", ClickRunAll);
			runStepOnceButton = new Button(32, 34, "Run Step Once", ClickRunStepOnce);
			runStepOverButton = new Button(32, 35, "Run Step Over", ClickRunStepOver);
			
			window.Add(runAllButton, runStepOnceButton, runStepOverButton);

			{
				var w = new Window(new Rect(64, 0, 42, 22), "Output");
				output = new TextView(new Rect(0, 0, 40, 20));
				output.Text = "";
				output.ReadOnly = true;
				w.Add(output);
				window.Add(w);
			}

			{
				var w = new Window(new Rect(64, 22, 42, 22), "Input");
				input = new TextView(new Rect(0, 0, 40, 20));
				input.Text = "";
				w.Add(input);
				window.Add(w);
			}

			{
				var w = new Window(new Rect(62, 44, 42, 22), "Console");
				console = new TextView(new Rect(0, 0, 40, 20));
				console.Text = "";
				console.ReadOnly = true;
				w.Add(console);
				window.Add(w);
			}

			UpdateRegisterView();
			UpdateHighlight();
			UpdateIOView();

		}
		public void ResetStatus() {
			switch(lc3.status) {
				case LC3.Status.ACTIVE:
					status.Text = "Idle".PadSurround(16, '-');
					break;
				case LC3.Status.HALT:
					status.Text = "Halt".PadSurround(16, '-');
					break;
				case LC3.Status.TRAP:
					status.Text = "Wait for TRAP".PadSurround(16, '-');
					break;
				case LC3.Status.ERROR:
					status.Text = "ERROR".PadSurround(16, '-');
					break;
			}
		}
		public void UpdateHighlight() {
			instructionPC = (ushort)lc3.control.pc; //Set highlighted instruction PC
													//Show the highlighted instruction in the center
													//Use ushort because negative short values break the instruction highlight index
			instructions.ContentOffset = new Point(0, instructionPC - instructions.Bounds.Height / 2);
			UpdateInstructionsView();
		}
		public void UpdateInstructionsView() {
			int start = -instructions.ContentOffset.Y;	//ContentOffset.Y is equal to the negative of the vertical scroll index
			instructions.RemoveAll();

			Label[] labels = new Label[40];
			for(int i = 0; i < 40; i++) {
				int index = start + i;
				labels[i] = new Label(0, index, $" {index.ToHexShort()} {assembly.Dissemble((short)index, lc3.memory.Read((short)index))}");
			}
			//Use ushort because negative short values break the index
			ushort pc = (ushort)lc3.control.pc;
			var l = labels.ElementAtOrDefault(pc - start);
			if (l != null) {
				l.TextColor = MakeColor(ConsoleColor.White);
			}

			instructions.Add(labels);
		}
		public void UpdateRegisterView() {
			for (int i = 0; i < 8; i++) {
				var r = lc3.processing.registers[i];
				registerLabels[i].Text = $"R{i} {r.ToRegisterString()}";
			}
			ccLabel.Text = $"CC {(lc3.processing.N ? 'N' : lc3.processing.Z ? 'Z' : lc3.processing.P ? 'P' : '?')}";

			pcLabel.Text = $"PC {lc3.control.pc.ToRegisterString()}";
			irLabel.Text = $"IR {lc3.control.ir.ToRegisterString()}";

			string text = "";
			foreach(var label in assembly.labels.Keys) {
				short location = assembly.labels[label];
				text += $"{location.ToHexString()} {label} {lc3.memory.Read(location).ToRegisterString()}\n";
			}
			labels.Text = text;
		}

		public static Attribute MakeColor(ConsoleColor f) {
			// Encode the colors into the int value.
			return new Attribute((int)f & 0xffff);
		}
		public void ClickRunAll() {
			if(running) {
				StopRunAll();
			} else {
				//we don't run if the lc3 is halted
				if (!lc3.Active)
					return;
				running = true;
				Application.MainLoop.AddIdle(RunAll);
				runAllButton.Text = "Pause Run All";
				status.Text = "Running All".PadSurround(16, '-');
			}
		}
		public void StopRunAll() {
			running = false;
			Application.MainLoop.RemoveIdle(RunAll);
			runAllButton.Text = "Run All";
			ResetStatus();
		}
        public bool RunAll() {
			Run();
			if(lc3.Active) {
				return true;  //We keep running as long as we are active
			} else {
				//Otherwise we are done running
				StopRunAll();
				return false;
			}
			
        }
		public void ClickRunStepOnce() {
			if (running) {
				StopRunStepOnce();
			} else {
				//we don't run if the lc3 is halted
				if (!lc3.Active)
					return;
				running = true;
				Application.MainLoop.AddIdle(RunStepOnce);
				runStepOnceButton.Text = "Pause Run Step Once";
				status.Text = "Running Step Once".PadSurround(16, '-');
			}
			void StopRunStepOnce() {
				running = false;
				Application.MainLoop.RemoveIdle(RunStepOnce);
				runStepOnceButton.Text = "Run Step Once";
				ResetStatus();
			}
			bool RunStepOnce() {
				Run();
				//If we are running a TRAP instruction, we should wait for it to finish
				if (lc3.status == LC3.Status.TRAP) {
					ResetStatus();
					return true;
				} else {
					StopRunStepOnce();
					return false;
				}
			}
		}
		public void ClickRunStepOver() {

			int pcDest = lc3.control.pc;
			if (running) {
				StopRunStepOver();
			} else {
				//we don't run if the lc3 is halted
				if (!lc3.Active)
					return;
				running = true;
				Application.MainLoop.AddIdle(RunStepOver);
				runStepOverButton.Text = "Pause Run Step Over";
				status.Text = "Running Step Over".PadSurround(16, '-');
			}

			void StopRunStepOver() {
				running = false;
				Application.MainLoop.RemoveIdle(RunStepOver);
				runStepOverButton.Text = "Run Step Over";
				ResetStatus();
			}
			bool RunStepOver() {
				Run();
				//If we are running a TRAP instruction, we should wait for it to finish
				if (lc3.status == LC3.Status.TRAP) {
					ResetStatus();
					return true;
				} else if (lc3.control.pc != pcDest) {
					return true;
				} else {
					StopRunStepOver();
					return false;
				}
			}
		}
		public void Run() {
			lc3.Fetch();

			if (lc3.control.ir == 0) {
				lc3.status = LC3.Status.ERROR;
				return;
			}

			lc3.Execute();

			//We execute TRAP instructions like regular subroutines but without updating the instruction highlight
			//Don't update if we just HALTed
			if (lc3.Active && lc3.status != LC3.Status.TRAP) {
				//Don't update the highlight if we're executing a subroutine
				//Set the instructions pane to highlight the current PC
				UpdateHighlight();
				//Do not update register view during a TRAP subroutine, but only if we don't watch them run
				UpdateRegisterView();
			}
			UpdateIOView();
		}
		public void UpdateIOView() {
			short kbsr = unchecked((short)0xFE00);
			short kbdr = unchecked((short)0xFE02);

			//See if KBSR is waiting for input
			if (lc3.memory.Read(kbsr) == 1) {
				//Check if we have input ready
				//For some reason, the box text always contains two invisible newline characters
				if (input.Text.Length > 2) {
					lc3.memory.Write(kbsr, unchecked((short)0xFFFF));           //Set KBSR ready
					lc3.memory.Write(kbdr, input.Text[0]);  //Write in the first character from input window
					//For some reason, the box text always contains two newline characters
					input.Text = input.Text.ToString().Substring(1, Math.Max(0, input.Text.Length - 3));   //Consume the first character from input window
					input.SetNeedsDisplay();
				}
			}



			short dsr = unchecked((short)0xFE04);
			short ddr = unchecked((short)0xFE06);

			//DSR is waiting for output
			if (lc3.memory.Read(dsr) == 1) {
				char c = (char)lc3.memory.Read(ddr);        //Read char from DDR
				if(c != 0) {
					output.Text = (output.Text.ToString().Substring(0, output.Text.Length - 2) + c);   //Send char to output window
					output.SetNeedsDisplay();
				}
			}
			lc3.memory.Write(dsr, unchecked((short)0xFFFF));              //Set DSR ready
		}
		public void Init() {
            //Application.UseSystemConsole = true;
            Application.Init();
            var top = Application.Top;
            top.Add(window);
            Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(1000/60), _ => true); //Hack to keep the window updating without touching the keyboard or mouse
            Application.Run(window);
		}
    }
}
