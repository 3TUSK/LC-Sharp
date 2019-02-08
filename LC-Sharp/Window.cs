using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace LC_Sharp {
    class Emulator {
        LC3 lc3;
        Window window;
        Label[] registerLabels;
        Label pcLabel, irLabel;

        public void Update() {
            for(int i = 0; i < 8; i++) {
                var r = lc3.processing.registers[i];
                registerLabels[i].Text = $"R{i + 1} 0x{r.ToString("X").PadRight(6)} #{lc3.processing.registers[i].ToString().PadRight(6)}";
            }
            pcLabel.Text = $"PC 0x{lc3.control.pc.ToString("X").PadRight(6)} #{lc3.control.pc.ToString().PadRight(6)}";
            irLabel.Text = $"PC 0x{lc3.control.ir.ToString("X").PadRight(6)} #{lc3.control.ir.ToString().PadRight(6)}";
        }
        public Emulator() {
            window = new Window("LC-Sharp");
            registerLabels = new Label[8];
            for(int i = 0; i < 8; i++) {
                registerLabels[i] = new Label(1, i + 1, $"R{i+1}");
            }
            window.Add(registerLabels);
        }
        public void Init() {
            Application.Init();
            
            {
                var top = Application.Top;
                top.Add(win);
            }

            Application.Run();
        }
    }
}
