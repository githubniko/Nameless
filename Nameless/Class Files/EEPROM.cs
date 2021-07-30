using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;

namespace Nameless.Class_Files
{
    public static class EEPROM
    {
        public static bool isSendEEPROM = true; //все изменения в EEPROM отправлять в память принтера

        private static double _stepsPerMM;
        private static double _tempSPM;
        private static double _zMaxLength;
        private static double _zProbeHeight;
        private static double _zProbeSpeed;
        private static double _HRadius;
        private static double _diagonalRod;
        private static double _offsetX;
        private static double _offsetY;
        private static double _offsetZ;
        private static double _A;
        private static double _B;
        private static double _C;
        private static double _DA;
        private static double _DB;
        private static double _DC;
        private static double _HA;
        private static double _HB;
        private static double _HC;

        public static double stepsPerMM { get { return _stepsPerMM; } set { _stepsPerMM = Math.Round(value, 4); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 11, _stepsPerMM); } } }
        public static double tempSPM { get { return _tempSPM; } set { _tempSPM = Math.Round(value, 4); } }
        public static double zMaxLength { get { return _zMaxLength; } set { _zMaxLength = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 153, _zMaxLength); } } }
        public static double zProbeHeight { get { return _zProbeHeight; } set { _zProbeHeight = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 808, _zProbeHeight); } } }
        public static double zProbeSpeed { get { return _zProbeSpeed; } set { _zProbeSpeed = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 812, _zProbeSpeed); } } }
        public static double HRadius { get { return _HRadius; } set { _HRadius = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 885, _HRadius); } } }
        public static double diagonalRod { get { return _diagonalRod; } set { _diagonalRod = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 881, _diagonalRod); } } }
        public static double offsetX { get { return _offsetX; } set { _offsetX = Math.Round(value, 0); if (isSendEEPROM) { GCode.sendEEPROMVariable(1, 893, _offsetX); } } }
        public static double offsetY { get { return _offsetY; } set { _offsetY = Math.Round(value, 0); if (isSendEEPROM) { GCode.sendEEPROMVariable(1, 895, _offsetY); } } }
        public static double offsetZ { get { return _offsetZ; } set { _offsetZ = Math.Round(value, 0); if (isSendEEPROM) { GCode.sendEEPROMVariable(1, 897, _offsetZ); } } }
        public static double A { get { return _A; } set { _A = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 901, _A); } } }
        public static double B { get { return _B; } set { _B = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 905, _B); } } }
        public static double C { get { return _C; } set { _C = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 909, _C); } } }
        public static double DA { get { return _DA; } set { _DA = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 933, _DA); } } }
        public static double DB { get { return _DB; } set { _DB = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 937, _DB); } } }
        public static double DC { get { return _DC; } set { _DC = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 941, _DC); } } }
        public static double HA { get { return _HA; } set { _HA = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 913, _HA); } } }
        public static double HB { get { return _HB; } set { _HB = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 917, _HB); } } }
        public static double HC { get { return _HC; } set { _HC = Math.Round(value, 3); if (isSendEEPROM) { GCode.sendEEPROMVariable(3, 921, _HC); } } }
    }

    static class EEPROMFunctions
    {
        public static bool tempEEPROMSet = false;
        public static bool EEPROMRequestSent = false;
        public static bool EEPROMReadOnly = false;
        public static int EEPROMReadCount = 0;

        public delegate void MethodContainerUpdateEEPROM();
        public static event MethodContainerUpdateEEPROM onUpdateEEPROM; /// Событие вызывается после получения нового значения EPR из памяти принтера
                                                                        /// 
        //private static Regex zProbeRegex = new Regex(@"Z-probe:(?<z>[-]?\d+(\.\d+)?) X:(?<x>[-]?\d+(\.\d+)?) Y:(?<y>[-]?\d+(\.\d+)?)", RegexOptions.Compiled);
        // static Regex EEP = new Regex(@"EPR:() ()", RegexOptions.Compiled);

        public static void readEEPROM()
        {
            GCode.sendReadEEPROMCommand();
        }

        /// <summary>
        /// Парсит заначение и заносит в объект  EEPROM
        /// </summary>
        /// <param name="message"></param>
        public static void getEEPROM(string message)
        {
            if (message.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0] != "EPR") return; // выходим, если данные не EPR

            string[] parseEPR = message.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parseEPR.Count() < 2)
            {
                UserInterface.logConsole("Error: " + message);
                return;
            }
            EEPROM.isSendEEPROM = false;
            setEEPROM(int.Parse(parseEPR[1], CultureInfo.InvariantCulture), double.Parse(parseEPR[2], CultureInfo.InvariantCulture));
            EEPROM.isSendEEPROM = true;
            onUpdateEEPROM();
        }
        /// <summary>
        /// Сохраняет значения в EEPROM в соответствии с кодом
        /// </summary>
        private static void setEEPROM(int intParse, double doubleParse2)
        {
            switch (intParse)
            {
                case 11:
                    UserInterface.logConsole("EEPROM capture initiated");
                    EEPROM.stepsPerMM = doubleParse2;
                    EEPROM.tempSPM = doubleParse2;
                    break;
                case 153:
                    EEPROM.zMaxLength = doubleParse2;
                    break;
                case 808:
                    EEPROM.zProbeHeight = doubleParse2;
                    break;
                case 812:
                    EEPROM.zProbeSpeed = doubleParse2;
                    break;
                case 881:
                    EEPROM.diagonalRod = doubleParse2;
                    break;
                case 885:
                    EEPROM.HRadius = doubleParse2;
                    break;
                case 893:
                    EEPROM.offsetX = doubleParse2;
                    break;
                case 895:
                    EEPROM.offsetY = doubleParse2;
                    break;
                case 897:
                    EEPROM.offsetZ = doubleParse2;
                    break;
                case 901:
                    EEPROM.A = doubleParse2;
                    break;
                case 905:
                    EEPROM.B = doubleParse2;
                    break;
                case 909:
                    EEPROM.C = doubleParse2;
                    break;
                case 933:
                    EEPROM.DA = doubleParse2;
                    break;
                case 937:
                    EEPROM.DB = doubleParse2;
                    break;
                case 941:
                    EEPROM.DC = doubleParse2;
                    break;
                case 913:
                    EEPROM.HA = doubleParse2;
                    break;
                case 917:
                    EEPROM.HB = doubleParse2;
                    break;
                case 921:
                    EEPROM.HC = doubleParse2;
                    break;
            }
        }
    }
}
