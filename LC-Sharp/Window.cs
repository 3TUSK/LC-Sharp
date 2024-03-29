﻿using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace LC_Sharp {
	class Emulator {
        LC3 lc3;
		Assembler assembly;
		Window window;

		string programFile;

        ScrollView instructions;

		Label status;
        Label[] registerLabels;
		Label ccLabel;
        Label pcLabel, irLabel;
		TextView labels;
		//Label dsrLabel, ddrLabel, kbsrLabel, kbdrLabel;
        private Button runAllButton;
		private Button runStepOnceButton;
		private Button runStepOverButton;

		private Button setPCbutton;
		private TextField setPCfield;

		private Button setScrollButton;
		private TextField setScrollField;

		private Button assembleDebugButton;
		private TextView assembleDebugField;

		ushort instructionPC;

		TextView input, output, console;

		public string FixedInput {
			set {
				input.Text = value;
			}
		}
		public string FixedOutput {
			set {

			}
		}

		//Note: Still need Run Step Over
		bool running;

		public Emulator(LC3 lc3, Assembler assembly, string programFile = "") {
			this.lc3 = lc3;
			this.assembly = assembly;
			this.programFile = programFile;
		}

		public void InitWindow() {
			//Console.WriteLine("Program loaded. Press Enter to continue.");
			//Console.ReadLine();
			Console.SetWindowSize(150, 60);
			window = new Window(new Rect(0, 0, 150, 60), "LC-Sharp");

            {
				TextField fileField = new TextField(0, 0, 56, programFile ?? "");
				window.Add(fileField);

				bool loading = false;

				var loadingLabel = new Label(56, 0, "Loading...");
				window.Add(loadingLabel);

				Button loadButton = new Button(56, 0, "Load");
				loadButton.Clicked += LoadProgramClick;
				window.Add(loadButton);

				var lastLoad = programFile != null ? File.GetLastWriteTimeUtc(programFile) : DateTime.Now;

				bool autoReload = false;
				CheckBox autoReloadBox = new CheckBox(66, 0, "Auto Reload", false);
				autoReloadBox.Toggled += b => {
					if(b) {
						autoReload = false;
					} else {
						autoReload = true;
						Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(1000), m => {
							if (autoReload) {
								if(!loading && fileField.Text.Any() && File.Exists(fileField.Text.ToString())) {
									var time = File.GetLastWriteTimeUtc(fileField.Text.ToString());
									if (time != lastLoad) {
										lastLoad = time;
										LoadProgramClick();
									}
								}
								
								return true;
							} else {
								return false;
							}
						});
					}
				};
				window.Add(autoReloadBox);



				var clearOutput = new Button(88, 0, "Clear Output");
				clearOutput.Clicked += () => {
					output.Text = "";
				};
				window.Add(clearOutput);

				if (programFile != null) {
					Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(0), m => {
						LoadProgramClick();
						return false;
					});
				}

				void LoadProgramClick() {
					if (loading) {
						return;
					}
					loading = true;

					fileField.SetFocus();

					loadingLabel.Visible = true;
					loadButton.Visible = false;
					
					new Thread(() => {
						try {
							LoadProgram();
						} catch (Exception e) {
							WriteConsole(e.Message);
						} finally {
							loadingLabel.Visible = false;
							loadButton.Visible = true;

							loading = false;
						}
					}).Start();
				}
				void LoadProgram() {
					var programFile = fileField.Text.ToString();
					var program = File.ReadAllText(programFile);
					var lc3 = new LC3();
					var assembly = new Assembler(lc3);
					assembly.AssembleLines(File.ReadAllText("trap.asm"));
					assembly.AssembleLines(program);

					//output.Text = "";
					//input.Text = "";
					//console.Text = "";
					//assembleDebugField.Text = "";

					//assembleDebugField.Text = program;

					this.lc3 = lc3;
					this.assembly = assembly;
					this.programFile = programFile;

					UpdateRegisterView();
					UpdateHighlight();
					UpdateIOView();

					UpdateInstructionsView();
					ResetStatus();

					WriteConsole($"[{DateTime.Now.ToShortTimeString()}] Loaded file {programFile}");
				}
			}
			int x = 0;
			int width = 56;
			instructions = new ScrollView(new Rect(0, 0, width-2, 40)) { ContentSize = new Size(30, 0xFFFF) };
			instructions.ShowVerticalScrollIndicator = true;
			instructions.MouseClick += _ => UpdateInstructionsView();
			UpdateInstructionsView();
			{
				Window w = new Window(new Rect(x, 1, width, 42), "Instructions");
				w.Add(instructions);
				window.Add(w);
			}

			{
				var w = new Window(new Rect(x, 43, width + 32, 15), "Assemble");
				assembleDebugField = new TextView(new Rect(0, 0, 62, 13));
				/*
				try {
					var program = File.ReadAllText(programFile);
					assembleDebugField.Text = program.Replace("\r", null);
				} catch (Exception e) {
					assembleDebugField.Text = "";
				}
				*/
				assembleDebugField.Text = "";
				w.Add(assembleDebugField);
				window.Add(w);
			}


			registerLabels = new Label[8];
			for (int i = 0; i < 8; i++) {
				//We need to make the label 30-wide on init because it won't expand
				Label l = new Label(1, i, $"R{i}".PadRight(30));
				registerLabels[i] = l;
			}

			ccLabel = new Label(1, 9, "CC".PadRight(30));

			pcLabel = new Label(1, 11, "PC".PadRight(30));
			irLabel = new Label(1, 12, "IR".PadRight(30));

			x += width;
			width = 32;
			{
				Window w = new Window(new Rect(x, 1, width, 16), "Registers");
				w.Add(registerLabels);
				w.Add(ccLabel);
				w.Add(pcLabel, irLabel);
				window.Add(w);
			}
			Window labelsView = new Window(new Rect(x, 17, width, 16), "Labels");
			labels = new TextView(new Rect(0, 0, width-2, 14)) { Text = "", ReadOnly = true };
			labelsView.Add(labels);
			window.Add(labelsView);

			status = new Label(new Rect(x, 33, 16, 4), "Status");
			ResetStatus();
			window.Add(status);

			runAllButton = new Button(x, 34, "Run All");
			runAllButton.Clicked += ClickRunAll;
			runStepOnceButton = new Button(x, 35, "Run Step Once");
			runStepOnceButton.Clicked += ClickRunStepOnce;
			runStepOverButton = new Button(x, 36, "Run Step Over");
			runStepOverButton.Clicked += ClickRunStepOver;

			setPCfield = new TextField(x + 22, 37, 9, "");
			setPCbutton = new Button(x, 37, "Set PC");
			setPCbutton.Clicked += SetPC;

			void SetPC() {
				try {
					string s = setPCfield.Text.ToString().Trim().ToLower();
					short pc;
					if (s.StartsWith("0x")) {
						pc = short.Parse(s.Substring(2), System.Globalization.NumberStyles.HexNumber);
					} else {
						pc = short.Parse(s.Substring(1));
					}

					if(pc == lc3.control.pc) {
						return;
                    }
					lc3.control.setPC(pc);
					WriteConsole($"PC set to {pc.ToHexString()}");
					lc3.Unhalt();
					UpdateRegisterView();
					UpdateHighlight();
					ResetStatus();
				} catch (Exception e) {
					WriteConsole(e.Message);
				}
			}

			setScrollField = new TextField(x + 22, 38, 9, "");
			setScrollButton = new Button(x, 38, "Set Scroll");
			setScrollButton.Clicked += SetScroll;

			void SetScroll() {
				try {
					string s = setScrollField.Text.ToString().Trim().ToLower();
					short pc;
					if (s.StartsWith("0x")) {
						pc = short.Parse(s.Substring(2), System.Globalization.NumberStyles.HexNumber);
					} else {
						pc = short.Parse(s.Substring(1));
					}

					instructions.ContentOffset = new Point(0, pc - instructions.Bounds.Height / 2);
					UpdateInstructionsView();
				} catch (Exception e) {
					WriteConsole(e.Message);
				}
			}

			assembleDebugButton = new Button(x, 39, "Assemble at PC");
			assembleDebugButton.Clicked += AssembleDebug;
			void AssembleDebug() {
				try {


					//assembly.AssembleToPC(assembleDebugField.Text.ToString().Replace("\r", "").Split('\n'));
					//Create a new assembler so we can redefine labels
					new Assembler(lc3).AssembleToPC(assembleDebugField.Text.ToString().Replace("\r", "").Split('\n'));
					instructions.ContentOffset = new Point(0, lc3.control.pc - instructions.Bounds.Height / 2);
					UpdateInstructionsView();
					WriteConsole("Debug code assembled successfully.");
				} catch (Exception e) { WriteConsole(e.Message); }
			}

			window.Add(runAllButton, runStepOnceButton, runStepOverButton, setPCbutton, setPCfield, setScrollButton, setScrollField, assembleDebugButton);

			x += width;
			width = 58;
			{
				var w = new Window(new Rect(x, 1, width, 16), "Output");
				output = new TextView(new Rect(0, 0, width-2, 14));
				output.Text = "";
				output.ReadOnly = true;
				w.Add(output);
				window.Add(w);
			}

			{
				var w = new Window(new Rect(x, 17, width, 16), "Input");
				input = new TextView(new Rect(0, 0, width-2, 14));
				input.Text = "";
				w.Add(input);
				window.Add(w);
			}

			{
				var w = new Window(new Rect(x, 33, width, 25), "Console");
				console = new TextView(new Rect(0, 0, width-2, 24));
				console.Text = "";
				console.ReadOnly = true;
				w.Add(console);
				window.Add(w);
			}

			UpdateRegisterView();
			UpdateHighlight();
			UpdateIOView();
		}

		public void WriteConsole(string text) {
			console.Text = $"{console.Text}{text}\n".Replace("\r", "").Replace("\t", "    ");
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
				labels[i] = new Label(0, index, $"{(assembly.breakpoints.Contains((short)index) ? "*" : " ")}{index.ToHexShort()} {assembly.Dissemble((short)index, lc3.memory.Read((short)index))}");
			}
			//Update the highlight
			//Use ushort because negative short values break the index
			ushort pc = (ushort)lc3.control.pc;
			var l = labels.ElementAtOrDefault(pc - start);
			if (l != null) {
				l.ColorScheme = new ColorScheme() {
					Normal = MakeColor(ConsoleColor.White),
				};
			}

			instructions.Add(labels);
		}
		public void UpdateRegisterView() {
			for (int i = 0; i < 8; i++) {
				var r = lc3.processing.registers[i];
				var s = $"R{i} {r.ToRegisterString()}";
				registerLabels[i].Text = s;
			}
			ccLabel.Text = $"CC {(lc3.processing.N ? 'N' : lc3.processing.Z ? 'Z' : lc3.processing.P ? 'P' : '?')}";
			
			pcLabel.Text = $"PC {lc3.control.pc.ToRegisterString()}";
			
			irLabel.Text = $"IR {lc3.control.ir.ToRegisterString()}";
			

			//Auto update the setter fields
			setPCfield.Text = $"{lc3.control.pc.ToHexString()}";
			setScrollField.Text = $"{lc3.control.pc.ToHexString()}";

			string text = "";
			foreach(var label in assembly.labels.Keys) {
				short location = assembly.labels[label];
				if(((short) (location - lc3.control.pc)).WithinRange(11))
					text += $"{location.ToHexString()} {label} {lc3.memory.Read(location).ToRegisterString()}\n";
			}
			labels.Text = text;
		}

		public static Attribute MakeColor(ConsoleColor f) {
			// Encode the colors into the int value.
			return new Attribute((int)f & 0xffff);
		}
		public void AddTimer(Func<bool> f) {
			Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(10), (m) => {
				int i = 100;
				bool result;
				do {
					result = f();
				} while (result && i-- > 0);
				return result;
			});
		}

		public void ClickRunAll() {
			if(running) {
				StopRunAll();
			} else {
				//we don't run if the lc3 is halted
				if (!lc3.Active) {
					WriteConsole($"Machine is halted");
					return;
				}
				running = true;
				//Application.MainLoop.AddIdle(RunAll);
				AddTimer(RunAll);


				runAllButton.Text = "Pause Run All";
				status.Text = "Running All".PadSurround(16, '-');
			}
			void StopRunAll() {
				running = false;
				Application.MainLoop.RemoveIdle(RunAll);
				runAllButton.Text = "Run All";
				ResetStatus();
			}
			bool RunAll() {
				Run();
				//Stop running if needed
				if (!running) {
					StopRunAll();
					WriteConsole($"Machine paused");
					return false;
				}

				if (lc3.Active) {
					return true;  //We keep running as long as we are active
				} else {
					//Otherwise we are done running
					StopRunAll();
					WriteConsole($"Program halted");
					return false;
				}
			}
		}
		public void ClickRunStepOnce() {
			if (running) {
				StopRunStepOnce();
			} else {
				//we don't run if the lc3 is halted
				if (!lc3.Active) {
					WriteConsole($"Program is halted");
					return;
				}
				running = true;
				//Application.MainLoop.AddIdle(RunStepOnce);
				AddTimer(RunStepOnce);

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
				//Stop running if needed
				if (!running) {
					StopRunStepOnce();
					WriteConsole($"Program paused");
					return false;
				}

				//If we are running a TRAP instruction, we should wait for it to finish
				if (lc3.status == LC3.Status.TRAP) {
					ResetStatus();
					WriteConsole($"Entered TRAP");
					return true;
				} else {
					StopRunStepOnce();
					WriteConsole($"Program paused");
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
				if (!lc3.Active) {
					WriteConsole($"Program is halted");
					return;
				}
				running = true;
				//Application.MainLoop.AddIdle(RunStepOver);
				AddTimer(RunStepOver);

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
				//Stop running if needed
				if(!running) {
					StopRunStepOver();
					WriteConsole($"Program paused");
					return false;
				}
				//If we are running a TRAP instruction, we should wait for it to finish
				if (lc3.status == LC3.Status.TRAP) {
					ResetStatus();
					WriteConsole($"Entered TRAP");
					return true;
				} else if (lc3.control.pc != pcDest) {
					return true;
				} else {
					StopRunStepOver();
					WriteConsole($"Program paused");
					return false;
				}
			}
		}
		public void Run() {
			if(lc3.memory.Read(lc3.control.pc) == 0) {
				console.Text = $"{console.Text.ToString().Replace("\r", "")}Warning: NOP instruction at {lc3.control.pc.ToHexString()};\nDid you forget a RET or HALT?\n";
				goto Error;
			} else if(assembly.nonInstruction.Contains(lc3.control.pc)) {
				console.Text = $"{console.Text.ToString().Replace("\r", "")}Warning: non-instruction data at {lc3.control.pc.ToHexString()};\nDid you forget a RET or HALT?\n";
				goto Error;
			}

			lc3.Fetch();

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

			if (assembly.breakpoints.Contains(lc3.control.pc)) {
				console.Text = $"{console.Text.ToString().Replace("\r", "")}Reached breakpoint at {lc3.control.pc.ToHexString()};\nProgram paused.\n";
				running = false;
				return;
			}

			return;
			Error:

			lc3.Fetch();
			UpdateHighlight();
			running = false;
			return;
		}
		public void UpdateIOView() {
			short kbsr = unchecked((short)0xFE00);
			short kbdr = unchecked((short)0xFE02);

			//See if KBSR is waiting for input
			if (lc3.memory.Read(kbsr) == 1) {
				//Check if we have input ready
				if (input.Text.Length > 0) {

					var text = input.Text.ToString().Replace("\r", null);
					lc3.memory.Write(kbsr, unchecked((short)0xFFFF));           //Set KBSR ready
					lc3.memory.Write(kbdr, (short)text[0]);  //Write in the first character from input window
					//For some reason, the box text always contains two newline characters
					input.Text = text.Substring(1);   //Consume the first character from input window
					input.SetNeedsDisplay();
				}
			}



			short dsr = unchecked((short)0xFE04);
			short ddr = unchecked((short)0xFE06);

			//DSR is waiting for output
			if (lc3.memory.Read(dsr) == 1) {
				char c = (char)lc3.memory.Read(ddr);        //Read char from DDR
				if(c != 0) {
					output.Text = output.Text.ToString().Replace("\r", null) + c;   //Send char to output window
					output.SetNeedsDisplay();
				}
			}
			lc3.memory.Write(dsr, unchecked((short)0xFFFF));              //Set DSR ready
		}
		public void Start() {
            //Application.UseSystemConsole;
            Application.Init();


			InitWindow();
			var top = new Toplevel(window.Frame);
			top.Add(window);

			//top.Add(new Window() { X = 0, Y = 0, Width = 5, Height = 10 });
			Application.Run(top);
		}
    }
}
