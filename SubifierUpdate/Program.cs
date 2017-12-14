using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace SubifierUpdate
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args /* SHOULD BE:  <install_location> <Subifier.exe process_id>  EXAMPLE:  "C:\\Program Files (x86)\\Azuru\\Subifier" "10987"  */)
        {
            try
            {
                WebClient wc = new WebClient();
                string temp_zip_file = Path.GetTempFileName() + ".Subifier_upd";
                wc.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                wc.DownloadFile("http://cdn.azuru.me/apps/subifier/latest.zip", temp_zip_file);

                ZipArchive ziparch = ZipFile.OpenRead(temp_zip_file);
                kill_Subifier(args[1]);
                File.Delete(args[0] + "\\Subifier.exe");
                Directory.Delete(args[0] + "\\ui", true);
                ziparch.ExtractToDirectory(args[0]);
                Process.Start(args[0] + "\\Subifier.exe", "updated \"" + Application.ExecutablePath + "\"");
                ziparch.Dispose();
                wc.Dispose();
                File.Delete(temp_zip_file);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating Subifier: " + ex.Message);
            }
        }

        private static void kill_Subifier(string pid)
        {
            Process p = Process.GetProcessById(Convert.ToInt32(pid));
            p.Kill();
            p.WaitForExit();
        }
    }
}
