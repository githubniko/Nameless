using Nameless.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;
using System.Globalization;

using Tao.OpenGl;
using Tao.FreeGlut;
using Tao.Platform.Windows;

namespace Nameless.Class_Files
{
    public partial class mainForm : Form
    {
        public bool isClose; // закрыто ли окно
        public mainForm()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentCulture;


            InitializeComponent();
            AnT.InitializeContexts();

            isClose = true;

            consoleMain.Text = "";
            consoleMain.ScrollBars = RichTextBoxScrollBars.Vertical;

            // Basic set of standard baud rates.
            baudRateCombo.Items.Add("250000");
            baudRateCombo.Items.Add("115200");
            baudRateCombo.Items.Add("57600");
            baudRateCombo.Items.Add("38400");
            baudRateCombo.Items.Add("19200");
            baudRateCombo.Items.Add("9600");
            baudRateCombo.Text = (string)Settings.Default["baudRateCombo"];  // This is the default for most RAMBo controllers.

            comboBoxTypeCalibration.Text = (string)Settings.Default["typeCalibration"];

            // Build the combobox of available ports.
            string[] ports = SerialPort.GetPortNames();

            if (ports.Length >= 1)
            {
                Dictionary<string, string> comboSource = new Dictionary<string, string>();

                int count = 0;

                foreach (string element in ports)
                {
                    comboSource.Add(ports[count], ports[count]);
                    count++;
                }

                portsCombo.DataSource = new BindingSource(comboSource, null);
                portsCombo.DisplayMember = "Key";
                portsCombo.ValueMember = "Value";
                portsCombo.SelectedIndex = count-1;
            }
            else
            {
                UserInterface.logConsole("No ports available");
            }

            //accuracyTime.Series["Accuracy"].Points.AddXY(0, 0);
            textAccuracy2.Text = Settings.Default["textAccuracy2"].ToString();
            textPlateDiameter.Text = (string)Settings.Default["textPlateDiameter"].ToString();

            EEPROMFunctions.onUpdateEEPROM += setEEPROMGUIList;

            Connection.onConnected += () =>
            {
                Invoke((MethodInvoker)delegate { 
                    connectButton.Text = "Disconnect";
                    connectButton.Enabled = true;
                });
            };
            Connection.onDisconnected += () =>
            {
                if (!isClose) return; // выход если приложение было закрыто

                Invoke((MethodInvoker)delegate { 
                    buttonEnabled(false);
                    connectButton.Text = "Connect";
                    UserInterface.logConsole("Disconnected");
                });
                
            };

        }
        /// <summary>
        /// Функция Вкл/Выкл кнопки при отсутствии соединения
        /// </summary>
        /// <param name="enabled"></param>
        public void buttonEnabled(bool enabled)
        {
            Invoke((MethodInvoker)delegate {
                
                if(enabled) // включает
                {
                    sendGCode.Enabled = true;
                    GCodeBox.Enabled = true;
                    sendEEPROMButton.Enabled = true;
                    readEEPROM.Enabled = true;

                    calibrateButton.Enabled = true;
                    buttonContinueCalibration.Enabled = true;
                    checkHeights.Enabled = true;
                }
                else// выключает
                {
                    sendGCode.Enabled = false;
                    GCodeBox.Enabled = false;
                    stopBut.Enabled = false;
                    sendEEPROMButton.Enabled = false;
                    readEEPROM.Enabled = false;

                    calibrateButton.Enabled = false;
                    buttonContinueCalibration.Enabled = false;
                    checkHeights.Enabled = false;
                }
            });
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
                if (!Connection.IsConnect())
                {
                    Settings.Default["baudRateCombo"] = baudRateCombo.Text;
                    Nameless.Properties.Settings.Default.Save();

                    connectButton.Enabled = false;
                    setResetFlags();
                    //Invoke((MethodInvoker)delegate
                    //{
                            if(!Connection.Connect()) connectButton.Enabled = true;
                    //});
                    
                }
                else
                {
                    //Invoke((MethodInvoker)delegate
                    //{
                        Connection.Disconnect();
                    //});
                }
        }


