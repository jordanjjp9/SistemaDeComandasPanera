using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CapaPresentacion.Impresion
{
    public class EscPosPrinter
    {
        public static void Print(string printerName, string ticketText, bool cut = true, bool openDrawer = false)
        {
            var init = new byte[] { 0x1B, 0x40 };               // ESC @
            var cutCmd = new byte[] { 0x1D, 0x56, 0x42, 0x00 };   // GS V 66 0
            var drawer = new byte[] { 0x1B, 0x70, 0x00, 0x40, 0xFF };

            using (var ms = new MemoryStream())
            {
                ms.Write(init, 0, init.Length);
                var bytes = Encoding.GetEncoding(850).GetBytes((ticketText ?? "").Replace("\n", "\r\n"));
                ms.Write(bytes, 0, bytes.Length);
                if (openDrawer) ms.Write(drawer, 0, drawer.Length);
                if (cut) ms.Write(cutCmd, 0, cutCmd.Length);

                RawPrinterHelper.SendBytesToPrinter(printerName, ms.ToArray());
            }
        }

        private static class RawPrinterHelper
        {
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public class DOCINFOA { public string pDocName; public string pOutputFile; public string pDataType; }

            [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
            static extern bool OpenPrinter(string src, out IntPtr hPrinter, IntPtr pd);
            [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
            static extern bool ClosePrinter(IntPtr hPrinter);
            [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
            static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);
            [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
            static extern bool EndDocPrinter(IntPtr hPrinter);
            [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
            static extern bool StartPagePrinter(IntPtr hPrinter);
            [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
            static extern bool EndPagePrinter(IntPtr hPrinter);
            [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
            static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

            public static void SendBytesToPrinter(string printerName, byte[] bytes)
            {
                IntPtr h;
                if (!OpenPrinter(printerName, out h, IntPtr.Zero))
                    throw new IOException("No se pudo abrir la impresora: " + printerName);

                try
                {
                    var di = new DOCINFOA { pDocName = "COMANDA", pDataType = "RAW" };
                    if (!StartDocPrinter(h, 1, di)) throw new IOException("StartDocPrinter falló");
                    if (!StartPagePrinter(h)) throw new IOException("StartPagePrinter falló");

                    IntPtr p = Marshal.AllocCoTaskMem(bytes.Length);
                    try
                    {
                        Marshal.Copy(bytes, 0, p, bytes.Length);
                        int written;
                        if (!WritePrinter(h, p, bytes.Length, out written) || written != bytes.Length)
                            throw new IOException("WritePrinter falló");
                    }
                    finally { Marshal.FreeCoTaskMem(p); }

                    EndPagePrinter(h);
                    EndDocPrinter(h);
                }
                finally { ClosePrinter(h); }
            }
        }
    }
}
