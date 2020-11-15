using System.Linq;

namespace LC_Sharp {
    //When the MSB is zero, the other bits represent the absolute value
    //When the MSB is one, the other bits represent the value to add to -2^n
    //If the unsigned value is greater than the max signed value (the MSB must be one), that means we subtract the max unsigned value to get the signed value 
    public static class Help {

		//Verifies that an offset fits within a given size
		public static bool WithinRange(this short offset, int size) {
			ushort mask = (ushort)(0xFFFF >> (16 - size));    //All ones
			short min = (short)-(1 << (size - 1));           //Highest negative number, same as the MSB
															 //short max = (short)(0xEFFF >> (16 - size));
			short max = (short)(-min - 1);                //Highest positive number, All ones except MSB
			if (offset < min || offset > max) {
				return false;
			} else {
				//Truncate the result to the given size
				return true;
			}
		}
		//n: the original size of the number to be extended
		public static short signExtend(this short s, int n = 16) {
            short msbMask = (short) (0b1 << (n - 1));
            short negativeMask = (short) (0xFFFF << n);
            if((s & msbMask) != 0) {
                return (short) (s | negativeMask);
            } else {
                return s;
            }
        }
        public static ushort signExtend(this ushort s, int n = 16) {
            ushort msbMask = (ushort)(1 << (n - 1));
            ushort negativeMask = (ushort) (0xFF << n);
            if ((s & msbMask) != 0) {
                return (ushort)(s | negativeMask);
            } else {
                return s;
            }
        }
		public static string ToHexShort(this int s) {
			return $"0x{s.ToString("X").PadLeft(4, '0')}";
		}

        public static string PadSegments(this string s) => string.Join(".", s.Split(' ').Select(PadFour));

        public static string PadFour(this string s) => s.PadRight(4 + (s.Length / 4) * 4);

        public static string PadInterval(this string s, int interval) => s.PadRight(interval + (s.Length / interval) * interval);

        public static string PadSurround(this string s, int length, char c) => s.PadLeft(s.Length + (length - s.Length) / 2, c).PadRight(length, c);
		public static string ToRegisterString(this ushort r) => $"0x{r.ToString("X").PadLeft(4, '0')} #{r.ToString()}";
		public static string ToRegisterString(this short r) => $"0x{r.ToString("X").PadLeft(4, '0')} #{r.ToString()}";
		public static string ToHexString(this ushort r) => $"0x{r.ToString("X").PadLeft(4, '0')}";
		public static string ToHexString(this short r) => $"0x{r.ToString("X").PadLeft(4, '0')}";
        /*
        //Functions to be used with ushorts containing smaller-sized numbers
        //Converts ushort to ushort with equivalent bit pattern
        public static ushort ToSigned(this ushort unsigned, int n = 16) {
            ushort maxUnsigned = (ushort) Math.Pow(2, n);   //0b1111111111111111
            ushort maxSigned = (ushort) Math.Pow(2, n - 1); //0b0111111111111111
            Console.WriteLine("Max Unsigned: " + maxUnsigned);
            if(unsigned >= maxSigned) {
                return (short) (unsigned - maxUnsigned);
            } else {
                return (short) unsigned;
            }
        }
        //Converts ushort to ushort with equivalent bit pattern
        public static ushort ToUnsigned(this ushort signed, int n = 16) {
            ushort maxUnsigned = (ushort)Math.Pow(2, n); //0b1111111111111111
            if(signed < 0) {
                //256 + (-1) = 255, 0b1_0000_0000_0000_0000 - 0b0000_0000_0000_0001 = 0b1111_1111_1111_1111
                //256 + (-2) = 254, 0b1_0000_0000_0000_0000 - 0b0000_0000_0000_0010 = 0b1111_1111_1111_1110
                return (ushort)(maxUnsigned + signed);
            } else {
                return (ushort)signed;
            }
        }
        */
    }
    
}