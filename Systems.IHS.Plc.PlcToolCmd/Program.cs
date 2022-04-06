using System;
using System.Net;
using System.Threading;
using S7.Net;

namespace Systems.IHS.Plc.PlcToolCmd
{
    class Program : Helper
    {

        static void Main(string[] args)
        {

            var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            //Console.WriteLine(environment);


            var fn = Helper.CheckFilePath(args);

            string file = System.IO.File.ReadAllText(fn);
            var text = file.Split("\r\n");
            var ip = text[0]; // parse ip address
            var cmd_i = 1; // commands line position
            var many_loops = false;
            var loop = true; // true for the fist time then depends on many_loop flag
            if (text[1].ToLower() == "loop")
            {
                many_loops = true;
                cmd_i = 2;
            }

            // parse commands 
            string[] commands = text[cmd_i..];

            // create PLC
            var ip_ok = IPAddress.TryParse(ip, out var _);
            if (ip_ok == true)
            {
                Helper.PLC = new S7.Net.Plc(CpuType.S71500, ip, 0, 0);
                Helper.PLC.Open();
            }
            else
            {
                Console.WriteLine($"Provided IP address '{ip}' is not valid !");
                return;
            }

            // Parse and execute commands line by line
            while (loop)
            {
                // reset loop flag if it is one time runner
                loop = many_loops == true ? true : false;

                for (int i = 0; i < commands.Length; i++)
                {
                    Helper.ParseLine(commands[i]);
                    Thread.Sleep(10);
                }
                Console.WriteLine("### loop finished");
            }

            PLC.Close();

            Console.WriteLine("Done");
            if (environment == "Development")
            {
                Console.ReadLine();
            }
        }

    }
    public class Helper
    {
        protected internal static S7.Net.Plc PLC { get; set; }

        private static int plc_db_index { get; set; }
        private static string field_type { get; set; }
        private static string plc_db_addr { get; set; }
        private static string plc_db_val { get; set; }

        public static string CheckFilePath(string[] args)
        {
            var fn = $"{AppDomain.CurrentDomain.BaseDirectory }../../../test1.plcm";
            if (args.Length > 0)
            {
                if (System.IO.File.Exists(args[0]))
                {
                    Console.WriteLine(args[0]);
                    fn = args[0];
                }
            }
            if (!System.IO.File.Exists(fn))
            {
                Console.WriteLine("Wrong File name...");
                Console.ReadLine();
            }
            return fn;
        }

        public static void ParseLine(string line)
        {
            Console.WriteLine(line);

            int wait_time = 0;
            var command_parts = line.Split(" ");


            if (line.ToLower().StartsWith("pause"))
            {
                // pause command
                wait_time = int.Parse(command_parts[1]);
                Thread.Sleep(wait_time);
            }
            else if (line.ToLower().StartsWith("cmd2"))
            {

            }
            else if (line.ToLower().StartsWith("db"))
            {

                //string memory_type = command_parts[0];
                plc_db_index = int.Parse(command_parts[1]);
                field_type = command_parts[2];
                plc_db_addr = command_parts[3];
                plc_db_val = command_parts[4];

                SendToPLCByValueType(DataType.DataBlock);



            }
            else if (line.ToLower().StartsWith("m") == true)
            {
                // no db index
                plc_db_index = 0;

                field_type = command_parts[2];
                plc_db_addr = command_parts[3];
                plc_db_val = command_parts[4];

                SendToPLCByValueType(DataType.Memory);

            }
        }

        private static void SendToPLCByValueType(DataType type)
        {
            // for byte and int
            var _val = int.Parse(plc_db_val);
            byte upperByte = (byte)(_val >> 8);
            byte lowerByte = (byte)(_val & 0xFF);

            switch (field_type)

            {

                case "b":
                    var _addr = plc_db_addr.Split("."); // split 2.4 as byte.bit
                    var byteAddr = int.Parse(_addr[0]);
                    var bitAddr = int.Parse(_addr[1]);
                    var val = plc_db_val == "1";
                    PLC.WriteBit(type, plc_db_index, byteAddr, bitAddr, val);

                    break;

                case "B":
                    var byteAddr2 = int.Parse(plc_db_addr);
                    PLC.WriteBytes(type, plc_db_index, byteAddr2, new byte[] { lowerByte });

                    break;

                case "I":
                    var byteAddr3 = int.Parse(plc_db_addr);
                    PLC.WriteBytes(type, plc_db_index, byteAddr3, new byte[] { upperByte, lowerByte });

                    break;

                default:
                    break;
            }
        }
    }

}
