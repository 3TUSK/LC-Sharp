using System;
using System.Collections.Generic;
using CommandLine;

namespace LC_Sharp
{
    namespace CommandLineInterface
    {
        public class Options
        {
            [Option("debug", Required = false, HelpText = "Will create verbose message")]
            public bool debug { get; set; }
            
            [Value(0)]
            public string program { get; set; }
            
            [Value(1)]
            public string input { get; set; }
            
            [Value(2)]
            public string output { get; set; }
        }
        
        public class LC3CommandLine
        {
            public static void StartCommandLine(string[] args) // It's not Main because there can only be one Main method.
            {
                global::CommandLine.Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(RunLC3)
                    .WithNotParsed(PropagateError);
            }

            private static void RunLC3(Options options)
            {
                var lc3 = new LC3();
                var program = new NeoAssembler(new Parser().ParseFile(options.program)).Assemble();
                foreach (var mem in program)
                {
                    lc3.memory.Write(mem.Key, mem.Value);
                }

                while (lc3.Active)
                {
                    lc3.Execute();
                }

                if (lc3.status == LC3.Status.ERROR)
                {
                    Console.Error.Write("Program unexpectedly exits");
                }
            }

            private static void PropagateError(IEnumerable<Error> errors)
            {
                foreach (var error in errors)
                {
                    Console.Error.WriteLine(error.ToString());
                }
            }
        }
    }
    
}