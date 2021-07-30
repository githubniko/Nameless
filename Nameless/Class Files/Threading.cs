using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;
using System.IO.Ports;

namespace Nameless.Class_Files
{
    static class Threading
    {
        /// <summary>
        /// Буфер чтения
        /// </summary>
        private static string _read = "";
        /// <summary>
        /// Результат обработки буфера чтения
        /// </summary>
        static List<string> readLineData = new List<string>();
        /// <summary>
        /// Очищает буфер приема 
        /// </summary>
        public static void ClearReadBuffer()
        {
            _read = "";
            readLineData.Clear();
        }

        public static void Read(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;

                if (!Connection.IsConnect()) return;
                _read += sp.ReadExisting();

                int lastIndexOf = _read.LastIndexOf(Connection._serialPort.NewLine.ToString());
                if (lastIndexOf > 0)
                {
                    lastIndexOf += Connection._serialPort.NewLine.Length;
                    string[] _split = _read.Substring(0, lastIndexOf).Split(Connection._serialPort.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    if (_read.Length > lastIndexOf)
                        _read = _read.Substring(lastIndexOf);
                    else
                        _read = "";

                    foreach (string message in _split)
                    {
                        lock (readLineData)
                        {
                            readLineData.Add(message);
                        }

                        if (message.Length > 1 && message.Substring(0, 2) == "ok")
                            lock (Connection.сurrentCommand) { Connection.сurrentCommand = ""; }// отчищаем буффер, что сообщает о возможности отправить следующую команду
                    }
                }

                    
            }
            catch (Exception e1) {
                Console.Write(e1.ToString());
            }

        }
       
        public static void Write()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en-US");

            while (Connection.IsConnect())
            {
                try
                {
                    if (Connection.ListCommand.Count > 0)
                    {
                        if (Connection.сurrentCommand.Length == 0) // если буфере пуст, то загружаем следующую команду
                        {
                            if (Connection.ListCommand.First() == null) { continue; }
                            Connection.сurrentCommand = Connection.ListCommand.First(); // Извлекаем первую команду из стека
                            lock (Connection.ListCommand) Connection.ListCommand.RemoveAt(0); // удаляем её из списка команд
                            Connection.WriteCOM(Connection.сurrentCommand);
                        }
                    }
                }
                catch (TimeoutException) { }
            }//end while
        }//end void

        public static void HandleRead()
        {
            while (Connection.IsConnect())
            {
                try
                {
                    while (Connection.IsConnect() && readLineData.Count > 0)
                    {
                        lock (readLineData)
                        {
                            DecisionHandler.handleInput2(readLineData.First()); // обработка значений, полученных от принтера
                            if (readLineData.Count > 0) readLineData.RemoveAt(0);
                        }
                    }//end while
                }
                catch (Exception e) { 
                    UserInterface.logConsole(e.Message); 
                }
            }//end while
        }//end void

        
    }
}
