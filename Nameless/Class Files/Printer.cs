using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;

namespace Nameless.Class_Files
{
    public class ZProbe
    {
        public double X;
        public double Y;
        public double Zprobe;
    }
    public static class Heights
    {
        //store every set of heights
        public static double Center = 0;
        public static double zMaxLength;
        public static double X;
        public static double XOpp;
        public static double Y;
        public static double YOpp;
        public static double Z;
        public static double ZOpp;
        public static double teX;
        public static double teXOpp;
        public static double teY;
        public static double teYOpp;
        public static double teZ;
        public static double teZOpp;
        public static bool firstHeights = true;

        /*
        public Heights(double _center, double _X, double _XOpp, double _Y, double _YOpp, double _Z, double _ZOpp)
        {
            center = _center;
            X = _X;
            XOpp = _XOpp;
            Y = _Y;
            YOpp = _YOpp;
            Z = _Z;
            ZOpp = _ZOpp;

            if (firstHeights == true)
            {
                teX = _X;
                teXOpp = _XOpp;
                teY = _Y;
                teYOpp = _YOpp;
                teZ = _Z;
                teZOpp = _ZOpp;
                firstHeights = false;
            }
        }
        */
    }

    public static class HeightFunctions
    {
        private static int position = 0;
        public static bool heightsSet = true; // true если процесс измерения всех высот завершен
        public static bool checkHeightsOnly = false; // нужно ли замерять высоту
        public static bool getzMaxLength = false; // true если высотра была измерена
        public static bool isProgress = false; // true пока идет процесс калибровки или измерений
        public static List<PointError> list;// = new List<PointError>(); // список координат с измерениями Z

        // определяемы высоты по координатам относительно высоты центра
        public static void setHeights (ZProbe ZProbe)
        {
            list.Add(new PointError((double)ZProbe.X, (double)ZProbe.Y, -1 * (double)ZProbe.Zprobe));


            double zMaxLength = EEPROM.zMaxLength;
            double probingHeight = UserVariables.probingHeight + EEPROM.zProbeHeight;

            if (ZProbe.X == 0 && ZProbe.Y == 0)
                Heights.zMaxLength = zMaxLength - (probingHeight - ZProbe.Zprobe); // высота до сопла

            double deltaZProbe = (probingHeight - ZProbe.Zprobe) - (zMaxLength - Heights.zMaxLength); // расчет высоты относительно центра

// !!! Тут нужно сделать код замера высоты по Эшеру
            if (ZProbe.X < 0 && ZProbe.Y < 0)
                Heights.X = deltaZProbe;
            if (ZProbe.X > 0 && ZProbe.Y > 0)
                Heights.XOpp = deltaZProbe;
            if (ZProbe.X > 0 && ZProbe.Y < 0)
                Heights.Y = deltaZProbe;
            if (ZProbe.X < 0 && ZProbe.Y > 0)
                Heights.YOpp = deltaZProbe;
            if (ZProbe.X == 0 && ZProbe.Y > 0)
                Heights.Z = deltaZProbe;
            if (ZProbe.X == 0 && ZProbe.Y < 0)
                Heights.ZOpp = deltaZProbe;

            position++;
            if (Calibration.calibrationState &&  UserVariables.typeCalibration == "Escher") // если тип калибровки по Эшеру
            {
                if (position >= GCode.lines.Count())
                {
                    heightsSet = true; position = 0;
                }
            }
            else
            {
                if (position >= 7)
                {
                    heightsSet = true; position = 0;
                }
            }
            
        }
        public static ZProbe parseZProbe(string message)
        {
            if (message.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0] != "Z-probe") return null; // выходим, если данные не EPR

            ZProbe zprobe = new ZProbe();
            string[] parse = message.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(string value in parse)
            {
                string[] parseValue = value.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parseValue[0] == "Z-probe") zprobe.Zprobe = double.Parse(parseValue[1], CultureInfo.InvariantCulture);
                else if (parseValue[0] == "X") zprobe.X = double.Parse(parseValue[1], CultureInfo.InvariantCulture);
                else if (parseValue[0] == "Y") zprobe.Y = double.Parse(parseValue[1], CultureInfo.InvariantCulture);
            }
            return zprobe;
        }
        public static void printHeights()
        {
            UserInterface.logConsole("Center:" + Heights.zMaxLength + " X:" + Heights.X + " XOpp:" + Heights.XOpp + " Y:" + Heights.Y + " YOpp:" + Heights.YOpp + " Z:" + Heights.Z + " ZOpp:" + Heights.ZOpp);
        }

        /// <summary>
        /// Сбрасывает все переменные класса по-умолчанию
        /// </summary>
        public static void Reset()
        {
            position = 0;
            heightsSet = true;
            checkHeightsOnly = false;
            getzMaxLength = false;
            isProgress = false;
        }
    }

   
}
