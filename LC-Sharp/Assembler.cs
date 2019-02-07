using System;
using System.Collections.Generic;

namespace LC_Sharp
{
    public class NeoAssembler
    {
        private ParsedFile _parsedFile;

        public NeoAssembler(ParsedFile parsedFile)
        {
            _parsedFile = parsedFile;
        }

        public Dictionary<ushort, ushort> Assemble()
        {
            // TODO Assemble
            return new Dictionary<ushort, ushort>();
        }
    }

    public interface IInstructionAssembler
    {
        ushort assemble(string sourceInstruction, ushort offset, ParsedFile environment);

        string disassemble(ushort instruction); // TODO Guess we need an environment to properly translate offset to labels.
    }

    // An exception that may be thrown from IInstructionAssembler.assemble
    public class AssembleException : Exception
    {
        private readonly string _instrInQuestion;

        public AssembleException(string instr)
        {
            _instrInQuestion = instr;
        }

        public string GetMalformedInstruction()
        {
            return _instrInQuestion;
        }
    }
}