using CommandLine;
using System;
using System.IO;

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
						if (Environment.UserInteractive)
						{
							Console.Error.WriteLine("No mode specified or invalid options occured, fallback to GUI mode...");
							var lc3 = new LC3();
							var assembly = new Assembler(lc3);
							assembly.AssembleLines(File.ReadAllText("trap.asm"));
							var program = @"C:\Users\alexm\source\repos\cs061\assn_mu_solution.asm";
							assembly.AssembleLines(File.ReadAllText(program));
							new Emulator(lc3, assembly, program).Start();
							return 0;
						}
						else
						{
							Console.Error.WriteLine("No mode specified or invalid options occured, fallback to CLI mode...");
							CommandLineMain(new CommandLineOptions()); // TODO (3TUSK): Provide fallback parameters
							return 0;
						}
					}
				);
		}

		[Verb("gui", HelpText = "Launch LC-Sharp in GUI mode.")]
		private class GraphicUserInterfaceOptions
		{
			[Value(0, MetaName = "Program Source", HelpText = "Program file to be loaded")]
			public string program { get; set; }
			
			[Option("input", HelpText = "Set input to the contents of a given file.")]
			public string input { get; set; }
			
			[Option("output", HelpText = "Can be used to redirect output stream, defualt to stdout.")]
			public string output { get; set; }
		}

		[Verb("cli", HelpText = "Launch LC-Sharp in Command-Line mode, suitable for headless environment.")]
		private class CommandLineOptions
		{
			[Value(0, MetaName = "Program Source", HelpText = "Program file to be loaded")]
			public string program { get; set; }
			
			[Option("input", HelpText = "Set input to the contents of a given file.")]
			public string input { get; set; }
			
			[Option("output", HelpText = "Expect output according to the contents of a given file.")]
			public string output { get; set; }
		}
		
		private static int GraphicUserInterfaceMain(GraphicUserInterfaceOptions options)
		{
			if (!Environment.UserInteractive)
			{
				Console.Error.WriteLine("It looks like you are attempting running LC-Sharp GUI mode in a headless environment.");
				Console.Error.WriteLine("Naturally, it does not make much senses. LC-Sharp will quit now to prevent further errors.");
				return -1;
			}
			LC3 lc3 = new LC3();
			Assembler assembly = new Assembler(lc3);
			assembly.AssembleLines(File.ReadAllLines("trap.asm"));
			assembly.AssembleLines(File.ReadAllLines(options.program));
			new Emulator(lc3, assembly).Start();
			return 0;
		}

		private static int CommandLineMain(CommandLineOptions options)
		{
			var lc3 = new LC3();
			var assembly = new Assembler(lc3);
			assembly.AssembleLines(File.ReadAllLines("trap.asm"));
			if (options.program != null) {
				assembly.AssembleToPC(File.ReadAllLines(options.program));
			}
			string input = options.input != null ? File.ReadAllText(options.input) : null;
			string output = options.output != null ? File.ReadAllText(options.output) : null;
			Console.WriteLine("Assembly successful, launching LC3 Finite State Machine...");
			Console.CursorVisible = false;
			new Terminal(lc3, assembly, input, output).Start();
			Console.WriteLine("LC3 Finite State Machine halted without exception.");
			return 0;
		}
	}    
}