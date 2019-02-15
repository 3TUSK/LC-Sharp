using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LC_Sharp
{
    public class ParserController
    {
        private static readonly Dictionary<string, IAssemblerInstruction> _pseudoOps = new Dictionary<string, IAssemblerInstruction>();

        static ParserController()
        {
            _pseudoOps["ORIG"] = WrappedAssemblerInstruction.of((controller, args) => controller.SetPosition(0x3000)); // TODO
            _pseudoOps["FILL"] = WrappedAssemblerInstruction.of((controller, args) => {}); // TODO
            _pseudoOps["BLKW"] = WrappedAssemblerInstruction.of((controller, args) => {}); // TODO
            _pseudoOps["STRINGZ"] = WrappedAssemblerInstruction.of((controller, args) => {}); // TODO
            _pseudoOps["END"] = WrappedAssemblerInstruction.of((controller, args) => controller.Terminate());
        }
        
        private static readonly ICollection<string> instr = AssemblerController.GetKnownInstructions();

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

        public void BeginLine() => sourceLine++;
        
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

        public void Terminate() => isFinished = true;

        public void SetPosition(ushort newOffset)
        {
            if (newOffset < position)
            {
                throw new ParserException("Origin cannot overlap - most likely you put too much instructions or data.");
            }
            position = newOffset;
        }

        public ushort GetPosition() => position;

        public bool isTerminated() => isFinished;

        public ParsedFile Assemble()
        {
            var instructions = this.instructions.ToDictionary(i => i.GetAddress(), i => i);
            var reversedLabelLookup = this.instructions.ToDictionary(i => i.GetAddress(), i => i.GetLabels());
            var labelLookup = new Dictionary<string, ushort>();
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

    public class ParserException : Exception
    {
        public ParserException(string message) : base(message) {}
    }

    public interface IAssemblerInstruction
    {
        void Process(ParserController controller, params string[] arguments);
    }

    public class WrappedAssemblerInstruction : IAssemblerInstruction
    {
        public static WrappedAssemblerInstruction of(Action<ParserController, string[]> action)
            => new WrappedAssemblerInstruction(action);
        
        private readonly Action<ParserController, string[]> _impl;

        public WrappedAssemblerInstruction(Action<ParserController, string[]> action)
        {
            _impl = action;
        }
        
        public void Process(ParserController controller, params string[] arguments) => _impl(controller, arguments);
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

        public ushort GetAddress() => address;
        
        public string GetPrimaryLabel() => labels[labels.Count - 1];

        public List<string> GetLabels() => labels;
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