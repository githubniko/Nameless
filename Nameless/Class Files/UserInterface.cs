using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;

namespace Nameless.Class_Files
{
    public static class UserVariables
    {
        //misc vars, alpha offsets, tower offsets, spm offsets, hrad offsets, drad offsets
        /*
        public double HRadRatio = -0.5F;
        public double DRadRatio = -0.5F;
        public double accuracy = 0.025F;
        public double calculationAccuracy = 0.001F;
        public double probingHeight = 10;

        public double offsetXCorrection = 1.55F;
        public double xxOppPerc = -0.35F;
        public double xyPerc = -0.23F;
        public double xyOppPerc = 0.16F;
        public double xzPerc = -0.23F;
        public double xzOppPerc = 0.16F;

        public double offsetYCorrection = 1.55F;
        public double yyOppPerc = -0.35F;
        public double yxPerc = -0.23F;
        public double yxOppPerc = 0.16F;
        public double yzPerc = -0.23F;
        public double yzOppPerc = 0.16F;

        public double offsetZCorrection = 1.55F;
        public double zzOppPerc = -0.35F;
        public double zxPerc = -0.23F;
        public double zxOppPerc = 0.16F;
        public double zyPerc = -0.23F;
        public double zyOppPerc = 0.16F;

        public double alphaRotationPercentageX = 1.725F;
        public double alphaRotationPercentageY = 1.725F;
        public double alphaRotationPercentageZ = 1.725F;
        public double deltaTower = 0.293F;
        public double deltaOpp = 0.214F;
        public double plateDiameter = 230F;
        public double diagonalRodLength = 269;
        public double FSROffset = 0.6F;
        public double probingSpeed = 5F;
        */

        public static double HRadRatio = -0.4; // Horizontal Radius Change:
        public static double DRadRatio = -0.05; // Diagonal Rod Change

        public static double accuracy;
        public static double calculationAccuracy = 0.001; // Calculation Accuracy (+/-):
        public static double probingHeight = 10.0; // высота с которой будет производится замер

        //XYZ Offset percs
        public static double offsetCorrection = -0.6; // Tower Offset Correction Main:
        public static double mainOppPerc = 0.5; // Tower Offset Correction Main Opposite:
        public static double towPerc = 0.3; // Tower Offset Correction Secondary:
        public static double oppPerc = -0.25; // Tower Offset Correction Secondary Opp:

        public static double alphaRotationPercentage = 2.5; //Alpha Rotation Percentage Correction:

        public static double plateDiameter;

        public static double probingSpeed;
        public static double xySpeed = 80; //feedrate in gcode mm/s

        public static int advancedCalCount;
        public static int maxIterations = 100; // число этераций калибровки

        public static bool checkTower = true;  // флаг устанавливает калиброку высоты
        public static bool checkHRad = true; // флаг устанавливает калибровку Horizontal Radius
        public static bool checkAlpha = true; // флаг устанавливает калибровку Alpha Radius
        public static bool checkDiagonalRod = true; // флаг устанавливает калибровку DiagonalRadius
        public static bool checkDeltaRadius = true; // флаг устанавливает калибровку DeltaRadius

        public static string typeCalibration = "Default"; // тип калибровки

    }


    static class UserInterface
    {
        
        public static void logConsole(string value)
        {
            if (!Program.mainFormTest.isClose) return;
            Program.mainFormTest.appendMainConsole(value);
            
        }


        public static void logPrinter(string value)
        {
            if (!Program.mainFormTest.isClose) return;
            Program.mainFormTest.appendPrinterConsole(value);
        }

    }
}
