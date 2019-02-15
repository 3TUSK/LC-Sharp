using System;
using System.Collections.Generic;
using System.Globalization;
using LC_Sharp.AssemblerImpl;

namespace LC_Sharp
{
    public class AssemblerController
    {
        private static readonly Dictionary<string, IInstructionAssembler> _assemblers = new Dictionary<string, IInstructionAssembler>();

        static AssemblerController()
        {
            _assemblers["ADD"] = new AddInstruction();
            _assemblers["AND"] = new AndInstruction();
            
            _assemblers["BR"] = new BranchInstruction("BR", true, true, true);
            _assemblers["BRN"] = new BranchInstruction("BRN", true, false, false);
            _assemblers["BRZ"] = new BranchInstruction("BRZ", false, true, false);
            _assemblers["BRP"] = new BranchInstruction("BRP", false, false, true);
            _assemblers["BRZP"] = new BranchInstruction("BR", false, true, true);
            _assemblers["BRNP"] = new BranchInstruction("BR", true, false, true);
            _assemblers["BRNZ"] = new BranchInstruction("BR", true, true, false);
            _assemblers["BRNZP"] = new BranchInstruction("BR", true, true, true);
            
            _assemblers["JMP"] = null;
            _assemblers["JSR"] = null;
            _assemblers["JSRR"] = null;
            _assemblers["LD"] = null;
            _assemblers["LDI"] = null;
            _assemblers["LDR"] = null;
            _assemblers["LEA"] = null;
            _assemblers["NOT"] = new NotInstruction();
            _assemblers["RET"] = new ZeroArgumentsInstruction("RET", 0xC1C0); // 1100 000 111 000000, i.e. JMP R7
            _assemblers["RTI"] = new ZeroArgumentsInstruction("RTI", 0x8000); // 1000 000000000000
            _assemblers["ST"] = null;
            _assemblers["STI"] = null;
            _assemblers["STR"] = null;
            _assemblers["TRAP"] = new TrapInstruction();
            
            _assemblers["GETC"] = new ZeroArgumentsInstruction("GETC", 0xF020); // TRAP x20
            _assemblers["OUT"] = new ZeroArgumentsInstruction("OUT", 0xF021); // TRAP x21
            _assemblers["PUTS"] = new ZeroArgumentsInstruction("PUTS", 0xF022); // TRAP x22
            _assemblers["IN"] = new ZeroArgumentsInstruction("IN", 0xF023); // TRAP x23
            _assemblers["PUTSP"] = new ZeroArgumentsInstruction("PUTSP", 0xF024); // TRAP x24
            _assemblers["HALT"] = new ZeroArgumentsInstruction("HALT", 0xF025); // TRAP x25
        }

        public static ICollection<string> GetKnownInstructions() => _assemblers.Keys;
        
        public static void registerNewInstruction(string instr, IInstructionAssembler assembler)
        {
            if (_assemblers.ContainsKey(instr))
            {
                throw new Exception("Cannot overwrite existed instruction");
            }
            _assemblers[instr] = assembler;
        }
    }
    
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
        ushort Assemble(string sourceInstruction, ushort offset, ParsedFile environment);

