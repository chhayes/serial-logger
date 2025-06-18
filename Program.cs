using CommandLine;
using System;
using System.IO;
using System.IO.Ports;
using System.Reflection;

namespace SimpleSerialLogger
{
    public enum FileModeOption
    {
        One,
        Hourly,
        Daily
    }
    class Options
    {
        [Option('p', "port", Required = false, HelpText = "Serial port name (e.g. COM3).")]
        public string Port { get; set; }

        [Option('b', "baud", Required = false, HelpText = "Baud rate (e.g. 9600, 19200).")]
        public int? BaudRate { get; set; }
        [Option("prefix", Required = false, HelpText = "String to append to data file names.")]
        public string Prefix { get; set; }

        [Option('m', "mode", Required = false, Default = FileModeOption.One, HelpText = "File mode: One(def), Hourly, or Daily.")]
        public FileModeOption Mode { get; set; }
    }
    class SerialPortReader
    {
        static void Main(string[] args)
        {
            // Print version info
            Console.WriteLine($"SimpleSerialLogger v{Assembly.GetExecutingAssembly().GetName().Version}\n");

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunWithOptions)
                .WithNotParsed(errors => Environment.Exit(1));
        }
        static void RunWithOptions(Options opts)
        {
            string portName = opts.Port;
            int baudRate = opts.BaudRate ?? 0;
            string prefix = opts.Prefix ?? "serialData";
            FileModeOption mode = opts.Mode;

            if (string.IsNullOrEmpty(portName))
            {
                Console.WriteLine("Enter the serial port number (e.g. COM2):");
                portName = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(portName))
            {
                Console.WriteLine("Invalid port name. Exiting...");
                return;
            }

            if (baudRate <= 0)
            {
                Console.WriteLine("Enter the baud rate (e.g., 9600, 19200):");
                if (!int.TryParse(Console.ReadLine(), out baudRate) || baudRate <= 0)
                {
                    Console.WriteLine("Invalid baud rate. Exiting...");
                    return;
                }
            }

            SerialPort serialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = baudRate,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            var startTime = DateTime.Now;

            string GetFilename()
            {
                var filenameTime = mode switch
                {
                    FileModeOption.Hourly => TruncateToHour(DateTime.Now) == TruncateToHour(startTime) ? startTime : TruncateToHour(DateTime.Now),
                    FileModeOption.Daily => TruncateToDay(DateTime.Now) == TruncateToDay(startTime) ? startTime : TruncateToDay(DateTime.Now),
                    _ => startTime
                };
                return $"{prefix}_{filenameTime:yyMMddHHmmss}.txt";
            }
            string fileName = "";

            try
            {
                serialPort.DataReceived += (sender, eventArgs) =>
                {
                    try
                    {
                        string data = serialPort.ReadLine();
                        string timestampedData = $"{DateTime.Now:yy-MM-dd HH:mm:ss},{data}";
                        if(fileName != GetFilename())
                        {
                            fileName = GetFilename();
                            Console.WriteLine($"Starting new file: {fileName}");
                        }
                        File.AppendAllText(fileName, timestampedData + Environment.NewLine);
                        Console.WriteLine($"Received: {timestampedData}");
                    }
                    catch (TimeoutException) 
                    {
                        Console.WriteLine($"Error: serial port timeout");
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
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open the serial port: {ex.Message}");
            }
            finally
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
                Console.WriteLine("Serial port closed. Exiting...");
            }
        }
        static DateTime TruncateToHour(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
        }
        static DateTime TruncateToDay(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
        }


    }

}