        private void calibrateButton_Click(object sender, EventArgs e)
        {
            accuracyTime.Series["Accuracy"].Points.Clear();// очицаем график

            EEPROM.offsetX = 0;
            EEPROM.offsetY = 0;
            EEPROM.offsetZ = 0;
            EEPROM.A = 210;
            EEPROM.B = 330;
            EEPROM.C = 90;
            EEPROM.HA = 0;
            EEPROM.HB = 0;
            EEPROM.HC = 0;
            
            GCode.checkHeights = false;
            HeightFunctions.getzMaxLength = false; // измерить высоту
            Calibration.calibrationState = true; // вкл. калибровку
            Calibration.iterationNum = 0;
            UserInterface.logConsole("Reset Tower, HRad, Alpha, DeltaDad");
            Program.mainFormTest.setEEPROMGUIList();

        }
        
        public void appendMainConsole(string value)
        {
            Invoke((MethodInvoker)delegate { consoleMain.AppendText(value + "\n"); });
            Invoke((MethodInvoker)delegate { consoleMain.ScrollToCaret(); });
        }

        public void appendPrinterConsole(string value)
        {
            Invoke((MethodInvoker)delegate { consolePrinter.Items.Add(value); });
            Invoke((MethodInvoker)delegate { if (CheckBoxAutoScroll.Checked) consolePrinter.SelectedIndex = consolePrinter.Items.Count-1; });
        }

       

        private void sendGCode_Click(object sender, EventArgs e)
        {
            if (HeightFunctions.isProgress) return;
            sendGCodeText();
        }

        private void GCodeBox_KeyUp(object sender, KeyEventArgs e) {
            if (HeightFunctions.isProgress) return;
            if (e.KeyCode == Keys.Enter)
                sendGCodeText();
        }

        private void sendGCodeText() 
            {
            if (Connection._serialPort.IsOpen) {
                Connection.WriteLine(GCodeBox.Text.ToString().ToUpper());
                UserInterface.logConsole("Sent: " + GCodeBox.Text.ToString().ToUpper());
            }
            else {
                UserInterface.logConsole("Not Connected");
            }
        }

