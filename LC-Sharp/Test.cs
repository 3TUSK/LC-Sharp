using NUnit.Framework;

namespace LC_Sharp
{
    namespace Test
    {   
        [TestFixture]
        public class AssemblerTest
        {

            [Test]
            public void SimpleAssembleTest()
            {
                var source = new[]
                {
                    "ADD R0, R0, #10",
                    "NOT R1, R1",
                    "AND R2, R0, #0",
                    "OUT",
                    "HALT"
                };
                var assembled = new NeoAssembler(new Parser().Parse(source)).Assemble();
                Assert.Equals(assembled[0], 0x102A);
                Assert.Equals(assembled[1], 0x927F);
                Assert.Equals(assembled[2], 0x5420);
                Assert.Equals(assembled[3], 0xF021);
                Assert.Equals(assembled[4], 0xF025);
            }
        }
        
        [TestFixture]
        public class StateMachineTest
        {
            [Test]
            public void StateMachineTest1()
            {
                var fsm = new LC3();
                Assert.Equals(fsm.control.pc, 0x3000);
                fsm.Execute(0b0001_001_001_1_01111); // ADD R1, R1, #15
                Assert.Equals(fsm.processing.registers[1], 15);
                Assert.Equals(fsm.control.pc, 0x3001);
                Assert.False(fsm.processing.N);
                Assert.False(fsm.processing.Z);
                Assert.True(fsm.processing.P);
                fsm.Execute(0b0011_001_000111101); // ST R1, LABEL_AT_X3040
                Assert.Equals(fsm.memory.Read(0x3040), 0x000F); // 0x000F == 15
            }
        }
    }

}