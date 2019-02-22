using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LC_Sharp.AssemblerImpl;

namespace LC_Sharp
{
    public static class AssemblerController
    {
        private static readonly Dictionary<string, IInstructionAssembler> Assemblers = new Dictionary<string, IInstructionAssembler>();

        static AssemblerController()
        {
            Assemblers["ADD"] = new AddInstruction();
            Assemblers["AND"] = new AndInstruction();
            
            Assemblers["BR"] = new BranchInstruction("BR", true, true, true); // Alias of BRNZP
            Assemblers["BRN"] = new BranchInstruction("BRN", true, false, false);
            Assemblers["BRZ"] = new BranchInstruction("BRZ", false, true, false);
            Assemblers["BRP"] = new BranchInstruction("BRP", false, false, true);
            Assemblers["BRZP"] = new BranchInstruction("BRZP", false, true, true);
            Assemblers["BRNP"] = new BranchInstruction("BRNP", true, false, true);
            Assemblers["BRNZ"] = new BranchInstruction("BRNZ", true, true, false);
            Assemblers["BRNZP"] = new BranchInstruction("BRNZP", true, true, true);
            
            Assemblers["JMP"] = new RegisterBasedPCAccessInstruction("JMP", 0b1100);
            Assemblers["JSR"] = new JSRInstruction();
            Assemblers["JSRR"] = new RegisterBasedPCAccessInstruction("JSRR", 0b0100);
            Assemblers["LD"] = new LabelBasedMemoryAccessInstruction("LD", 0b0010);
            Assemblers["LDI"] = new LabelBasedMemoryAccessInstruction("LDI", 0b1010);
            Assemblers["LDR"] = new OffsetBasedMemoryAccessInstruction("LDR", 0b0110);
            Assemblers["LEA"] = new LabelBasedMemoryAccessInstruction("LEA", 0b1110);
            Assemblers["NOT"] = new NotInstruction();
            Assemblers["RET"] = new ZeroArgumentsInstruction("RET", 0xC1C0); // 1100 000 111 000000, i.e. JMP R7
            Assemblers["RTI"] = new ZeroArgumentsInstruction("RTI", 0x8000); // 1000 000000000000
            Assemblers["ST"] = new LabelBasedMemoryAccessInstruction("ST", 0b0011);
            Assemblers["STI"] = new LabelBasedMemoryAccessInstruction("STI", 0b1011);
            Assemblers["STR"] = new OffsetBasedMemoryAccessInstruction("STR", 0b0111);
            Assemblers["TRAP"] = new TrapInstruction();
            
            Assemblers["GETC"] = new ZeroArgumentsInstruction("GETC", 0xF020); // TRAP x20
            Assemblers["OUT"] = new ZeroArgumentsInstruction("OUT", 0xF021); // TRAP x21
            Assemblers["PUTS"] = new ZeroArgumentsInstruction("PUTS", 0xF022); // TRAP x22
            Assemblers["IN"] = new ZeroArgumentsInstruction("IN", 0xF023); // TRAP x23
            Assemblers["PUTSP"] = new ZeroArgumentsInstruction("PUTSP", 0xF024); // TRAP x24
            Assemblers["HALT"] = new ZeroArgumentsInstruction("HALT", 0xF025); // TRAP x25
        }

        public static ICollection<string> GetKnownInstructions() => Assemblers.Keys;
        
        public static void RegisterNewInstruction(string instr, IInstructionAssembler assembler)
        {
            if (Assemblers.ContainsKey(instr))
            {
                throw new Exception("Cannot overwrite existed instruction");
            }
            Assemblers[instr] = assembler;
        }

        public static IInstructionAssembler Find(string instName)
            => Assemblers.TryGetValue(instName, out var instruction) ? instruction : null;        
    }
    
    public class NeoAssembler
    {
        private readonly ParsedFile _parsedFile;

        public NeoAssembler(ParsedFile parsedFile) => _parsedFile = parsedFile;

        public Dictionary<ushort, ushort> Assemble()
        {
            var assembleResult = new Dictionary<ushort, ushort>();
            foreach (var instr in _parsedFile.Instructions)
            {
                var singleInstructionAssembler = AssemblerController.Find(instr.Value.GetInstruction().Split(' ').First());
                if (singleInstructionAssembler != null)
                {
                    assembleResult[instr.Key] = singleInstructionAssembler.Assemble(instr.Value.GetInstruction(), instr.Key, _parsedFile);
                }
                else
                {
                    throw new Exception($"Don't know how to assemble {instr.Value.GetInstruction()}");
                }
            }
            return assembleResult;
        }
    }