        public void setAccuracyPoint(double x, double y)
        {
            Invoke((MethodInvoker)delegate
            {
                accuracyTime.Refresh();
                accuracyTime.Series["Accuracy"].Points.AddXY(x, y);
            });
        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            var about = new AboutBox1();
            about.Show();
        }
        private void contactButton_Click_1(object sender, EventArgs e)
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "https://www.thingiverse.com/thing:3259394";
            proc.Start();
        }


        public void setHeightsInvoke()
        {
            double X = Heights.X;
            double XOpp = Heights.XOpp;
            double Y = Heights.Y;
            double YOpp = Heights.YOpp;
            double Z = Heights.Z;
            double ZOpp = Heights.ZOpp;

            //set base heights for advanced calibration comparison
            Invoke((MethodInvoker)delegate { this.XText.Text = Math.Round(X, 3).ToString(); });
            Invoke((MethodInvoker)delegate { this.XOppText.Text = Math.Round(XOpp, 3).ToString(); });
            Invoke((MethodInvoker)delegate { this.YText.Text = Math.Round(Y, 3).ToString(); });
            Invoke((MethodInvoker)delegate { this.YOppText.Text = Math.Round(YOpp, 3).ToString(); });
            Invoke((MethodInvoker)delegate { this.ZText.Text = Math.Round(Z, 3).ToString(); });
            Invoke((MethodInvoker)delegate { this.ZOppText.Text = Math.Round(ZOpp, 3).ToString(); });

            // Выводим 3д графику
            //HeightFunctions.list.ToArray();
        }
        /// <summary>
        /// Выключает поля ввода EEPROM
        /// </summary>
        public void EEPROMDisabled()
        {
            Invoke((MethodInvoker)delegate
            {
                this.textPlateDiameter.Enabled = false;
                this.textAccuracy2.Enabled = false;
                this.stepsPerMMText.Enabled = false;
                this.zMaxLengthText.Enabled = false;
                this.zProbeText.Enabled = false;
                this.zProbeSpeedText.Enabled = false;
                this.diagonalRod.Enabled = false;
                this.HRadiusText.Enabled = false;
                this.offsetXText.Enabled = false;
                this.offsetYText.Enabled = false;
                this.offsetZText.Enabled = false;
                this.AText.Enabled = false;
                this.BText.Enabled = false;
                this.CText.Enabled = false;
                this.DAText.Enabled = false;
                this.DBText.Enabled = false;
                this.DCText.Enabled = false;
                this.HAText.Enabled = false;
                this.HBText.Enabled = false;
                this.HCText.Enabled = false;
                this.sendEEPROMButton.Enabled = false;
                this.readEEPROM.Enabled = false;
            });
        }
        /// <summary>
        /// Включает поля ввода EEPROM
        /// </summary>
        public void EEPROMEnabled()
        {
            Invoke((MethodInvoker)delegate
            {
                this.textPlateDiameter.Enabled = true;
                this.textAccuracy2.Enabled = true;
                this.stepsPerMMText.Enabled = true;
                this.zMaxLengthText.Enabled = true;
                this.zProbeText.Enabled = true;
                this.zProbeSpeedText.Enabled = true;
                this.diagonalRod.Enabled = true;
                this.HRadiusText.Enabled = true;
                this.offsetXText.Enabled = true;
                this.offsetYText.Enabled = true;
                this.offsetZText.Enabled = true;
                this.AText.Enabled = true;
                this.BText.Enabled = true;
                this.CText.Enabled = true;
                this.DAText.Enabled = true;
                this.DBText.Enabled = true;
                this.DCText.Enabled = true;
                this.HAText.Enabled = true;
                this.HBText.Enabled = true;
                this.HCText.Enabled = true;
                this.sendEEPROMButton.Enabled = true;
                this.readEEPROM.Enabled = true;
            });
        }

        // Заполняет форму значениями из EEPROM
        public void setEEPROMGUIList()
        {
            Invoke((MethodInvoker)delegate
            {
                this.stepsPerMMText.Text = EEPROM.stepsPerMM.ToString();
                this.zMaxLengthText.Text = EEPROM.zMaxLength.ToString();
                this.zProbeText.Text = EEPROM.zProbeHeight.ToString();
                this.zProbeSpeedText.Text = EEPROM.zProbeSpeed.ToString();
                this.diagonalRod.Text = EEPROM.diagonalRod.ToString();
                this.HRadiusText.Text = EEPROM.HRadius.ToString();
                this.offsetXText.Text = EEPROM.offsetX.ToString();
                this.offsetYText.Text = EEPROM.offsetY.ToString();
                this.offsetZText.Text = EEPROM.offsetZ.ToString();
                this.AText.Text = EEPROM.A.ToString();
                this.BText.Text = EEPROM.B.ToString();
                this.CText.Text = EEPROM.C.ToString();
                this.DAText.Text = EEPROM.DA.ToString();
                this.DBText.Text = EEPROM.DB.ToString();
                this.DCText.Text = EEPROM.DC.ToString();
                this.HAText.Text = EEPROM.HA.ToString();
                this.HBText.Text = EEPROM.HB.ToString();
                this.HCText.Text = EEPROM.HC.ToString();
            });
        }

        private void sendEEPROMButton_Click(object sender, EventArgs e)
        {
            EEPROM.stepsPerMM = Convert.ToDouble(this.stepsPerMMText.Text);
            EEPROM.zMaxLength = Convert.ToDouble(this.zMaxLengthText.Text);
            EEPROM.zProbeHeight = Convert.ToDouble(this.zProbeText.Text);
            EEPROM.zProbeSpeed = Convert.ToDouble(this.zProbeSpeedText.Text);
            EEPROM.diagonalRod = Convert.ToDouble(this.diagonalRod.Text);
            EEPROM.HRadius = Convert.ToDouble(this.HRadiusText.Text);
            EEPROM.offsetX = Convert.ToDouble(this.offsetXText.Text);
            EEPROM.offsetY = Convert.ToDouble(this.offsetYText.Text);
            EEPROM.offsetZ = Convert.ToDouble(this.offsetZText.Text);
            EEPROM.A = Convert.ToDouble(this.AText.Text);
            EEPROM.B = Convert.ToDouble(this.BText.Text);
            EEPROM.C = Convert.ToDouble(this.CText.Text);
            EEPROM.DA = Convert.ToDouble(this.DAText.Text);
            EEPROM.DB = Convert.ToDouble(this.DBText.Text);
            EEPROM.DC = Convert.ToDouble(this.DCText.Text);
            EEPROM.HA = Convert.ToDouble(this.HAText.Text);
            EEPROM.HB = Convert.ToDouble(this.HBText.Text);
            EEPROM.HC = Convert.ToDouble(this.HCText.Text);

        }
        
        /// <summary>
        /// Заносит данные из формы в объект EEPROM
        /// </summary>
        private void readEEPROM_Click(object sender, EventArgs e)
        {
            if (Connection._serialPort.IsOpen)
            {
                EEPROMFunctions.readEEPROM();
            }
            else
                UserInterface.logConsole("Not Connected");
        }

        public void setButtonValues()
        {
            Invoke((MethodInvoker)delegate
            {
                this.textAccuracy2.Text = UserVariables.accuracy.ToString();
                this.textPlateDiameter.Text = UserVariables.plateDiameter.ToString();
            });
        }

        public void setUserVariables()
        {
            UserVariables.accuracy = Convert.ToDouble(this.textAccuracy2.Text);
            UserVariables.plateDiameter = Convert.ToDouble(this.textPlateDiameter.Text);
            UserVariables.checkTower = this.checkBoxTower.Checked;
            UserVariables.checkHRad = this.checkBoxHRad.Checked;
            UserVariables.checkAlpha = this.checkBoxAlpha.Checked;
            UserVariables.checkDiagonalRod = this.checkBoxDiagonalRod.Checked;
            UserVariables.checkDeltaRadius = this.checkBoxDeltaRad.Checked;

            Settings.Default["textAccuracy2"] = Convert.ToDouble(textAccuracy2.Text);
            Settings.Default["textPlateDiameter"] = Convert.ToDouble(textPlateDiameter.Text);
            Nameless.Properties.Settings.Default.Save();
        }

        private void checkHeights_Click(object sender, EventArgs e)
        {
            setUserVariables(); // копируем значения из формы в UserVariables

            setButtonStopEnable();

            GCode.checkHeights = false;
            HeightFunctions.getzMaxLength = false;
            Calibration.calibrationState = false;

            
            HeightFunctions.isProgress = true;

            if (!checkBoxMeasureZMaxLength.Checked)
            {
                HeightFunctions.checkHeightsOnly = false; // операция измерения высоты рабочей зоны завершена
                HeightFunctions.heightsSet = false; // включаем прием данных замера по высотам
                GCode.checkHeights = true; // отправить G-код измерения высот
                HeightFunctions.getzMaxLength = true;

                GCode.positionFlow(); // отправляем G-код измерения высот
            } else 
            GCode.positionFlowHeight();// запускаем последовательность измерение высоты, а затем замер по координатам
            UserInterface.logConsole("Start Check Heights");
            UserInterface.logConsole("Plate Diameter: " + UserVariables.plateDiameter + " mm");
            UserInterface.logConsole("Height-Map Accuracy: " + UserVariables.accuracy + " mm");
        }

        private void stopBut_Click(object sender, EventArgs e)
        {
            try
            {
                stopBut.Enabled = false;
                setResetFlags();
                Connection.Reset(); // сброс пинтера

                UserInterface.logConsole("Stop\n");
            }
            catch
            {

            }

        }
        

        private void mainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            isClose = false;
            Connection.Disconnect();
        }

        private void CheckBoxAutoScroll_CheckedChanged(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate { if (CheckBoxAutoScroll.Checked) consolePrinter.SelectedIndex = consolePrinter.Items.Count - 1; });
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label36_Click(object sender, EventArgs e)
        {

        }

        private void consolePrinter_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.consolePrinter.Invalidate();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void consolePrinter_DrawItem(object sender, DrawItemEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            e.DrawBackground();
            Brush myBrush = Brushes.Silver;

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                myBrush = Brushes.Black;
                e.Graphics.FillRectangle(new SolidBrush(Color.Yellow), e.Bounds);
            }

            else
            {
                e.Graphics.FillRectangle(Brushes.Black, e.Bounds);

            }

            e.Graphics.DrawString(listBox.Items[e.Index].ToString(), e.Font, myBrush, e.Bounds);
            e.DrawFocusRectangle();
        }

        private void consolePrinter_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            // Cast the sender object back to ListBox type.
            ListBox listBox = (ListBox)sender;
            e.ItemHeight = listBox.Font.Height;
        }
        /// <summary>
        /// Метод сбрасывает все флаги в первоначальные
        /// </summary>
        public void setResetFlags()
        {
            Calibration.Reset();
            HeightFunctions.Reset();
            GCode.Reset();
        }
        /// <summary>
        /// Устанавливает кнопки после завершения процедуры
        /// </summary>
        public void setButtonStopDisable()
        {
            Invoke((MethodInvoker)delegate {
                stopBut.Enabled = false;
                checkHeights.Enabled = true;
                calibrateButton.Enabled = true;
                buttonContinueCalibration.Enabled = true;
                sendGCode.Enabled = true;
                GCodeBox.Enabled = true;

                checkBoxTower.Enabled = true;
                checkBoxHRad.Enabled = true;
                checkBoxAlpha.Enabled = true;
                checkBoxDeltaRad.Enabled = UserVariables.typeCalibration != "Escher" ? true : false;
                checkBoxDiagonalRod.Enabled = true;
                comboBoxTypeCalibration.Enabled = true;
            });
            EEPROMEnabled();
        }
        public void setButtonStopEnable()
        {
            Invoke((MethodInvoker)delegate
            {
                stopBut.Enabled = true;
                checkHeights.Enabled = false;
                calibrateButton.Enabled = false;
                buttonContinueCalibration.Enabled = false;
                sendGCode.Enabled = false;
                GCodeBox.Enabled = false;

                checkBoxTower.Enabled = false;
                checkBoxHRad.Enabled = false;
                checkBoxAlpha.Enabled = false;
                checkBoxDeltaRad.Enabled = false;
                checkBoxDiagonalRod.Enabled = false;
                comboBoxTypeCalibration.Enabled = false;
            });
            EEPROMDisabled();
        }

        private void textAccuracy2_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = textAccuracy2;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;
                
            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void textPlateDiameter_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = textPlateDiameter;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void stepsPerMMText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = stepsPerMMText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void zMaxLengthText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = zMaxLengthText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void zProbeText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = zProbeText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void zProbeSpeedText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = zProbeSpeedText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void HRadiusText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = HRadiusText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void diagonalRod_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = diagonalRod;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void offsetXText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = offsetXText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void offsetYText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = offsetYText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void offsetZText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = offsetZText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void AText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = AText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void BText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = BText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void CText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = CText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void DAText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = DAText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void DBText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = DBText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void DCText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = DCText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void portsCombo_MouseDown(object sender, MouseEventArgs e)
        {
            // Build the combobox of available ports.
            string[] ports = SerialPort.GetPortNames();

            if (ports.Length >= 1)
            {
                Dictionary<string, string> comboSource = new Dictionary<string, string>();

                int count = 0;

                foreach (string element in ports)
                {
                    comboSource.Add(ports[count], ports[count]);
                    count++;
                }

                portsCombo.DataSource = new BindingSource(comboSource, null);
                portsCombo.DisplayMember = "Key";
                portsCombo.ValueMember = "Value";
                portsCombo.SelectedIndex = count - 1;
            }
            else
            {
                UserInterface.logConsole("No ports available");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            accuracyTime.Series["Accuracy"].Points.Clear();// очицаем график
            Calibration.iterationNum = 0;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            setUserVariables(); // копируем значения из формы в UserVariables

            setButtonStopEnable();

            EEPROMFunctions.readEEPROM();

            GCode.checkHeights = false;
            HeightFunctions.getzMaxLength = false; // измерить высоту
            Calibration.calibrationState = true; // вкл. калибровку
            GCode.positionFlowHeight();// запускаем последовательность измерение высоты, а затем замер по координатам
            UserInterface.logConsole("\nStart Calibration");
            UserInterface.logConsole("Plate Diameter: " + UserVariables.plateDiameter + " mm");
            UserInterface.logConsole("Height-Map Accuracy: " + UserVariables.accuracy + " mm");
        }

        private void HAText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = HAText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void HBText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = HBText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void HCText_Validating(object sender, CancelEventArgs e)
        {
            double out1;
            TextBox textbox = HCText;

            string newChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string oldChar = (newChar == ",") ? "." : ",";
            textbox.Text = textbox.Text.Replace(oldChar, newChar);

            if (!double.TryParse(textbox.Text, out out1))
            {
                textbox.ForeColor = Color.Red;
                e.Cancel = true;

            }
            else
                textbox.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void comboBoxTypeCalibration_SelectedIndexChanged(object sender, EventArgs e)
        {
            UserVariables.typeCalibration = comboBoxTypeCalibration.Text;

            if (checkBoxDiagonalRod.Checked) { checkBoxTower.Checked = true; checkBoxHRad.Checked = true; checkBoxAlpha.Checked = true; }
            else if (checkBoxAlpha.Checked) { checkBoxTower.Checked = true; checkBoxHRad.Checked = true; }
            else if (checkBoxHRad.Checked) { checkBoxTower.Checked = true; }

            if(UserVariables.typeCalibration == "Escher") {
                checkBoxDeltaRad.Enabled = false;
                checkBoxDeltaRad.Checked = false;
            }
            else {
                checkBoxDeltaRad.Enabled = true;
            }
        }

        private void checkBoxTower_CheckedChanged(object sender, EventArgs e)
        {
            if (UserVariables.typeCalibration != "Escher") return;
            if (!checkBoxTower.Checked)
            {
                checkBoxHRad.Checked = false;
                checkBoxAlpha.Checked = false;
            }
        }

        private void checkBoxHRad_CheckedChanged(object sender, EventArgs e)
        {
            if (UserVariables.typeCalibration != "Escher") return;
            if (checkBoxHRad.Checked)
            {
                checkBoxTower.Checked = true;
            }
            else 
            {
                checkBoxAlpha.Checked = false;
            }
        }
        
        private void checkBoxAlpha_CheckedChanged(object sender, EventArgs e)
        {
            if (UserVariables.typeCalibration != "Escher") return;
            if (checkBoxAlpha.Checked)
            {
                checkBoxTower.Checked = true;
                checkBoxHRad.Checked = true;
            }
            else
            {
                checkBoxDiagonalRod.Checked = false;
            }
        }
        private void checkBoxDiagonalRod_CheckedChanged(object sender, EventArgs e)
        {
            if (UserVariables.typeCalibration != "Escher") return;
            if (checkBoxDiagonalRod.Checked)
            {
                checkBoxTower.Checked = true;
                checkBoxHRad.Checked = true;
                checkBoxAlpha.Checked = true;
            }
        }
        private void checkBoxDeltaRad_CheckedChanged(object sender, EventArgs e)
        {
            
        }
        private void AnT_Load(object sender, EventArgs e)
        {
            // Инициализация глут
            Glut.glutInit();
            Glut.glutInitDisplayMode(Glut.GLUT_RGB | Glut.GLUT_DOUBLE | Glut.GLUT_DEPTH);

            // отчистка окна 
            Gl.glClearColor(255, 255, 255, 1);

            // установка порта вывода в соответствии с размерами элемента anT 
            Gl.glViewport(0, 0, AnT.Width, AnT.Height);


            // настройка проекции 
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            Glu.gluPerspective(45d, (double)AnT.Width/(double)AnT.Height, 0.1d, 1000d);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();

            // настройка параметров OpenGL для визуализации 
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            // очистка окна
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ///////////////////
            double maxRadius = 220 / 2;
            uint turnCount = 5;
            uint pointsPerTurn = 10;
            double turn = Math.PI / (turnCount + 1);
            double interval = 2.0 * maxRadius / (pointsPerTurn - 1);
            var list = new List<PointError>() { new PointError(0, 0, 0) };
            Random rand = new Random();
            for (int i = 0; i < turnCount; i++)
            {
                var angle = i * turn;
                for (int j = 0; j < pointsPerTurn; j++)
                {
                    var rad = -1.0 * maxRadius + (interval * j);
                    list.Add(new PointError(Math.Sin(angle) * rad, Math.Cos(angle) * rad, rand.NextDouble()));
                }
            }

            /////////////////////////////

            if (list.Count == 0) return;
            double maxX = Math.Abs(list.Max(point => point.X));
            double maxY = Math.Abs(list.Max(point => point.Y));
            double maxZ = Math.Abs(list.Max(point => point.ZError) - list[0].ZError);
            double max = maxX > maxY ? maxX : maxY;


            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

            Gl.glLoadIdentity();
            Gl.glColor3f(0, 0, 0);

            Gl.glPushMatrix();
            Gl.glTranslated(0, 0, -3 * max);
            //Gl.glScaled(1, 10, 1);
            
            // 2 поворота
            //Gl.glRotated(135, 0, 0, 1);
            Gl.glRotated(30, 1, 0, 0);
            Gl.glRotated(30, 0, 1, 0);
 

            Gl.glPointSize(3.0f); // размер точек


            Gl.glPushMatrix();
            // Рисуем линии координат
            Gl.glColor3f(0, 0, 255);
            Gl.glBegin(Gl.GL_LINE_LOOP);
                Gl.glVertex3d(0.0, 0.0, 0.0);
                Gl.glVertex3d((double)AnT.Width, 0.0, 0.0);
            Gl.glEnd();

            Gl.glColor3f(0, 255, 0);
            Gl.glBegin(Gl.GL_LINE_LOOP);
                Gl.glVertex3d(0.0, 0.0, 0.0);
                Gl.glVertex3d(0.0, (double)AnT.Height, 0.0);
            Gl.glEnd();

            Gl.glColor3f(255, 0, 0);
            Gl.glBegin(Gl.GL_LINE_LOOP);
                Gl.glVertex3d(0.0, 0.0, 0.0);
                Gl.glVertex3d(0.0, 0.0, (double)AnT.Width);
            Gl.glEnd();

            

            // Рисуем плоскость стола
            Gl.glColor3f(0, 0, 0);
            Gl.glBegin(Gl.GL_POINTS);
            Gl.glVertex3d(list[0].X, list[0].ZError - list[0].ZError, list[0].Y);
            Gl.glEnd();
            int start = 1;
            int end = list.Count - 1;
            int div = 9;
            //Gl.glBegin(Gl.GL_POINTS);
            for (int i = start; i <= end; i++)
            {
                if (i % (div + 2) == 0 || i == start)
                    Gl.glBegin(Gl.GL_LINE_LOOP);

                Gl.glVertex3d(list[i].X, list[i].ZError - list[0].ZError, list[i].Y);

                if (i % (div + 1) == 0 && i != start || (i % div != 0 && i == end)) 
                    Gl.glEnd();
            }
            //Gl.glEnd();
            //
            // рисуем сферу с помощью библиотеки FreeGLUT 
            //Glut.glutWireSphere(2, 32, 32);
            Gl.glPopMatrix();
            Gl.glPopMatrix();

            Gl.glFlush();
            AnT.Invalidate();
        }

        

        

        


    }
}
