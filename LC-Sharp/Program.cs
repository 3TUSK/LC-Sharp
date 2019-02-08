using System;
using System.Text;
using System.Threading.Tasks;

namespace LC_Sharp {
    class Program {
        //Note to self: do not use left shift to remove left bits since operands get converted to ints first
		public static void Main(string[] args) {
            new Emulator();
            //Console.WriteLine($"0b10000 = {(ushort)0b10000} => {((ushort) (0b10000)).ToSigned(5)}");
            //Console.ReadLine();

            Console.WriteLine(0b0111_1111_1111_1111_1111_1111_1111_1111 << 1);
            Console.ReadLine();

            //Console.WriteLine(((0b0000_111_0_0000_0111 << 99) >> 99).ToString("X"));
            //Console.ReadLine();
            var c = new LC3();
            var a = new Assembler(c);
            //a.Label(0x2FF0, "TEST");
            //a.AssembleToPC("BRnzp TEST");
            //a.DissembleToPC();
            //c.memory.WriteToMemory(0x3000, 0b0000_010_0_0000_0111);
            a.AssembleToPC("ADD R0, R0, #-10");
            //0b0001_000_000_1_01010
            c.Fetch();
            //c.DebugPrint();
            //Console.WriteLine($"Assembled: {a.DissembleIR()}");
            c.Execute();
            c.DebugPrint();

            a.AssembleToPC("NOT R0, R0");
            c.Fetch();
            c.Execute();
            c.DebugPrint();

            Console.ReadLine();
		}
	}    
}