    public interface IInstructionAssembler
    {
        ushort Assemble(string sourceInstruction, ushort offset, ParsedFile environment);

        string Disassemble(ushort instruction); // TODO Guess we need an environment to properly translate offset to labels.
    }
    
    public class StandardInstructionAssembler : IInstructionAssembler
    {
        // TODO (3TUSK): Get rid of that strange instruction assembler hierarchy by using standardized token system (see below StandardOperands)
        ushort Assemble(string sourceInstruction, ushort offset, ParsedFile environment)
        {
            throw new Exception("Not implemented yet");
        }
        string Disassemble(ushort instruction)
        {
            throw new Exception("Not implemented yet");
        }
    }

    public class StandardOperands
    {
        //public static readonly StandardOperands REGISTER
        //public static readonly StandardOperands LABEL // TODO (3TUSK): there are PCOffset9 and PCOffset11
        //public static readonly StandardOperands IMMEDIATE_5 // TODO (3TUSK): there is also offset 6
        //public static readonly StandardOperands TRAP_VECTOR
    }

    // An exception that may be thrown from IInstructionAssembler
    public class AssemblerException : Exception
    {
        private readonly string _instrInQuestion;

        public AssemblerException(string instr, string reason = "Unknown Assembler Error") : base(reason) 
            =>_instrInQuestion = instr;

        public string GetMalformedInstruction() => _instrInQuestion;
        
    }
    
    namespace AssemblerImpl
    {
        public static class AssemblerUtil
        {
            public static ushort ExpectRegister(string token)
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

            public static ushort ExpectLabel(string label, IDictionary<string, ushort> symbolTable)
            {
                if (symbolTable.ContainsKey(label)) // TODO (3TUSK): Should we add label name max. length restriction?
                {
                    return symbolTable[label];
                }
                throw new Exception($"Not a valid label: {label}");
            }

            public static short ExpectImm(string token, ushort len)
            {
                if (token.Length <= 1 || token[0] != '#')
                {
                    throw new Exception($"Not a valid immediate value: {token}");
                }
                var parsed = short.Parse(token.Substring(1));
                if (parsed >= -(1 << len) && parsed <= (1 << len) - 1)
                {
                    return parsed;
                }
                throw new Exception($"Immediate value overflow: {token}");
            }

            public static ushort ExpectTrapVec8(string token)
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
                if (sourceInstruction.Trim() == _instr)
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
            private readonly string _instr;
            private readonly ushort _argCount;

            protected ArithematicInstruction(string instr, ushort argCount)
            {
                _instr = instr;
                _argCount = argCount;
            }
            
            public ushort Assemble(string sourceInstruction, ushort offset, ParsedFile environment)
            {
                var tokens = sourceInstruction.Split(' ', ',').Where(s => s.Length > 0).ToArray();
                if (tokens.Length != _argCount)
                {
                    throw new AssemblerException(sourceInstruction, "Operands mismatch");
                }

                if (tokens[0] != _instr)
                {
                    throw new AssemblerException(sourceInstruction, "Instruction mismatch");
                }

                var destination = AssemblerUtil.ExpectRegister(tokens[1]);
                var operand1 = AssemblerUtil.ExpectRegister(tokens[2]);
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
                    var imm5 = AssemblerUtil.ExpectImm(tokens[3], 5);
                    isImm5 = true;
                    op2 = (ushort) (0 + (imm5 < 0 ? 0x10 : 0x00) + (imm5 & 0x000F));
                }
                catch (AssemblerException ignored)
                {
                    op2 = AssemblerUtil.ExpectRegister(tokens[3]);
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
                    var imm5 = AssemblerUtil.ExpectImm(tokens[3], 5);
                    isImm5 = true;
                    op2 = (ushort) (0 + (imm5 < 0 ? 0x10 : 0x00) + (imm5 & 0x000F));
                }
                catch (AssemblerException ignored)
                {
                    op2 = AssemblerUtil.ExpectRegister(tokens[3]);
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

                return (ushort) (AssemblerUtil.ExpectTrapVec8(tokens[1]) | 0xF000); // 1111 0000 TRAPVEC8
            }

            public string Disassemble(ushort instruction)
            {
                throw new NotImplementedException();
            }
        }

        public abstract class PCAccessInstruction : IInstructionAssembler
        {
            private readonly string _instr;

            protected PCAccessInstruction(string instr) => _instr = instr;
            
            public ushort Assemble(string sourceInstruction, ushort offset, ParsedFile environment)
            {
                var tokens = sourceInstruction.Split(' ');
                if (tokens[0] != _instr)
                {
                    throw new AssemblerException(sourceInstruction, "Instruction mismatch");
                }

                return ContinueAssemble(offset, environment, tokens);
            }

