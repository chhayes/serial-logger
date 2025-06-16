using System;
using System.IO;
using System.IO.Ports;

namespace SimpleSerialLogger
{
    class SerialPortReader
    {
        static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  SerialPortReader.exe [-p PORT] [-b BAUD]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -p        Serial port name (e.g., COM3)");
            Console.WriteLine("  -b        Baud rate (e.g., 9600, 19200)");
            Console.WriteLine("  -h, -help, /help   Show this help message");
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the serial port number (e.g. COM2):");
            string portName = Console.ReadLine();

            if (string.IsNullOrEmpty(portName))
            {
                Console.WriteLine("Invalid port name. Exiting...");
                return;
            }

            Console.WriteLine("Enter the baud rate (e.g., 9600, 19200):");
            if (!int.TryParse(Console.ReadLine(), out int baudRate) || baudRate <= 0)
            {
                Console.WriteLine("Invalid baud rate. Exiting...");
                return;
            }

            SerialPort serialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = baudRate,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500, // Adjust as needed
                WriteTimeout = 500  // Adjust as needed
            };

            string fileName = $"serialdata_{DateTime.Now:yyMMddHHmmss}.txt";
            Console.WriteLine($"Data will be saved to: {fileName}");

            try
            {
                serialPort.DataReceived += (sender, eventArgs) =>
                {
                    try
                    {
                        string data = serialPort.ReadLine();
                        string timestampedData = $"{DateTime.Now:yy-MM-dd HH:mm:ss},{data}";
                        File.AppendAllText(fileName, timestampedData + Environment.NewLine);
                        Console.WriteLine($"Received: {timestampedData}");
                    }
                    catch (TimeoutException)
                    {
                        // Handle timeout exception if needed
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while processing data: {ex.Message}");
                    }
                };

                serialPort.Open();
                Console.WriteLine("Listening to the serial port. Press 'q' and Enter to quit.");

                while (true)
                {
                    string userInput = Console.ReadLine();
                    if (userInput.Equals("q", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open the serial port: {ex.Message}");
            }
            finally
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                Console.WriteLine("Serial port closed. Exiting...");
            }
            Console.ReadLine(); //Keep the program open if it throws an error
        }
    }

}
