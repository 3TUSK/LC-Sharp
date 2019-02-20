using System.Globalization;
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
                Assert.Equals(assembled[0], ushort.Parse("102A", NumberStyles.HexNumber));
                Assert.Equals(assembled[1], ushort.Parse("927F", NumberStyles.HexNumber));
                Assert.Equals(assembled[2], ushort.Parse("5420", NumberStyles.HexNumber));
                Assert.Equals(assembled[3], ushort.Parse("F021", NumberStyles.HexNumber));
                Assert.Equals(assembled[4], ushort.Parse("F025", NumberStyles.HexNumber));
            }
        }    
    }

}