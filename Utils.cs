﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WidevineClient
{
    class Utils
    {
        public static double EvaluateEquation(string equation, int decimals = 3)
        {
            var dataTable = new DataTable();
            return Math.Round((double)dataTable.Compute(equation, ""), decimals);
        }

        public static string RunCommand(string command, string args)
        {
            Process p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = command,
                    Arguments = args,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }

        public static int RunCommandCode(string command, string args)
        {
            Process p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    FileName = command,
                    Arguments = args
                }
            };

            p.Start();
            p.WaitForExit();
            return p.ExitCode;
        }

        public static byte[] Xor(byte[] a, byte[] b)
        {
            byte[] x = new byte[Math.Min(a.Length, b.Length)];

            for (int i = 0; i < x.Length; i++)
            {
                x[i] = (byte)(a[i] ^ b[i]);
            }

            return x;
        }

        public static string GenerateRandomId()
        {
            return BytesToHex(RandomBytes(3)).ToLower();
        }

        public static byte[] RandomBytes(int length)
        {
            var bytes = new byte[length];
            new Random().NextBytes(bytes);
            return bytes;
        }

        public static string[] GetElementsInnerTextByAttribute(string html, string element, string attribute)
        {
            List<string> content = new List<string>();

            foreach (string line in html.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (line.Contains("<" + element) && line.Contains(attribute))
                {
                    string contentPart = line.Substring(0, line.LastIndexOf("<"));
                    if (contentPart.EndsWith(">"))
                        contentPart = contentPart[..^1];

                    contentPart = contentPart[(contentPart.LastIndexOf(">") + 1)..];

                    if (contentPart.Contains("<"))
                        contentPart = contentPart[..contentPart.IndexOf("<")];

                    content.Add(contentPart);
                }
            }
            return content.ToArray();
        }

        public static string BytesToHex(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", "");
        }
        public static byte[] HexToBytes(string hex)
        {
            hex = hex.Trim();
            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        }

        public static bool IsBase64Encoded(string str)
        {
            try
            {
                byte[] data = Convert.FromBase64String(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string Base64Pad(string base64)
        {
            if (base64.Length % 4 != 0)
            {
                base64 = base64.PadRight(base64.Length + (4 - (base64.Length % 4)), '=');
            }
            return base64;
        }
        public static string Base64ToString(string base64)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
        public static string StringToBase64(string str)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }

        public static void TitleProgress(long read, long length)
        {
            long readMB = read / 1024 / 1024;
            long lengthMB = length / 1024 / 1024;
            Console.Title = $"{readMB}/{lengthMB}MB";
        }

        public static void TitleProgressNoConversion(long read, long length)
        {
            Console.Title = $"{read}/{length}MB";
        }

        public static string Version()
        {
            return System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString();
        }
    }
}