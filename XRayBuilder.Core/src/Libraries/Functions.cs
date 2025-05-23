﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace XRayBuilder.Core.Libraries
{
    public static class Functions
    {
        public static string ReadFromFile(string file)
        {
            using var streamReader = new StreamReader(file, Encoding.UTF8);
            return streamReader.ReadToEnd();
        }

        // TODO: Clean this up more cause it still sucks
        public static string Clean(this string str)
        {
            (string[] searches, string replace)[] replacements =
            {
                (new[] {"&#169;", "&amp;#169;", "&#174;", "&amp;#174;", "&mdash;", @"</?[a-z]+>", @"(<a.*?>.*?</a>)", "@", @"#(?!\d+)", @"\$", "%", "_", "<b></b>", @"\*" }, ""),
                (new[] { "&#133;", "&amp;#133;", @" \. \. \. ?", @"\.\.\." }, "…"),
                (new[] { @"\t|\n|\r|•", @"\s+", "<br><br>"}, " "),
                (new[] { "“", "”", "\"", "&quot;" }, "'"),
                (new[] { " - ", "--" }, "—"),
                (new[] { @"\. …$"}, ".")
            };
            foreach (var (s, r) in replacements)
            {
                str = Regex.Replace(str, $"({string.Join("|", s)})", r, RegexOptions.Multiline);
            }

            return HtmlDecode(str).Trim();
        }

        /// <summary>
        /// Decode hex HTML entities.
        /// </summary>
        public static string HtmlDecode(string input)
        {
            var regex = new Regex(@"( ?&x(\d{4}); ?)");
            var matches = regex.Matches(input);
            var sb = new StringBuilder (input);

            foreach (Match m in matches)
            {
                sb.Replace(m.Groups[0].Value, $"&#x{m.Groups[2].Value};".Replace(" ", string.Empty));
            }

            return WebUtility.HtmlDecode(sb.ToString());
        }

        public static string GetBookOutputDirectory(string author, string title, bool create, string baseOutputDir)
        {
            var newAuthor = RemoveInvalidFileChars(author);
            var newTitle = RemoveInvalidFileChars(title);
            var path = Path.Combine(baseOutputDir, $"{newAuthor}\\{newTitle}");
            if (create)
                Directory.CreateDirectory(path);
            return path;
        }

        public static string RemoveInvalidFileChars(string filename)
        {
            var fileChars = Path.GetInvalidFileNameChars();
            return new string(filename.Where(x => !fileChars.Contains(x)).ToArray());
        }

        //public static string GetTempDirectory()
        //{
        //    string path;
        //    do
        //    {
        //        path = Path.Combine(Properties.Settings.Default.tmpDir, Path.GetRandomFileName());
        //    } while (Directory.Exists(path));
        //    Directory.CreateDirectory(path);
        //    return path;
        //}

        public static string TimeStamp()
        {
            var assemblyName = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName();
            return $"Running {assemblyName.Name} v{assemblyName.Version}. Log started on {DateTime.Now:dd/MM/yyyy} at {DateTime.Now:HH:mm:ss}.\r\n";
        }

        public static void RunNotepad(string filename)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "notepad",
                Arguments = filename,
                UseShellExecute = false
            };
            try
            {
                using var process = Process.Start(startInfo);
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                throw new Exception("Error trying to launch notepad.", ex);
            }
        }

        /// <summary>
        /// Fix author name if in last, first format or if multiple authors present (returns first author)
        /// </summary>
        public static string FixAuthor(string author)
        {
            if (author == null) return null;
            if (author.Contains(';'))
                author = author.Split(';')[0];
            if (author.Contains(','))
            {
                var parts = author.Split(',');
                author = $"{parts[1].Trim()} {parts[0].Trim()}";
            }
            return author;
        }

        /// <summary>
        /// Convert non-ascii characters into the \uXXXX hexadecimal format
        /// </summary>
        public static string ExpandUnicode(string input)
        {
            var output = new StringBuilder(input.Length);
            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] > 127)
                {
                    var uniBytes = Encoding.Unicode.GetBytes(input.Substring(i, 1));
                    output.AppendFormat(@"\u{0:X2}{1:X2}", uniBytes[1], uniBytes[0]);
                }
                else
                    output.Append(input[i]);
            }
            return output.ToString();
        }

        /// <summary>
        /// Process GUID. If in decimal form, convert to hex.
        /// </summary>
        [CanBeNull]
        public static string ConvertGuid(string guid)
        {
            if (guid == null)
                return null;

            if (Regex.IsMatch(guid, "/[a-zA-Z]/", RegexOptions.Compiled))
                guid = guid.ToUpper();
            else
            {
                long.TryParse(guid, out var guidDec);
                guid = guidDec.ToString("X");
            }

            if (guid == "0")
                throw new ArgumentException("An error occurred while converting the GUID.");

            return guid;
        }

        public static bool ValidateFilename(string author, string title)
        {
            var newAuthor = RemoveInvalidFileChars(author);
            var newTitle = RemoveInvalidFileChars(title);
            return author.Equals(newAuthor) && title.Equals(newTitle);
        }

        public static long UnixTimestampSeconds()
        {
            return (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public static long UnixTimestampMilliseconds()
        {
            return (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        public static void ShellExecute(string path)
        {
            // Assumes the input `path` has been validated before calling this method.
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
        }
    }

    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}