        string Disassemble(ushort instruction); // TODO Guess we need an environment to properly translate offset to labels.
    }

    // An exception that may be thrown from IInstructionAssembler
    public class AssemblerException : Exception
    {
        private readonly string _instrInQuestion;

        public AssemblerException(string instr, string reason = "Unknown Assembler Error") : base(reason)
        {
            _instrInQuestion = instr;
        }

        public string GetMalformedInstruction() => _instrInQuestion;
        
    }
    
    namespace AssemblerImpl
    {
        public static class AssemblerUtil
        {
            public static ushort expectRegister(string token)
            {
                if (token.Length != 2 || token[0] != 'R')
                {
                    throw new Exception($"Not a valid register: {token}");
                }
                switch (token[1]) // Brutal force implementation
                {
                    case '0': return 0x0000;
                    case '1': return 0x0001;
                    case '2': return 0x0002;
                    case '3': return 0x0003;
                    case '4': return 0x0004;
                    case '5': return 0x0005;
                    case '6': return 0x0006;
                    case '7': return 0x0007;
                    default: throw new Exception($"No such a register: {token}");
                }
            }

            public static ushort expectLabel(string label, IDictionary<string, ushort> symbolTable)
            {
                if (symbolTable.ContainsKey(label)) // TODO (3TUSK): Should we add label name max. length restriction?
                {
                    return symbolTable[label];
                }
                throw new Exception($"Not a valid label: {label}");
            }

            public static short expectImm5(string token)
            {
                if (token.Length <= 1 || token[0] != '#')
                {
                    throw new Exception($"Not a valid immediate value: {token}");
                }
                var parsed = short.Parse(token.Substring(1));
                if (parsed >= -16 && parsed <= 15) // uh wait what?
                {
                    return parsed;
                }
                throw new Exception($"Immediate value overflow: {token}");
            }

            public static ushort expectTrapVec8(string token)
            {
                if (token.Length <= 1 || token[0] != 'X')
                {
                    throw new Exception($"Not a valid Trap Vector: {token}");
                }

                var parsed = ushort.Parse(token, NumberStyles.HexNumber);
                if (parsed > 0xFF)
                {
                    throw new Exception($"Trap Vector out of range (max. 0xFF, dec. 255): {token}");
                }

                return parsed;
            }
        }

        /**
         * A simple IInstructionAssembler implementation that directly converts instruction to
         * assemble result. Used by RET, RTI, and all aliased TRAP routine instructions (i.e.
         * GETC, OUT, PUTS, IN, PUTSP, HALT).
         */
        public sealed class ZeroArgumentsInstruction : IInstructionAssembler
        {

            private readonly string _instr;
            private readonly ushort _assembleResult;

            public ZeroArgumentsInstruction(string instr, ushort assembled)
            {
                _instr = instr;
                _assembleResult = assembled;
            }
            
            public ushort Assemble(string sourceInstruction, ushort offset, ParsedFile environment)
            {
                if (sourceInstruction == _instr)
                {
                    return _assembleResult;
                }
                throw new AssemblerException(sourceInstruction, "Instruction mismatch!");
            }

            public string Disassemble(ushort instruction)
            {
                if (instruction == _assembleResult)
                {
                    return _instr;
                }
                throw new AssemblerException(instruction.ToString(), "Instruction mismatch!");
            }
        }

        public abstract class ArithematicInstruction : IInstructionAssembler
        {
            protected readonly string _instr;
            private readonly ushort _argCount;

            protected ArithematicInstruction(string instr, ushort argCount)
            {
                _instr = instr;
                _argCount = argCount;
            }
            
            public ushort Assemble(string sourceInstruction, ushort offset, ParsedFile environment)
            {
                var tokens = sourceInstruction.Split(' ', ',');
                if (tokens.Length != _argCount)
                {
                    throw new AssemblerException(sourceInstruction, "Operands mismatch");
                }

                if (tokens[0] != _instr)
                {
                    throw new AssemblerException(sourceInstruction, "Instruction mismatch");
                }

                var destination = AssemblerUtil.expectRegister(tokens[1]);
                var operand1 = AssemblerUtil.expectRegister(tokens[2]);
                return ContinueAssemble(offset, environment, destination, operand1, tokens);
            }

            protected abstract ushort ContinueAssemble(ushort offset, ParsedFile env, ushort destReg, ushort op1, string[] tokens);

            public string Disassemble(ushort instruction)
            {
                throw new NotImplementedException();
            }
        }

        public sealed class AddInstruction : ArithematicInstruction
        {
            public AddInstruction() : base("ADD", 4)
            {
            }

            protected override ushort ContinueAssemble(ushort offset, ParsedFile env, ushort destReg, ushort op1, string[] tokens)
            {
                ushort op2;
                var isImm5 = false;
                try
                {
                    var imm5 = AssemblerUtil.expectImm5(tokens[3]);
                    isImm5 = true;
                    op2 = (ushort) (0 + (imm5 < 0 ? 0x10 : 0x00) + (imm5 & 0x000F));
                }
                catch (AssemblerException e)
                {
                    op2 = AssemblerUtil.expectRegister(tokens[3]);
                }
                // 0001 DR SR 0 00 SR2 If 2nd operand is register
                // 0001 DR SR 1 Imm5 If 2nd operand is immediate value
                return (ushort) (0x1000 + (destReg << 9) + (op1 << 6) + (isImm5 ? 0x0020 : 0x0000) + op2);
            }
        }
        
        public sealed class AndInstruction : ArithematicInstruction
        {
            public AndInstruction() : base("AND", 4)
            {
            }

            protected override ushort ContinueAssemble(ushort offset, ParsedFile env, ushort destReg, ushort op1, string[] tokens)
            {
                ushort op2;
                var isImm5 = false;
                try
                {
                    var imm5 = AssemblerUtil.expectImm5(tokens[3]);
                    isImm5 = true;
                    op2 = (ushort) (0 + (imm5 < 0 ? 0x10 : 0x00) + (imm5 & 0x000F));
                }
                catch (AssemblerException e)
                {
                    op2 = AssemblerUtil.expectRegister(tokens[3]);
                }
                // 0101 DR SR 0 00 SR2 If 2nd operand is register
                // 0101 DR SR 1 Imm5 If 2nd operand is immediate value
                return (ushort) (0x5000 + (destReg << 9) + (op1 << 6) + (isImm5 ? 0x0020 : 0x0000) + op2);
            }
        }
        
        public sealed class NotInstruction : ArithematicInstruction
        {
            public NotInstruction() : base("NOT", 3)
            {
            }

            protected override ushort ContinueAssemble(ushort offset, ParsedFile env, ushort destReg, ushort op1, string[] tokens)
                => (ushort) (0x9000 + (destReg << 9) + (op1 << 6) + 0x003F); // 1001 DR SR 1 11111
        }

        public sealed class TrapInstruction : IInstructionAssembler
        {
            public ushort Assemble(string sourceInstruction, ushort offset, ParsedFile environment)
            {
                var tokens = sourceInstruction.Split(' ');
                if (tokens.Length != 2 || tokens[0] != "TRAP")
                {
                    throw new AssemblerException("Malformed TRAP instruction!");
                }

                return (ushort) (AssemblerUtil.expectTrapVec8(tokens[1]) | 0xF000); // 1111 0000 TRAPVEC8
            }

            public string Disassemble(ushort instruction)
            {
                throw new NotImplementedException();
            }
        }

        public sealed class BranchInstruction : IInstructionAssembler
        {
            private readonly string _instr;
            private readonly ushort _controlFlag = 0x0000;

            public BranchInstruction(string instr, bool n, bool z, bool p)
            {
                _instr = instr;
                if (n)
                {
                    _controlFlag |= 0x0800;
                }
                if (z)
                {
                    _controlFlag |= 0x0400;
                }
                if (p)
                {
                    _controlFlag |= 0x0200;
                }
            }
            
            public ushort Assemble(string sourceInstruction, ushort offset, ParsedFile environment)
            {
                var tokens = sourceInstruction.Split(' ');
                if (tokens.Length != 2)
                {
                    throw new AssemblerException(sourceInstruction, "Wrong number of tokens");
                }
                if (tokens[0] != _instr)
                {
                    throw new AssemblerException(sourceInstruction, "Instruction mismatch");
                }
                return (ushort) (_controlFlag | AssemblerUtil.expectLabel(tokens[1], environment.LabelTable));
            }

            public string Disassemble(ushort instruction)
            {
                throw new NotImplementedException();
            }
        }
    }
}