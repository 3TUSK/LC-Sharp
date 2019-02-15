using System;
using System.Text;
using System.Threading.Tasks;

namespace LC_Sharp {
    class Program {
        //Note to self: do not use left shift to remove left bits since operands get converted to ints first
		public static void Main(string[] args) {
            new Emulator().Init();
            /*

            var c = new LC3();
            var a = new Assembler(c);
            a.AssembleToPC("ADD R7, R7, #1");
            c.Fetch();
            c.DebugPrint();
            c.Execute();
            c.DebugPrint();

            //a.AssembleToPC("NOT R0, R0");
            a.AssembleToPC("JSRR R0");
            c.Fetch();
            c.Execute();
            c.DebugPrint();

            Console.ReadLine();
            */
		}
	}    
}