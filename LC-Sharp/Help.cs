namespace LC_Sharp {
    //When the MSB is zero, the other bits represent the absolute value
    //When the MSB is one, the other bits represent the value to add to -2^n
    //If the unsigned value is greater than the max signed value (the MSB must be one), that means we subtract the max unsigned value to get the signed value 
    public static class Help {
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
        /*
        //Functions to be used with ushorts containing smaller-sized numbers
        //Converts ushort to short with equivalent bit pattern
        public static short ToSigned(this ushort unsigned, int n = 16) {
            ushort maxUnsigned = (ushort) Math.Pow(2, n);   //0b1111111111111111
            ushort maxSigned = (ushort) Math.Pow(2, n - 1); //0b0111111111111111
            Console.WriteLine("Max Unsigned: " + maxUnsigned);
            if(unsigned >= maxSigned) {
                return (short) (unsigned - maxUnsigned);
            } else {
                return (short) unsigned;
            }
        }
        //Converts short to ushort with equivalent bit pattern
        public static ushort ToUnsigned(this short signed, int n = 16) {
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