using System;
using System.IO;
using CommandLine;

namespace LC_Sharp {
    class Program {
        //Note to self: do not use left shift to remove left bits since operands get converted to ints first
		public static void Main(string[] args)
		{
			CommandLine.Parser.Default.ParseArguments<GraphicUserInterfaceOptions, CommandLineOptions>(args)
				.MapResult(
					(GraphicUserInterfaceOptions options) => GraphicUserInterfaceMain(options),
					(CommandLineOptions options) => CommandLineMain(options),
					err => // Fall back to GUI directly if not parsed.
					{
						// TODO (3TUSK): Warn users about not using `lc3 gui`?
						new Emulator().Init();
						return 0;
					}
				);
		}

		[Verb("gui", HelpText = "Launch LC-Sharp in GUI mode.")]
		private class GraphicUserInterfaceOptions
		{
			[Value(0, MetaName = "Program Source", HelpText = "Program file to be loaded")]
			public string program { get; set; }
			
			[Option("input", HelpText = "Can be used to redirect input stream, default to stdin.")]
			public string input { get; set; }
			
			[Option("output", HelpText = "Can be used to redirect output stream, defualt to stdout.")]
			public string output { get; set; }
		}

		[Verb("cli", HelpText = "Launch LC-Sharp in Command-Line mode, suitable for headless environment.")]
		private class CommandLineOptions
		{
			[Value(0, MetaName = "Program Source", HelpText = "Program file to be loaded")]
			public string program { get; set; }
			
			[Option("input", HelpText = "Can be used to redirect input stream, default to stdin.")]
			public string input { get; set; }
			
			[Option("output", HelpText = "Can be used to redirect output stream, defualt to stdout.")]
			public string output { get; set; }
		}
		
		private static int GraphicUserInterfaceMain(GraphicUserInterfaceOptions options)
		{
			new Emulator().Init();
			return 0;
		}

		private static int CommandLineMain(CommandLineOptions options)
		{
			var fsm = new LC3();
			var assembly = new Assembler(fsm);
			var insns = new[]
			{
				".ORIG x3000",
				"LEA R0, HELLO_WORLD",
				"PUTS",
				"LD R0, FOO",
				"ADD R0, R0, #15",
				"NOT R0, R0",
				"ST R0, FOO",
				"HALT",
				"HELLO_WORLD .STRINGZ \"HELLO, WORLD\"",
				"FOO .FILL #2333"
			};
			assembly.AssembleLines(File.ReadAllLines("trap.asm"));
			assembly.AssembleToPC(insns);
			Console.WriteLine("Assemble finish, launching LC3 Finite State Machine...");
			while (fsm.Active)
			{
				fsm.Fetch();
				fsm.Execute();
			}
			Console.WriteLine("LC3 Finite State Machine halted without exception.");
			return 0;
		}
	}    
}