using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;

namespace Nameless.Class_Files
{
    class Validation
    {
        public static double checkZero(double value)
        {
            if (value > 0 && value < 0.001)
            {
                return 0;
            }
            else if (value < 0 && value > 0.001)
            {
                return 0;
            }
            else
            {
                return value;
            }
        }

        //uses long string instead of scientific notation
        public static string ToLongString(double input)
        {
            string str = input.ToString().ToUpper();

            // if string representation was collapsed from scientific notation, just return it:
            if (!str.Contains("E")) return str;

            bool negativeNumber = false;

            if (str[0] == '-')
            {
                str = str.Remove(0, 1);
                negativeNumber = true;
            }

            string sep = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            char decSeparator = sep.ToCharArray()[0];

            string[] exponentParts = str.Split('E');
            string[] doubleParts = exponentParts[0].Split(decSeparator);

            // fix missing double point:
            if (doubleParts.Length == 1) doubleParts = new string[] { exponentParts[0], "0" };

            int exponentValue = int.Parse(exponentParts[1], CultureInfo.InvariantCulture);

            string newNumber = doubleParts[0] + doubleParts[1];

            string result;

            if (exponentValue > 0)
            {
                result =
                    newNumber +
                    GetZeros(exponentValue - doubleParts[1].Length);
            }
            else // negative exponent
            {
                result =
                    "0" +
                    decSeparator +
                    GetZeros(exponentValue + doubleParts[0].Length) +
                    newNumber;

                result = result.TrimEnd('0');
            }

            if (negativeNumber)
                result = "-" + result;

            return result;
        }

        //keeps input from being in scientific notation, and if the value is near zero then it will be set to zero
        public static string GetZeros(int zeroCount)
        {
            if (zeroCount < 0)
            {
                zeroCount = Math.Abs(zeroCount);
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < zeroCount; i++)
            {
                sb.Append("0");
            }

            return sb.ToString();
        }
    }
}
