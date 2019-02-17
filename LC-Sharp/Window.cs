using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace LC_Sharp {
    class Emulator {
        LC3 lc3;
        Window window;
        ScrollView instructions;
		Label status;
        Label[] registerLabels;
        Label pcLabel, irLabel;
        private Button runAllButton;
		private Button runStepButton;
		//Note: Still need Run Step Over
		bool running;

        public Emulator() {
			lc3 = new LC3();
			Console.SetWindowSize(96, 48);
            window = new Window(new Rect(0, 0, 96, 48), "LC-Sharp");
            instructions = new ScrollView(new Rect(0, 0, 30, 42), new Rect(0, 0, 30, 0xFF));
            instructions.ShowVerticalScrollIndicator = true;
			instructions.Scrolled += _ => UpdateInstructionsView();
			UpdateInstructionsView();
			{
                Window w = new Window(new Rect(0, 0, 32, 46), "Instructions");
                w.Add(instructions);
                window.Add(w);
            }

			registerLabels = new Label[8];
            for(int i = 0; i < 8; i++) {
                registerLabels[i] = new Label(1, i, $"R{i+1}");
            }
            pcLabel = new Label(1, 9, "PC");
            irLabel = new Label(1, 10, "IR");
            {
                Window w = new Window(new Rect(32, 0, 16, 16), "Registers");
                w.Add(registerLabels);
                w.Add(pcLabel, irLabel);
                window.Add(w);
            }
            Window labels = new Window(new Rect(32, 16, 16, 16), "Labels");
            window.Add(labels);

			status = new Label(new Rect(32, 32, 16, 4), "");
			ResetStatus();
			window.Add(status);

            runAllButton = new Button(32, 33, "Run All", ClickRunAll);
			runStepButton = new Button(32, 34, "Run Step", ClickRunStep);
			window.Add(runAllButton);
			window.Add(runStepButton);
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
					status.Text = "Waiting".PadSurround(16, '-');
					break;
				case LC3.Status.ERROR:
					status.Text = "ERROR".PadSurround(16, '-');
					break;
			}
		}
		public void UpdateRegisterView() {
			for (int i = 0; i < 8; i++) {
				var r = lc3.processing.registers[i];
				registerLabels[i].Text = $"R{i + 1} {r.ToRegisterString()}";
			}
			pcLabel.Text = $"PC {lc3.control.pc.ToRegisterString()}";
			pcLabel.Text = $"IR {lc3.control.ir.ToRegisterString()}";
		}
		public void UpdateInstructionsView() {
			int start = -instructions.ContentOffset.Y;	//ContentOffset.Y is equal to the negative of the vertical scroll index
			instructions.RemoveAll();
			for(int i = 0; i < 40; i++) {
				int index = start + i;
				instructions.Add(new Label(0, index, index.ToHexShort()));
			}
		}
		public void ClickRunAll() {
			if(running) {
				StopRunAll();
			} else {
				//we don't run if the lc3 is halted
				if (lc3.status == LC3.Status.HALT)
					return;
				running = true;
				Application.MainLoop.AddIdle(RunAll);
				runAllButton.Text = "Pause";
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
			if(lc3.status != LC3.Status.HALT && lc3.status != LC3.Status.ERROR) {
				return true;  //We keep running as long as we are active
			} else {
				//Otherwise we are done running
				StopRunAll();
				return false;
			}
			
        }
		public void ClickRunStep() {
			if (running) {
				StopRunStep();
			} else {
				//we don't run if the lc3 is halted
				if (lc3.status == LC3.Status.HALT || lc3.status == LC3.Status.ERROR)
					return;
				running = true;
				Application.MainLoop.AddIdle(RunStep);
				runStepButton.Text = "Pause";
				status.Text = "Running Step".PadSurround(16, '-');
			}
		}
		public void StopRunStep() {
			running = false;
			Application.MainLoop.RemoveIdle(RunStep);
			runStepButton.Text = "Run Step";
			ResetStatus();
		}
		public bool RunStep() {
			Run();
			//If we are running a TRAP instruction, we should wait for it to finish
			if (lc3.status == LC3.Status.TRAP) {
				return true;
			} else {
				StopRunStep();
				return false;
			}
		}
		public void Run() {
			//If we are running a TRAP instruction, we rerun it until it's done
			if (lc3.status == LC3.Status.TRAP) {
				lc3.Fetch();
			}

			if(lc3.control.ir == 0) {
				lc3.status = LC3.Status.ERROR;
				return;
			}

			lc3.Execute();
		}
        public void Init() {
            //Application.UseSystemConsole = true;
            Application.Init();
            var top = Application.Top;
            top.Add(window);
            Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(1000/60), _ => true); //Hack to keep the window updating without touching the keyboard or mouse
            Application.Run(window);

            instructions.CanFocus = true;
		}
    }
}