            protected abstract ushort ContinueAssemble(ushort offset, ParsedFile env, string[] tokens);

            public string Disassemble(ushort instruction)
            {
                throw new NotImplementedException();
            }
        }

        public abstract class LabelBasedPCAccessInstruction : PCAccessInstruction
        {
            private readonly ushort _offsetLimit;

            protected LabelBasedPCAccessInstruction(string instr, ushort offset) : base(instr) => _offsetLimit = offset;

            protected sealed override ushort ContinueAssemble(ushort offset, ParsedFile env, string[] tokens)
            {
                var labelTarget = AssemblerUtil.ExpectLabel(tokens[1], env.LabelTable);
                if ((offset + 1 - labelTarget) >> _offsetLimit > 0)
                {
                    throw new AssemblerException(tokens[1], "Unreachable label");
                }

                return FinalAssemble(labelTarget);
            }

            protected abstract ushort FinalAssemble(ushort labelOffset);
        }

        public sealed class RegisterBasedPCAccessInstruction : PCAccessInstruction
        {
            private readonly ushort _opcode;
            
            public RegisterBasedPCAccessInstruction(string instr, ushort opcode) : base(instr) => _opcode = opcode;

            protected override ushort ContinueAssemble(ushort offset, ParsedFile env, string[] tokens)
                => (ushort) ((_opcode << 12) | (AssemblerUtil.ExpectRegister(tokens[2]) << 6) & 0b1111_000_111_000000);
        }

        public sealed class BranchInstruction : LabelBasedPCAccessInstruction
        {
            private readonly ushort _controlFlag;

            public BranchInstruction(string instr, bool n, bool z, bool p) : base(instr, 9)
            {
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

            protected override ushort FinalAssemble(ushort labelOffset)
                => (ushort) (_controlFlag | labelOffset & 0x0FFF); // BR has opcode of 0000, so we use 0x0FFF to normalize it
        }

        public sealed class JSRInstruction : LabelBasedPCAccessInstruction
        {
            public JSRInstruction() : base("JSR", 11)
            {
            }

            protected override ushort FinalAssemble(ushort labelOffset) 
                => (ushort) (0b0100_1000_0000_0000 | (labelOffset & 0b0000_0_11111111111));
        }

        public abstract class MemoryAccessInstruction : IInstructionAssembler
        {
            private readonly string _instr;
            protected readonly ushort _opcode;

            protected MemoryAccessInstruction(string instr, ushort opcode)
            {
                _instr = instr;
                _opcode = opcode;
            }
            
            public ushort Assemble(string sourceInstruction, ushort offset, ParsedFile environment)
            {
                var tokens = sourceInstruction.Split(' ', ',').Where(s => s.Length > 0).ToArray();
                if (_instr != tokens[0])
                {
                    throw new AssemblerException(sourceInstruction, "Instruction mismatch");
                }

                var reg1 = AssemblerUtil.ExpectRegister(tokens[1]);
                return ContinueAssemble(offset, environment, reg1, tokens);
            }

            protected abstract ushort ContinueAssemble(ushort offset, ParsedFile env, ushort reg1, string[] tokens);

            public string Disassemble(ushort instruction)
            {
                throw new NotImplementedException();
            }
        }

        public sealed class LabelBasedMemoryAccessInstruction : MemoryAccessInstruction
        {
            public LabelBasedMemoryAccessInstruction(string instr, ushort opcode) : base(instr, opcode)
            {
            }

            protected override ushort ContinueAssemble(ushort offset, ParsedFile env, ushort reg1, string[] tokens)
            {
                var labelTarget = AssemblerUtil.ExpectLabel(tokens[2], env.LabelTable);
                var offset9 = offset + 1 - labelTarget;
                if (offset9 > 511)
                {
                    throw new AssemblerException(tokens[2], "Unreachable label");
                }
                return (ushort) ((_opcode << 12) | (reg1 << 9) | (offset9 & 0x01FF));
            }
        }

        public sealed class OffsetBasedMemoryAccessInstruction : MemoryAccessInstruction
        {
            public OffsetBasedMemoryAccessInstruction(string instr, ushort opcode) : base(instr, opcode)
            {
            }

            protected override ushort ContinueAssemble(ushort offset, ParsedFile env, ushort reg1, string[] tokens)
            {
                var reg2 = AssemblerUtil.ExpectRegister(tokens[2]);
                var offset6 = AssemblerUtil.ExpectImm(tokens[3], 6);
                return (ushort) ((_opcode << 12) | (reg1 << 9) | (reg2 << 6) | (offset6 & 0x003F));
            }
        }
    }
}
