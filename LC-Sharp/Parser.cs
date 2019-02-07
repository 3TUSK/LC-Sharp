using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LC_Sharp
{
    public class ParserController
    {
        // TODO This actually needs a proper registry
        private static readonly HashSet<string> instr = new HashSet<string>
        {
            "ADD",
            "AND",
            "BR", // Split into 8 variants?
            "JMP",
            "JSR", "JSRR",
            "LD", "LDI", "LDR",
            "LEA",
            "NOT",
            "RET",
            "RTI",
            "ST", "STI", "STR",
            "TRAP",
                
            "GETC",   // TRAP x20
            "OUT",    // TRAP x21
            "PUTS",   // TRAP x22
            "IN",     // TRAP x23
            "PUTSP",  // TRAP x24
            "HALT"    // TRAP x25
        };

        private uint sourceLine; // Record this for debug purpose
        private ushort position; // Used for calculating memory offset, used by .ORIGIN pseudo-instruction
        private string sourceInst = "";
        private List<string> comments = new List<string>();
        private List<string> labels = new List<string>();
        private readonly List<InstructionEntry> instructions = new List<InstructionEntry>();

        private bool isComment;
        private bool isInstruction;
        private bool isFinished; // Used by .END pseudo-instruction
        private string currentComment = "";

        public void BeginLine()
        {
            sourceLine++;
        }
        
        public void Accept(string token)
        {
            if (isComment)
            {
                currentComment += " ";
                currentComment += token;
            }
            else if (token[0] == ';')
            {
                isComment = true;
                isInstruction = false;
                currentComment += token;
            }
            else
            {
                ConstructInstruction();
                if (isInstruction)
                {
                    sourceInst += token;
                }
                if (instr.Contains(token.ToUpper()))
                {
                    isComment = false;
                    isInstruction = true;
                    sourceInst += token;
                    sourceInst += " "; // TODO 
                }
                else
                {
                    labels.Add(token);
                }
            }
        }

        private void ConstructInstruction()
        {
            instructions.Add(new InstructionEntry(position, sourceInst, labels, comments, sourceLine));
            position++;
            sourceInst = "";
            labels = new List<string>();
            comments = new List<string>();
        }

        public void EndLine()
        {
            comments.Add(currentComment);
            currentComment = "";
            isComment = false;
            isInstruction = false;
        }

        public void Terminate()
        {
            isFinished = true;
        }

        public bool isTerminated()
        {
            return isFinished;
        }

        public ParsedFile Assemble()
        {
            Dictionary<ushort, InstructionEntry> instructions =
                this.instructions.ToDictionary(i => i.GetAddress(), i => i);
            Dictionary<ushort, List<string>> reversedLabelLookup =
                this.instructions.ToDictionary(i => i.GetAddress(), i => i.GetLabels());
            Dictionary<string, ushort> labelLookup = new Dictionary<string, ushort>();
            foreach (var entry in reversedLabelLookup)
            {
                foreach (var label in entry.Value)
                {
                    labelLookup[label] = entry.Key;
                }
            }
            return new ParsedFile(instructions, labelLookup, reversedLabelLookup);
        }
    }
    
    public class Parser
    {
        // Read the source file from given path, and perform only the first round of assembly.
        // The parsed result can be later used directly for second round of assembly.
        public ParsedFile Parse(string path)
        {           
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            var controller = new ParserController();
            // Use index-based loop for deterministic iteration order
            for (var i = 0; i < lines.Length; i++)
            {
                var tokens = lines[i].Split(' ');
                controller.BeginLine();
                // Same above
                for (var j = 0; j < tokens.Length; j++)
                {
                    controller.Accept(tokens[j]);
                }
                controller.EndLine();

                if (controller.isTerminated()) // Can happen if there is .END pseudo-instruction
                {
                    break;
                }
            }

            return controller.Assemble();
        }
        
        public ParsedFile Parse(FileStream input)
        {
            // TODO Looks like there isn't a convenient method to read all lines from FileStream
            return null;
        }
    }
    
    public class InstructionEntry
    {
        private readonly string sourceInst; // The original/disassembled form of instruction
        private readonly List<string> labels; // Yes it's List<string> - the last one is the primary label. Other labels are served as alias.
        private readonly ushort address; // Where it should be.
        private readonly uint sourceLineNumber;
        
        private ushort assembledInst = 0x0000; // Default to no-op to prevent error
        
        // The comments that belong to this instruction; each element
        // represents one line of comment.
        private readonly List<string> comments;

        public InstructionEntry(ushort address, string inst, List<string> labels, List<string> comments, uint sourceLineNumber = 0)
        {
            this.address = address;
            this.sourceInst = inst;
            this.labels = labels;
            this.comments = comments;
            this.sourceLineNumber = sourceLineNumber;
        }

        public ushort GetAddress()
        {
            return address;
        }
        
        public string GetPrimaryLabel()
        {
            return labels[labels.Count - 1];
        }

        public List<string> GetLabels()
        {
            return labels;
        }
    }

    public class ParsedFile
    {
        public readonly ReadOnlyDictionary<ushort, InstructionEntry> Instructions;
        public readonly ReadOnlyDictionary<string, ushort> LabelTable;
        public readonly ReadOnlyDictionary<ushort, List<string>> ReversedLabelTable;

        public ParsedFile(IDictionary<ushort, InstructionEntry> instructions, IDictionary<string, ushort> labelTable, IDictionary<ushort, List<string>> reversedLabelTable)
        {
            Instructions = new ReadOnlyDictionary<ushort, InstructionEntry>(instructions);
            LabelTable = new ReadOnlyDictionary<string, ushort>(labelTable);
            ReversedLabelTable = new ReadOnlyDictionary<ushort, List<string>>(reversedLabelTable);
        }
    }
    
}