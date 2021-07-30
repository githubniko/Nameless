using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;

namespace Nameless.Class_Files
{
    static class GCode
    {
        public static int currentPosition = 0;
        public static int iteration = 0;
        public static bool checkHeights = false;
        public static bool wasZProbeHeightSet = false; // Флаг указывает был ли замер высоты рабочей области
        public static bool isHeuristicComplete = false;
        public static IEnumerable<string> lines;

        public static void sendToPosition(double X, double Y, double Z)
        {
            if (Connection._serialPort.IsOpen)
            {
                Connection.WriteLine("G1 Z" + Z.ToString(CultureInfo.InvariantCulture) + " X" + X.ToString() + " Y" + Y.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                UserInterface.logConsole("Not Connected");
            }
        }
        /// <summary>
        /// Отправляет команду G28 (возврат домой)
        /// </summary>
        public static void homeAxes()
        {
            if (Connection._serialPort.IsOpen)
            {
                Connection.WriteLine("G28");
            }
            else
            {
                UserInterface.logConsole("Not Connected");
            }
        }
        
        private static void probe()
        {
            if (Connection._serialPort.IsOpen)
            {
                Connection.WriteLine("G30 P0");
            }
            else
            {
                UserInterface.logConsole("Not Connected");
            }
        }
        public static void emergencyReset()
        {
            if (Connection._serialPort.IsOpen)
            {
                Connection.WriteLine("M112");
            }
            else
            {
                UserInterface.logConsole("Not Connected");
            }
        }
        public static void sendReadEEPROMCommand()
        {
            if (Connection._serialPort.IsOpen)
            {
                Connection.WriteLine("M205");
            }
            else
            {
                UserInterface.logConsole("Not Connected");
            }
        }

        public static void sendEEPROMVariable(int type, int position, double value)
        {
            if (Connection._serialPort.IsOpen)
            {
                if (type == 1)
                {
                    Connection.WriteLine("M206 T1 P" + position + " S" + value.ToString("F3", CultureInfo.InvariantCulture));
                }
                else if (type == 3)
                {
                    Connection.WriteLine("M206 T3 P" + position + " X" + value.ToString("F3", CultureInfo.InvariantCulture));
                }
                else
                {
                    UserInterface.logConsole("Invalid EEPROM Variable.");
                }
            }
            else
            {
                UserInterface.logConsole("Not Connected");
            }
        }

        public static void positionFlowHeight()
        {
            //if (EEPROM.zProbeHeight < 0) EEPROM.zProbeHeight = 0;
            EEPROM.zProbeHeight = 0; // лучше пускай устанавливают 0 отдельно, чем бится
            HeightFunctions.checkHeightsOnly = true;
            homeAxes();
            probe();
        }
        struct CirclePoint
        {
            public double x;
            public double y;
            public CirclePoint(double x, double y)
            {
                this.x = x;
                this.y = y;
            }
        }
        public static void positionFlow()
        {
            double probingHeight = UserVariables.probingHeight - EEPROM.zProbeHeight;
            double plateDiameter = UserVariables.plateDiameter;
            double valueZ = 0.482 * plateDiameter;
            double valueXYLarge = 0.417 * plateDiameter;
            double valueXYSmall = 0.241 * plateDiameter;
            int speed = 60;

            HeightFunctions.list = new List<PointError>(); // очищаем список точек


            homeAxes();
            //Connection.WriteLine("G0 F" + UserVariables.xySpeed * speed);//converts mm/s to mm/min

            if (Calibration.calibrationState && UserVariables.typeCalibration == "Escher") // если тип калибровки по Эшеру
            {
                double maxRadius = plateDiameter / 2;
                uint turnCount = 5;
                uint pointsPerTurn = 10;
                double returnZ = probingHeight;
                double turn = Math.PI / (turnCount + 1);
                double interval = 2.0 * maxRadius / (pointsPerTurn - 1);
                var list = new List<CirclePoint>() { new CirclePoint(0, 0) };
                for (int i = 0; i < turnCount; i++)
                {
                    var angle = i * turn;
                    for (int j = 0; j < pointsPerTurn; j++)
                    {
                        var rad = -1.0 * maxRadius + (interval * j);
                        list.Add(new CirclePoint(Math.Sin(angle) * rad, Math.Cos(angle) * rad));
                    }
                }

                lines =
                    from item in list
                    select String.Format("G1 X{0:0.00} Y{1:0.00} Z{2:0.00}", item.x, item.y, returnZ);


                for(int i = 0; i < lines.Count(); i++)
                {
                    Connection.WriteLine(lines.ElementAt(i));
                    probe();
                }

            }
            else
            {

                Connection.WriteLine("G1 Z" + probingHeight.ToString(CultureInfo.InvariantCulture) + " X0 Y0" + " F" + UserVariables.xySpeed * speed);
                probe();
                Connection.WriteLine("G1 Z" + probingHeight.ToString(CultureInfo.InvariantCulture) + " X-" + valueXYLarge.ToString(CultureInfo.InvariantCulture) + " Y-" + valueXYSmall.ToString(CultureInfo.InvariantCulture) + " F" + UserVariables.xySpeed * speed);
                probe();
                Connection.WriteLine("G1 Z" + probingHeight.ToString(CultureInfo.InvariantCulture) + " X-" + valueXYLarge.ToString(CultureInfo.InvariantCulture) + " Y" + valueXYSmall.ToString(CultureInfo.InvariantCulture) + " F" + UserVariables.xySpeed * speed);
                probe();
                Connection.WriteLine("G1 Z" + probingHeight.ToString(CultureInfo.InvariantCulture) + " X0 Y" + valueZ.ToString(CultureInfo.InvariantCulture) + " F" + UserVariables.xySpeed * speed);
                probe();
                Connection.WriteLine("G1 Z" + probingHeight.ToString(CultureInfo.InvariantCulture) + " X" + valueXYLarge.ToString(CultureInfo.InvariantCulture) + " Y" + valueXYSmall.ToString(CultureInfo.InvariantCulture) + " F" + UserVariables.xySpeed * speed);
                probe();
                Connection.WriteLine("G1 Z" + probingHeight.ToString(CultureInfo.InvariantCulture) + " X" + valueXYLarge.ToString(CultureInfo.InvariantCulture) + " Y-" + valueXYSmall.ToString(CultureInfo.InvariantCulture) + " F" + UserVariables.xySpeed * speed);
                probe();
                Connection.WriteLine("G1 Z" + probingHeight.ToString(CultureInfo.InvariantCulture) + " X0 Y-" + valueZ.ToString(CultureInfo.InvariantCulture) + " F" + UserVariables.xySpeed * speed);
                probe();

                Connection.WriteLine("G1 Z" + probingHeight.ToString(CultureInfo.InvariantCulture) + " X0 Y0" + " F" + UserVariables.xySpeed * speed);
            }
            checkHeights = false;
        }


        /// <summary>
        /// Сбрасывает все переменные по-умолчанию
        /// </summary>
        public static void Reset()
        {
            currentPosition = 0;
            iteration = 0;
            checkHeights = false;
            wasZProbeHeightSet = false; // Флаг указывает был ли замер высоты рабочей области
            isHeuristicComplete = false;
        }
    }
}
