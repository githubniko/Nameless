using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Nameless.Class_Files
{
    static class Calibration
    {
        public static bool calibrateInProgress = false;
        public static bool calibrationState = false;
        public static bool calibrationComplete = false;

        public static int iterationNum = 0;

        public static bool calibrate()
        {
            int numFactors = 4;

            if (UserVariables.checkTower && UserVariables.checkHRad && UserVariables.checkAlpha && UserVariables.checkDiagonalRod) numFactors = 7;
            else if (UserVariables.checkTower && UserVariables.checkHRad && UserVariables.checkAlpha) numFactors = 6;
            else if (UserVariables.checkTower && UserVariables.checkHRad) numFactors = 4;
            else if (UserVariables.checkTower) numFactors = 3;

            if (UserVariables.typeCalibration == "Escher")
            {
                if (iterationNum > 2) return false; // пару опреаций достаточно по эшеру
                else
                    return Escher(numFactors);
            }
            else
                return basicCalibration();
        }
        /// <summary>
        /// Производит вычисления
        /// </summary>
        /// <returns>false - если текущие измерения попадают в предел точности Accuracy</returns>
        public static bool basicCalibration()
        {
            double ema = (Heights.XOpp + Heights.YOpp + Heights.ZOpp) / 3;
            double dXOpp = Math.Abs(Heights.XOpp / ema);
            double dYOpp = Math.Abs(Heights.YOpp / ema);
            double dZOpp = Math.Abs(Heights.ZOpp / ema);
            //double minK = Math.Min(Math.Min(dXOpp, dYOpp), dZOpp) * 3;
            double minK = ((dXOpp + dYOpp + dZOpp) / 3) + UserVariables.accuracy;
            UserInterface.logConsole("ema = " + ema.ToString());
            UserInterface.logConsole("dXOpp = " + dXOpp.ToString());
            UserInterface.logConsole("dYOpp = " + dYOpp.ToString());
            UserInterface.logConsole("dZOpp = " + dZOpp.ToString());
            UserInterface.logConsole("minK = " + minK.ToString());

            bool tower = UserVariables.checkTower && Math.Abs(Math.Max(Heights.X, Math.Max(Heights.Y, Heights.Z)) - Math.Min(Heights.X, Math.Min(Heights.Y, Heights.Z))) > UserVariables.accuracy; // если дельта между высотами больше, чем установленная погрешность

            double accuracy = UserVariables.accuracy;

            bool drad = UserVariables.checkDeltaRadius && !checkDiagonalRadius();

            bool alpha = UserVariables.checkAlpha && !drad && (
                   ((Heights.XOpp > Heights.YOpp + accuracy || Heights.XOpp < Heights.YOpp - accuracy || Heights.XOpp > Heights.ZOpp + accuracy || Heights.XOpp < Heights.ZOpp - accuracy) && Math.Abs(Heights.YOpp - Heights.ZOpp) > accuracy)
                || ((Heights.YOpp > Heights.XOpp + accuracy || Heights.YOpp < Heights.XOpp - accuracy || Heights.YOpp > Heights.ZOpp + accuracy || Heights.YOpp < Heights.ZOpp - accuracy) && Math.Abs(Heights.XOpp - Heights.ZOpp) > accuracy)
                || ((Heights.ZOpp > Heights.YOpp + accuracy || Heights.ZOpp < Heights.YOpp - accuracy || Heights.ZOpp > Heights.XOpp + accuracy || Heights.ZOpp < Heights.XOpp - accuracy) && Math.Abs(Heights.YOpp - Heights.XOpp) > accuracy));//возвращает false, если коррекции башни не нуждаются в исправлении


            /*bool alpha = UserVariables.checkAlpha && (Heights.XOpp > Heights.YOpp - UserVariables.accuracy ||
                                Heights.XOpp > Heights.ZOpp - UserVariables.accuracy ||
                                Heights.YOpp > Heights.XOpp - UserVariables.accuracy ||
                                Heights.YOpp > Heights.ZOpp - UserVariables.accuracy ||
                                Heights.ZOpp > Heights.XOpp - UserVariables.accuracy ||
                                Heights.ZOpp > Heights.YOpp - UserVariables.accuracy);*/

            /*bool hrad = UserVariables.checkHRad
                && (Heights.X < Heights.Center && Heights.Y < Heights.Center && Heights.Z < Heights.Center && Heights.XOpp < Heights.Center && Heights.YOpp < Heights.Center && Heights.ZOpp < Heights.Center)
                    || (Heights.X > Heights.Center && Heights.Y > Heights.Center && Heights.Z > Heights.Center && Heights.XOpp > Heights.Center && Heights.YOpp > Heights.Center && Heights.ZOpp > Heights.Center)
                && !checkAccuracy();*/

            bool hrad = UserVariables.checkHRad
                && ((Heights.X < Heights.Center && Heights.Y < Heights.Center && Heights.Z < Heights.Center)
                    || (Heights.X > Heights.Center && Heights.Y > Heights.Center && Heights.Z > Heights.Center))
                && !checkAccuracyXYZ();



            bool drod = UserVariables.checkDiagonalRod && !checkDRod();

            UserInterface.logConsole((UserVariables.checkTower ? "Tower:" + tower : "") + (UserVariables.checkHRad ? " HRad:" + hrad : "") + (UserVariables.checkDeltaRadius ? " DeltaRadius:" + drad : "") + (UserVariables.checkAlpha ? " Alpha:" + alpha : "") + (UserVariables.checkDiagonalRod ? " DRod:" + drod : ""));


            if (tower)
            {
                towerOffsets2(ref Heights.X, ref Heights.XOpp, ref Heights.Y, ref Heights.YOpp, ref Heights.Z, ref Heights.ZOpp);
                if (hrad) HRad3point(ref Heights.X, ref Heights.Y, ref Heights.Z);
            }
            else if (hrad)
            {
                //HRad(ref Heights.X, ref Heights.XOpp, ref Heights.Y, ref Heights.YOpp, ref Heights.Z, ref Heights.ZOpp);
                HRad3point(ref Heights.X, ref Heights.Y, ref Heights.Z);
            }
            else if (drad)
            {
                DeltaRadius();
            }
            else if (alpha)
            {
                alphaRotation(ref Heights.X, ref Heights.XOpp, ref Heights.Y, ref Heights.YOpp, ref Heights.Z, ref Heights.ZOpp);
            }
            else if (drod)
            {
                DRod(hrad);
            }
            else
            {
                return false;
            }
            return true;

        }
        /// <summary>
        /// Выполняет вычисления по Эшеру
        /// </summary>
        public static bool Escher(int numFactors = 6)
        {
            var delta = new Escher3d(
                        diagonal: EEPROM.diagonalRod,
                        radius: EEPROM.HRadius,
                        height: EEPROM.zMaxLength,
                        xstop: EEPROM.offsetX / EEPROM.stepsPerMM,
                        ystop: EEPROM.offsetY / EEPROM.stepsPerMM,
                        zstop: EEPROM.offsetZ / EEPROM.stepsPerMM,
                        xadj: EEPROM.A - 210.0,
                        yadj: EEPROM.B - 330.0,
                        zadj: EEPROM.C - 90.0
                    );

            double bedRadius = UserVariables.plateDiameter / 2;
            double bedRadiusSquared = bedRadius * bedRadius;

            var result = delta.DoDeltaCalibration(numFactors, HeightFunctions.list.ToList(), true);
            EEPROM.offsetX = Convert.ToInt32(delta.xstop * EEPROM.stepsPerMM);
            EEPROM.offsetY = Convert.ToInt32(delta.ystop * EEPROM.stepsPerMM);
            EEPROM.offsetZ = Convert.ToInt32(delta.zstop * EEPROM.stepsPerMM);
            EEPROM.A = 210.0 + (double)delta.xadj;
            EEPROM.B = 330.0 + (double)delta.yadj;
            EEPROM.C = 90.0 + (double)delta.zadj;
            EEPROM.HRadius = (double)delta.radius;
            EEPROM.diagonalRod = (double)delta.diagonal;
            Heights.zMaxLength = (double)delta.homedHeight - UserVariables.probingHeight - EEPROM.zProbeHeight;
            Program.mainFormTest.setEEPROMGUIList();// обновляем форму
            UserInterface.logConsole("Escher calculator, numFactors: " + numFactors);
            UserInterface.logConsole("Error escher: " + Math.Round(result.Item1, 2).ToString() + " " + Math.Round(result.Item2, 2).ToString());
            UserInterface.logConsole("offsetX:" + EEPROM.offsetX.ToString() + " offsetY:" + EEPROM.offsetY.ToString() + " offsetZ:" + EEPROM.offsetZ.ToString());
            UserInterface.logConsole("HRad:" + EEPROM.HRadius.ToString());
            UserInterface.logConsole("ABC:" + EEPROM.A + " " + EEPROM.B + " " + EEPROM.C);
            return true;
        }
        /// <summary>
        /// Проверяем, попадают ли измеренные велечины в нужны предел
        /// </summary>
        /// <returns>true если калибровка входит в заданный диапазон</returns>
        public static bool checkAccuracy()
        {
            iterationNum++;
            double accuracy = UserVariables.accuracy;

            double tempAccuracy = Math.Abs(Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(Heights.X, Heights.XOpp), Heights.Y), Heights.YOpp), Heights.Z), Heights.ZOpp), Heights.Center)
            - Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(Heights.X, Heights.XOpp), Heights.Y), Heights.YOpp), Heights.Z), Heights.ZOpp), Heights.Center));

            Program.mainFormTest.setAccuracyPoint(iterationNum, tempAccuracy); // рисуем график
            UserInterface.logConsole("Height difference: " + Math.Round(tempAccuracy, 3).ToString() + " mm");

            if (tempAccuracy <= accuracy) return true;

            return false;
        }
        /// <summary>
        /// Попадают ли измеренные велечины в нужны предел для XYZ относительно центра
        /// </summary>
        /// <returns></returns>
        public static bool checkAccuracyXYZ()
        {
            double accuracy = UserVariables.accuracy;

            double tempAccuracy = Math.Abs(Math.Max(Math.Max(Heights.X, Math.Max(Heights.Y, Heights.Z)), Heights.Center) - Math.Min(Math.Min(Heights.X, Math.Min(Heights.Y, Heights.Z)), Heights.Center));

            if (tempAccuracy <= accuracy) return true;

            return false;
        }
        /// <summary>
        /// Проверяет соответстиве Diagonal Radius
        /// </summary>
        /// <returns>Возвращает false если нужна калибровка</returns>
        public static bool checkDiagonalRadius()
        {
            double ema = (Heights.XOpp + Heights.YOpp + Heights.ZOpp) / 3;
            double dXOpp = Math.Abs(Heights.XOpp / ema);
            double dYOpp = Math.Abs(Heights.YOpp / ema);
            double dZOpp = Math.Abs(Heights.ZOpp / ema);
            //double minK = Math.Min(Math.Min(dXOpp, dYOpp), dZOpp) * 3;
            double minK = ((dXOpp + dYOpp + dZOpp) / 3) + UserVariables.accuracy;

            if (dXOpp > minK || dYOpp > minK || dZOpp > minK) return false;

            return true;
        }
        /// <summary>
        /// Проверяет нужно ли корректировать DROD
        /// </summary>
        /// <returns>Возвращает false если нужна калибровка</returns>
        public static bool checkDRod()
        {
            double accuracy = UserVariables.accuracy;

            double deltaOpp = Math.Abs(Math.Max(Heights.XOpp, Math.Max(Heights.YOpp, Heights.ZOpp)) - Math.Min(Heights.XOpp, Math.Min(Heights.YOpp, Heights.ZOpp)));
            double delta = Math.Abs(Math.Max(Heights.X, Math.Max(Heights.Y, Heights.Z)) - Math.Min(Heights.X, Math.Min(Heights.Y, Heights.Z)));
            double sa = (Heights.X + Heights.Y + Heights.Z) / 3;
            double saOpp = (Heights.XOpp + Heights.YOpp + Heights.ZOpp) / 3;

            if ((deltaOpp <= accuracy || delta <= accuracy) && Math.Abs(sa - saOpp) > accuracy) return false;

            return true;
        }

        private static void HRad(ref double X, ref double XOpp, ref double Y, ref double YOpp, ref double Z, ref double ZOpp)
        {
            double HRadSA = ((X + XOpp + Y + YOpp + Z + ZOpp) / 6);
            double HRadRatio = UserVariables.HRadRatio;

            EEPROM.HRadius -= (HRadSA / HRadRatio);

            UserInterface.logConsole("HRad:" + EEPROM.HRadius.ToString());
        }
        private static void HRad3point(ref double X, ref double Y, ref double Z)
        {
            double HRadSA = ((X + Y + Z) / 3);
            double HRadRatio = UserVariables.HRadRatio;

            EEPROM.HRadius -= (HRadSA / HRadRatio);

            UserInterface.logConsole("HRad:" + EEPROM.HRadius.ToString());
        }
        // вычисляет DeltaRdius для каждой башни
        private static void DeltaRadius()
        {
            double HRadRatio = UserVariables.HRadRatio;

            double ema = (Heights.XOpp + Heights.YOpp + Heights.ZOpp) / 3;
            double dXOpp = Math.Abs(Heights.XOpp / ema);
            double dYOpp = Math.Abs(Heights.YOpp / ema);
            double dZOpp = Math.Abs(Heights.ZOpp / ema);
            //double minK = Math.Min(Math.Min(dXOpp, dYOpp), dZOpp) * 3;
            double minK = ((dXOpp + dYOpp + dZOpp) / 3) + UserVariables.accuracy;

            double DASA = ((Heights.X + Heights.XOpp) / 2);
            double DBSA = ((Heights.Y + Heights.YOpp) / 2);
            double DCSA = ((Heights.Z + Heights.ZOpp) / 2);

            //EEPROM.HA = (dXOpp > minK) ? EEPROM.HA - (DASA / HRadRatio) : EEPROM.HA;
            //EEPROM.HB = (dYOpp > minK) ? EEPROM.HB - (DBSA / HRadRatio) : EEPROM.HB;
            //EEPROM.HC = (dZOpp > minK) ? EEPROM.HC - (DCSA / HRadRatio) : EEPROM.HC;

            double HA = EEPROM.HA - (DASA / HRadRatio);
            double HB = EEPROM.HB - (DBSA / HRadRatio);
            double HC = EEPROM.HC - (DCSA / HRadRatio);

            // Определяем ближайшее к нулю значение
            double[] minArr = new double[]{ HA, HB, HC };
            double min = Math.Abs(minArr[0]);
            for (int i=0; i < minArr.Count()-1; i++) {
                min = (Math.Abs(minArr[i+1]) < min) ? minArr[i+1] : min; // ближкайшее к нулю значение
            }

            EEPROM.HA = HA - min;
            EEPROM.HB = HB - min;
            EEPROM.HC = HC - min;

            UserInterface.logConsole("DeltaRadiusABC:" + EEPROM.HA.ToString() + " " + EEPROM.HB.ToString() + " " + EEPROM.HC.ToString());
        }
        /// <summary>
        /// Калиброует длину тяг
        /// </summary>
        /// <param name="HRad">Указывает, нужно ли калибровать горизонтальный радиус</param>
        private static void DRod(bool HRad = false)
        {
            double sa = (Heights.X + Heights.Y + Heights.Z) / 3;
            double saOpp = (Heights.XOpp + Heights.YOpp + Heights.ZOpp) / 3;

            double deltaSa = sa - saOpp;

            if (deltaSa == 0) { UserInterface.logConsole("Error, deltaSa = 0"); return; }

            EEPROM.diagonalRod -= (deltaSa / UserVariables.DRadRatio);
            UserInterface.logConsole("DRod:" + EEPROM.diagonalRod.ToString());

            EEPROM.HRadius -= (deltaSa / UserVariables.HRadRatio);
            UserInterface.logConsole("HRad:" + EEPROM.HRadius.ToString());

        }
        private static void towerOffsets2(ref double X, ref double XOpp, ref double Y, ref double YOpp, ref double Z, ref double ZOpp)
        {
            double max = Math.Max(X, Math.Max(Y, Z));

            int znak = (max > 0) ? -1 : 1;

            double offsetX = -(X + znak * max) * EEPROM.stepsPerMM;
            double offsetY = -(Y + znak * max) * EEPROM.stepsPerMM;
            double offsetZ = -(Z + znak * max) * EEPROM.stepsPerMM;

            offsetX = EEPROM.offsetX + Convert.ToInt32(offsetX);
            offsetY = EEPROM.offsetY + Convert.ToInt32(offsetY);
            offsetZ = EEPROM.offsetZ + Convert.ToInt32(offsetZ);

            double min = Math.Min(offsetX, Math.Min(offsetY, offsetZ));
            znak = (min > 0) ? -1 : 1;
            //round to the nearest whole number
            EEPROM.offsetX = (offsetX + znak * min);
            EEPROM.offsetY = (offsetY + znak * min);
            EEPROM.offsetZ = (offsetZ + znak * min);

            UserInterface.logConsole("offsetX:" + EEPROM.offsetX.ToString() + " offsetY:" + EEPROM.offsetY.ToString() + " offsetZ:" + EEPROM.offsetZ.ToString());
        }
        private static void towerOffsets(ref double X, ref double XOpp, ref double Y, ref double YOpp, ref double Z, ref double ZOpp)
        {
            int j = 0;
            double tempX2 = X;
            double tempXOpp2 = XOpp;
            double tempY2 = Y;
            double tempYOpp2 = YOpp;
            double tempZ2 = Z;
            double tempZOpp2 = ZOpp;
            double offsetX = EEPROM.offsetX;
            double offsetY = EEPROM.offsetY;
            double offsetZ = EEPROM.offsetZ;
            double stepsPerMM = EEPROM.stepsPerMM;

            double towMain = UserVariables.offsetCorrection;//0.6
            double oppMain = UserVariables.mainOppPerc;//0.5
            double towSub = UserVariables.towPerc;//0.3
            double oppSub = UserVariables.oppPerc;//-0.25

            while (j < 100)
            {
                if (Math.Abs(tempX2) > UserVariables.accuracy || Math.Abs(tempY2) > UserVariables.accuracy || Math.Abs(tempZ2) > UserVariables.accuracy)
                {
                    offsetX -= tempX2 * stepsPerMM * (1 / towMain);

                    tempXOpp2 -= tempX2 * (oppMain / towMain);
                    tempY2 -= tempX2 * (towSub / towMain);
                    tempYOpp2 -= tempX2 * (-oppSub / towMain);
                    tempZ2 -= tempX2 * (towSub / towMain);
                    tempZOpp2 -= tempX2 * (-oppSub / towMain);
                    tempX2 -= tempX2 / 1;

                    offsetY -= tempY2 * stepsPerMM * (1 / towMain);

                    tempYOpp2 -= tempY2 * (oppMain / towMain);
                    tempX2 -= tempY2 * (towSub / towMain);
                    tempXOpp2 -= tempY2 * (-oppSub / towMain);
                    tempZ2 -= tempY2 * (towSub / towMain);
                    tempZOpp2 -= tempY2 * (-oppSub / towMain);
                    tempY2 -= tempY2 / 1;

                    offsetZ -= tempZ2 * stepsPerMM * (1 / towMain);

                    tempZOpp2 -= tempZ2 * (oppMain / towMain);
                    tempX2 -= tempZ2 * (towSub / towMain);
                    tempXOpp2 += tempZ2 * (-oppSub / towMain);
                    tempY2 -= tempZ2 * (towSub / towMain);
                    tempYOpp2 -= tempZ2 * (-oppSub / towMain);
                    tempZ2 -= tempZ2 / 1;

                    tempX2 = Validation.checkZero(tempX2);
                    tempY2 = Validation.checkZero(tempY2);
                    tempZ2 = Validation.checkZero(tempZ2);
                    tempXOpp2 = Validation.checkZero(tempXOpp2);
                    tempYOpp2 = Validation.checkZero(tempYOpp2);
                    tempZOpp2 = Validation.checkZero(tempZOpp2);

                    if (Math.Abs(tempX2) <= UserVariables.accuracy && Math.Abs(tempY2) <= UserVariables.accuracy && Math.Abs(tempZ2) <= UserVariables.accuracy)
                    {
                        UserInterface.logConsole("\nVHeights :" + tempX2 + " " + tempXOpp2 + " " + tempY2 + " " + tempYOpp2 + " " + tempZ2 + " " + tempZOpp2);
                        UserInterface.logConsole("Offs :" + offsetX + " " + offsetY + " " + offsetZ);
                        UserInterface.logConsole("No Hrad correction");

                        double smallest = Math.Min(offsetX, Math.Min(offsetY, offsetZ));

                        offsetX -= smallest;
                        offsetY -= smallest;
                        offsetZ -= smallest;

                        UserInterface.logConsole("Offs :" + offsetX + " " + offsetY + " " + offsetZ);

                        X = tempX2;
                        XOpp = tempXOpp2;
                        Y = tempY2;
                        YOpp = tempYOpp2;
                        Z = tempZ2;
                        ZOpp = tempZOpp2;

                        //round to the nearest whole number
                        EEPROM.offsetX = Convert.ToInt32(offsetX);
                        EEPROM.offsetY = Convert.ToInt32(offsetY);
                        EEPROM.offsetZ = Convert.ToInt32(offsetZ);

                        j = 100;
                    }
                    else if (j == 99)
                    {
                        UserInterface.logConsole("\nVHeights :" + tempX2 + " " + tempXOpp2 + " " + tempY2 + " " + tempYOpp2 + " " + tempZ2 + " " + tempZOpp2);
                        UserInterface.logConsole("Offs :" + offsetX + " " + offsetY + " " + offsetZ);
                        double dradCorr = tempX2 * -1.25;
                        double HRadRatio = UserVariables.HRadRatio;

                        EEPROM.HRadius += dradCorr;

                        EEPROM.offsetX = 0;
                        EEPROM.offsetY = 0;
                        EEPROM.offsetZ = 0;

                        //hradsa = dradcorr
                        //solve inversely from previous method
                        double HRadOffset = HRadRatio * dradCorr;

                        tempX2 -= HRadOffset;
                        tempY2 -= HRadOffset;
                        tempZ2 -= HRadOffset;
                        tempXOpp2 -= HRadOffset;
                        tempYOpp2 -= HRadOffset;
                        tempZOpp2 -= HRadOffset;

                        UserInterface.logConsole("Hrad correction: " + dradCorr);
                        UserInterface.logConsole("HRad: " + EEPROM.HRadius.ToString());

                        j = 0;
                    }
                    else
                    {
                        j++;
                    }

                    //UserInterface.logConsole("Offs :" + offsetX + " " + offsetY + " " + offsetZ);
                    //UserInterface.logConsole("VHeights :" + tempX2 + " " + tempXOpp2 + " " + tempY2 + " " + tempYOpp2 + " " + tempZ2 + " " + tempZOpp2);
                }
                else
                {
                    j = 100;

                    UserInterface.logConsole("\nTower Offsets and Delta Radii Calibrated");
                }
            }

            if (EEPROM.offsetX > 1000 || EEPROM.offsetY > 1000 || EEPROM.offsetZ > 1000)
            {
                UserInterface.logConsole("\nTower offset calibration error, setting default values.");
                UserInterface.logConsole("Tower offsets before damage prevention: X" + offsetX + " Y" + offsetY + " Z" + offsetZ);
                offsetX = 0;
                offsetY = 0;
                offsetZ = 0;
            }
        }

        private static void alphaRotation(ref double X, ref double XOpp, ref double Y, ref double YOpp, ref double Z, ref double ZOpp)
        {
            double offsetX = EEPROM.offsetX;
            double offsetY = EEPROM.offsetY;
            double offsetZ = EEPROM.offsetZ;

            //change to object
            double alphaRotationPercentage = UserVariables.alphaRotationPercentage;

            int k = 0;
            while (k < 100)
            {
                //X Alpha Rotation
                if (YOpp < ZOpp)
                {
                    double ZYOppAvg = (YOpp - ZOpp) / 2;
                    EEPROM.A -= (ZYOppAvg * alphaRotationPercentage); // (0.5/((diff y0 and z0 at X + 0.5)-(diff y0 and z0 at X = 0))) * 2 = 1.75
                    YOpp = YOpp - ZYOppAvg;
                    ZOpp = ZOpp + ZYOppAvg;
                }
                else if (YOpp > ZOpp)
                {
                    double ZYOppAvg = (ZOpp - YOpp) / 2;

                    EEPROM.A += (ZYOppAvg * alphaRotationPercentage);
                    YOpp = YOpp + ZYOppAvg;
                    ZOpp = ZOpp - ZYOppAvg;
                }

                //Y Alpha Rotation
                if (ZOpp < XOpp)
                {
                    double XZOppAvg = (ZOpp - XOpp) / 2;
                    EEPROM.B -= (XZOppAvg * alphaRotationPercentage);
                    ZOpp = ZOpp - XZOppAvg;
                    XOpp = XOpp + XZOppAvg;
                }
                else if (ZOpp > XOpp)
                {
                    double XZOppAvg = (XOpp - ZOpp) / 2;

                    EEPROM.B += (XZOppAvg * alphaRotationPercentage);
                    ZOpp = ZOpp + XZOppAvg;
                    XOpp = XOpp - XZOppAvg;
                }
                //Z Alpha Rotation
                if (XOpp < YOpp)
                {
                    double YXOppAvg = (XOpp - YOpp) / 2;
                    EEPROM.C -= (YXOppAvg * alphaRotationPercentage);
                    XOpp = XOpp - YXOppAvg;
                    YOpp = YOpp + YXOppAvg;
                }
                else if (XOpp > YOpp)
                {
                    double YXOppAvg = (YOpp - XOpp) / 2;

                    EEPROM.C += (YXOppAvg * alphaRotationPercentage);
                    XOpp = XOpp + YXOppAvg;
                    YOpp = YOpp - YXOppAvg;
                }

                //determine if value is close enough
                double hTow = Math.Max(Math.Max(XOpp, YOpp), ZOpp);
                double lTow = Math.Min(Math.Min(XOpp, YOpp), ZOpp);
                double towDiff = hTow - lTow;

                if (towDiff < UserVariables.calculationAccuracy)
                {
                    k = 100;

                    //log
                    UserInterface.logConsole("ABC:" + EEPROM.A + " " + EEPROM.B + " " + EEPROM.C);
                }
                else
                {
                    k++;
                }
            }
        }

        /// <summary>
        /// Сбрасывает все переменные по-умолчанию
        /// </summary>
        public static void Reset()
        {
            calibrateInProgress = false;
            calibrationState = false;
            calibrationComplete = false;
            iterationNum = 0;
        }
    }
}
