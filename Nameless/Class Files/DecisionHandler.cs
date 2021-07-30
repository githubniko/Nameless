using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nameless.Class_Files
{
    static class DecisionHandler
    {
        
        // обработка значений, полученных от принтера 
        public static void handleInput2(string message)
        {
            if (message != "wait" && message != "ok 0")
            {
                UserInterface.logPrinter(message);
            }

            if (message == "start") // Если принтер перезагружен, то выставляем флаг и ждем пока загрузится
            {
                Connection.flagM110 = true;
                Program.mainFormTest.buttonEnabled(false); // выключаем кнопки
                Program.mainFormTest.EEPROMDisabled(); // вылючаем поля ввода EEPROM
                Program.mainFormTest.setResetFlags(); // обнуляем все переменные
                Connection.ClearBuffer(); // очищаем буффер
                Connection.WriteCOM("M110"); // код инициализации
            }
            else if (Connection.flagM110 && message == "ok") // если принтер загрузился
            {
                Connection.flagM110 = false;
                EEPROMFunctions.readEEPROM();  // загружаем EEPROM
                Program.mainFormTest.buttonEnabled(true); // включаем кнопки
                Program.mainFormTest.setButtonStopDisable();  // выключаем кнопку Stop
            }

            switch (message.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0]) // получаем первое значение из данных
            {
                case "EPR":// Если значение содержить EPR:
                    EEPROMFunctions.getEEPROM(message); // сохраняем значения в объект EEPROM
                    break;
                case "Z-probe":// Если значение содержить Z-Probe:
                    ZProbe zProbe = HeightFunctions.parseZProbe(message);

                    if (HeightFunctions.checkHeightsOnly) // Если запущен процесс измерения высоты
                    {
                        if (!HeightFunctions.getzMaxLength) // Если высота не известна
                        { 
                            EEPROM.zMaxLength = Convert.ToDouble(zProbe.Zprobe); // записываем новое значение высоты
                            //EEPROM.zProbeHeight = Convert.ToDouble(Math.Round(EEPROM.zMaxLength / 6) - HeightFunctions.parseZProbe(message)); // высичиляем значение до пробника
                            HeightFunctions.getzMaxLength = true; // высота измерена. Откллючает прием данных
                            UserInterface.logConsole("zMaxLength: " + EEPROM.zMaxLength + " mm");
                        }
                        HeightFunctions.checkHeightsOnly = false; // операция измерения высоты рабочей зоны завершена
                        HeightFunctions.heightsSet = false; // включаем прием данных замера по высотам
                        GCode.checkHeights = true; // отправить G-код измерения высот
                        
                                               
                        Program.mainFormTest.setEEPROMGUIList(); // обновляем форму
                    }
                    else if (!HeightFunctions.heightsSet) // тут обязательно else if
                    {
                        HeightFunctions.setHeights(zProbe); // парсим высоту
                        if (HeightFunctions.heightsSet) // Если прием данных замера по высотам завершился (см. HeightFunctions.setHeights(zProbe))
                        {
                            bool checkAccuracy = Calibration.checkAccuracy(); // проверяем точность и выводим график
                            Program.mainFormTest.setHeightsInvoke(); // выводим данные на рисунок

                            // НУЖНО ЛИ КАЛИБРОВАТЬ
                            if(Calibration.calibrationState)
                            {
                                if (!checkAccuracy)
                                {
                                    //GCode.checkHeights = true;
                                    if (Calibration.iterationNum+1 > UserVariables.maxIterations) // если число этераций превысило заданное
                                    {
                                        UserInterface.logConsole("\nThe number of iterations exceeded " + (Calibration.iterationNum) + ". Try changing the printer's geometry and repeat the calibration.\n");
                                        HeightFunctions.isProgress = false;
                                        Program.mainFormTest.setButtonStopDisable();
                                        // выключаем повторный проход
                                        HeightFunctions.heightsSet = true;
                                        GCode.checkHeights = false;
                                        Calibration.calibrationState = false;
                                        GCode.homeAxes();
                                        break;
                                    }
                                    else {
                                        UserInterface.logConsole("----------------------------------\n\nCalibration Iteration Number: " + (Calibration.iterationNum));
                                        if(!Calibration.calibrate()) // выполняем калибровку
                                        {
                                            Calibration.calibrationState = false; // выключаем калибровку, если нечего калибровать
                                        }
                                    }
                                }
                                else
                                {
                                    Calibration.calibrationState = false;
                                    GCode.checkHeights = false;
                                }
                                
                                
                                if (Calibration.calibrationState) // если калибровка не была остановлена во время calibrate(), то обновляем данные на графике и меняем высоту
                                { 
                                    EEPROM.zMaxLength = Heights.zMaxLength;
                                    UserInterface.logConsole("New zMaxLength: " + EEPROM.zMaxLength + " mm");
                                    Program.mainFormTest.setEEPROMGUIList();// обновляем форму
                                    //UserInterface.logConsole("\nContinuing calibration");
                                    // включаем повторный проход 
                                    HeightFunctions.heightsSet = false;
                                    GCode.checkHeights = true;
                                }
                                else
                                {
                                    UserInterface.logConsole("\nCalibration Сomplete\n");
                                    HeightFunctions.isProgress = false;
                                    Program.mainFormTest.setButtonStopDisable();
                                    // выключаем повторный проход
                                    HeightFunctions.heightsSet = true;
                                    GCode.checkHeights = false;
                                    Calibration.calibrationState = false;
                                    GCode.homeAxes();
                                }
                            }
                            else if (!Calibration.calibrationState && HeightFunctions.isProgress) // Замер высот завершился
                            {
                                UserInterface.logConsole("\nCheck Current Heights Completed\n");
                                HeightFunctions.isProgress = false;
                                Program.mainFormTest.setButtonStopDisable();
                                GCode.homeAxes();
                            }

                        }
                    }
                    break;
             }

            if (GCode.checkHeights) // если установлен флаг отправки G-кода измерения высот
            {
                GCode.positionFlow(); // отправляем G-код измерения высот
            }
         }
    }
}
