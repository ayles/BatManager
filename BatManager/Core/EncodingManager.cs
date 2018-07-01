using System;
using System.Diagnostics;
using System.Text;

namespace BatManager.Core {
    public static class EncodingManager {
        private static Encoding _encodingToUse;
        public static Encoding EncodingToUse {
            get {
                if (_encodingToUse == null) {
                    try {
                        var process = Process.Start(new ProcessStartInfo() {
                            FileName = "cmd",
                            Arguments = "/c chcp",
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        var enc = process.StandardOutput.ReadToEnd().Trim().Split(' ');
                        _encodingToUse = Encoding.GetEncoding(int.Parse(enc[enc.Length - 1]));
                    } catch (Exception e) {
                        Console.WriteLine(e);
                        try {
                            _encodingToUse = Console.OutputEncoding;
                        } catch (Exception e2) {
                            Console.WriteLine(e2);
                            _encodingToUse = Encoding.Default;
                        }
                    }
                }

                return _encodingToUse;
            }
        }
    }
}