using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC_Sharp {
	class LC3 {
		short bus, mar, mdr, pc, ir;

		static void Main(string[] args) {
			/*
			short s = ~0b0000000000000001 + 1;
			Console.WriteLine(s);
			Console.WriteLine(s.AsBits());
			*/
			short s = -1 - 1;
			Console.WriteLine(s);
			Console.WriteLine(s.AsBits());

			Console.WriteLine(s.GetBits(15, 12) == ~0b0001 + 1);
			Console.ReadLine();
		}

		public void Run() {

		}
	}

	public static class Word {
		/*
		public static bool[] AsBits(this short s) {
			bool[] value = new bool[16];
			for (int i = 0; i < 16; i++) {
				value[i] = s % 2 == 1;
				s /= 2;
			}
			return value;
		}
		public static short GetBits(this bool[] value, int start, int end) {
			//Index decreases from left to right, starting at 15
			short result = 0;
			for (int i = start; i >= end; i--) {
				if (value[i])
					result++;
				result *= 2;
			}
			return result;
		}
		public static short GetBits(this short s, int start, int end) {
			return s.AsBits().GetBits(start, end);
		}
		*/
		public static string AsBits(this short s) {
			string result = "";
			if(s < 0) {
				result = "1";
				s = (short)(~s);
				for (int i = 14; i > -1; i--) {
					short power = (short)Math.Pow(2, i);
					if (s >= power) {
						s -= power;
						result += "0";
					} else {
						result += "1";
					}
				}
			} else {
				result = "0";
				for (int i = 14; i > -1; i--) {
					short power = (short)Math.Pow(2, i);
					if (s >= power) {
						s -= power;
						result += "1";
					} else {
						result += "0";
					}
				}
			}
			return result;
		}
		public static short GetBits(this short s, int start, int end) {
			//We assume that end is lower than start
			//Delete the bits before the start
			short rightShift = (short) (15 - start);
			s = (short)((s << rightShift) >> rightShift);
			//The end becomes the MSB
			s = (short)(s >> end);
			return s;
		}
	}
}
