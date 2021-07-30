using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using Nameless.Properties;

namespace Nameless.Class_Files
{
    static class Connection
    {
        public static SerialPort _serialPort;
        public static Thread writeThread, handleReadThread, checkComPortThread;
        //public static Thread calcThread;
        public static List<string> ListCommand = new List<string>();
        public static string сurrentCommand = ""; // текущая выполняемая комманда
        private static bool isConnected = false; // установлено ли соединение
        public static bool flagM110 = false; // флаг, ожидать ли ответ от команда M110
        

        public delegate void MethodContainer();
        /// <summary>
        /// Событие выполняется, если потеряно соединение с COM портом
        /// </summary>
        public static event MethodContainer onDisconnected;
        /// <summary>
        /// Событие выполняется после установления соединения с COM портом
        /// </summary>
        public static event MethodContainer onConnected;

        public static bool Connect()
        {
            try
            {
                // Opens a new thread if there has been a previous thread that has closed.
                writeThread = new Thread(Threading.Write);
                handleReadThread = new Thread(Threading.HandleRead);
                checkComPortThread = new Thread(CheckComPort);
                _serialPort = new SerialPort();
                flagM110 = false;
                isConnected = false;
                сurrentCommand = "";

                _serialPort.PortName = Program.mainFormTest.portsCombo.Text;
                _serialPort.BaudRate = int.Parse(Program.mainFormTest.baudRateCombo.Text, CultureInfo.InvariantCulture);
                _serialPort.DataBits = 8;
                _serialPort.StopBits = StopBits.One;
                _serialPort.Parity = Parity.None;
                _serialPort.Handshake = Handshake.None;
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(Threading.Read);
                _serialPort.NewLine = "\r\n";


                // Set the read/write timeouts.
                _serialPort.ReadTimeout = 500;
                _serialPort.WriteTimeout = 500;

                UserInterface.logConsole(Program.mainFormTest.portsCombo.Text);

                if (Program.mainFormTest.portsCombo.Text != "" && Program.mainFormTest.baudRateCombo.Text != "")
                {
                    if (!_serialPort.IsOpen)
                    {
                        Threading.ClearReadBuffer();
                        _serialPort.Open();
                        Reset(); // сброс пинтера
                    }

                    isConnected = true;
                    if (!writeThread.IsAlive) writeThread.Start(); 
                    if(!handleReadThread.IsAlive) handleReadThread.Start();
                    if (!checkComPortThread.IsAlive) checkComPortThread.Start();
                    
                    UserInterface.logConsole("Connected");
                    onConnected();
                }
                else
                {
                    UserInterface.logConsole("Please fill all text boxes above");
                }
            }
            catch (Exception e1)
            {
                UserInterface.logConsole(e1.Message);
                isConnected = false;
                if (writeThread.IsAlive)  writeThread.Abort();
                if (handleReadThread.IsAlive) handleReadThread.Abort();
                if (checkComPortThread.IsAlive) checkComPortThread.Abort();
                if (_serialPort.IsOpen) _serialPort.Close();
                _serialPort.Dispose();
                return false;
            }
            return true;
        }

        public static void Disconnect()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    isConnected = false;
                    //writeThread.Abort();
                    //handleReadThread.Abort();
                    //checkComPortThread.Abort();
                    if (_serialPort != null && _serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }
                }
                catch (Exception e1)
                {
                    UserInterface.logConsole(e1.Message);
                }
            }
            else
            {
                UserInterface.logConsole("Not Connected");
            }
        }
        /// <summary>
        /// Функция добавляет команду в последовательный буфер команд
        /// </summary>
        /// <param name="messageIn"></param>
        public static void WriteLine(string messageIn)
        {
            lock (ListCommand) { ListCommand.Add(messageIn); }
        }
        /// <summary>
        /// Функция отправляет на принтер g-cod
        /// </summary>
        /// <param name="messageIn"></param>
        public static void WriteCOM(string messageIn)
        {
            int len = 63; // длина буфера на устройстве
            if (messageIn.Length == 0)
            {
                //_serialPort.Write(_serialPort.NewLine);
                UserInterface.logConsole("messageIn.Length == 0");
                return;
            }

            char[] message = messageIn.ToCharArray();

            UserInterface.logPrinter(">> " + messageIn);
            for (int i = 0; i < message.Length; i++)
            {
                if (message.Length - i < len) len = message.Length;
                if (isConnected) _serialPort.Write(message, i, len);
                i += len;
            }
            if(isConnected) _serialPort.Write(_serialPort.NewLine);
        }

        /// <summary>
        /// Сбрасываем COM соединение
        /// </summary>
        public static void Reset()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                ClearBuffer();
                _serialPort.DtrEnable = false; _serialPort.DtrEnable = true; _serialPort.DtrEnable = false; // сброс пинтера
            }
        }
        /// <summary>
        /// Очищает буффер команд
        /// </summary>
        public static void ClearBuffer()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                ListCommand.Clear();
                сurrentCommand = "";
            }
        }
        /// <summary>
        /// Проверяет каждую секунду на существование открытого COM порта
        /// </summary>
        private static void CheckComPort() 
        {
            while (true)
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    isConnected = false;
                    //writeThread.Abort();
                    //handleReadThread.Abort();                    
                    onDisconnected();
                    return;
                }
                checkComPortThread.Join(1000);
            }
        }
        /// <summary>
        /// true если соединение установлено
        /// </summary>
        /// <returns></returns>
        public static bool IsConnect()
        {
            return isConnected;
        }
    }
}
