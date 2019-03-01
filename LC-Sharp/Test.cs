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
                var source = new string[]
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
    }

}