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
        Label[] registerLabels;
        Label pcLabel, irLabel;
        private Button runButton;
		bool running;

        public void Update() {
            for(int i = 0; i < 8; i++) {
                var r = lc3.processing.registers[i];
                registerLabels[i].Text = $"R{i + 1} 0x{r.ToString("X").PadRight(6)} #{lc3.processing.registers[i].ToString().PadRight(6)}";
            }
            pcLabel.Text = $"PC 0x{lc3.control.pc.ToString("X").PadRight(6)} #{lc3.control.pc.ToString().PadRight(6)}";
            irLabel.Text = $"PC 0x{lc3.control.ir.ToString("X").PadRight(6)} #{lc3.control.ir.ToString().PadRight(6)}";
        }
        public Emulator() {
            Console.SetWindowSize(96, 48);
            window = new Window(new Rect(0, 0, 96, 48), "LC-Sharp");
            instructions = new ScrollView(new Rect(0, 0, 30, 44), new Rect(0, 0, 30, 440));
            instructions.ShowVerticalScrollIndicator = true;
            
			for(int i = 0; i < 0xFF; i++) {
				instructions.Add(new Label(0, i, i.ToString("X")));
			}

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
            runButton = new Button(32, 32, "Run", RunButtonClicked);
            window.Add(runButton);

        }
		public void RunButtonClicked() {
			if(running) {
				running = false;
				Application.MainLoop.RemoveIdle(RunAll);
				runButton.Text = "Stop";
			} else {
				running = true;
				Application.MainLoop.AddIdle(RunAll);
				runButton.Text = "Pause";
			}
		}
        public bool RunAll() {
            runButton.Enabled = false;
            runButton.Text += "a";
            return true;
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
