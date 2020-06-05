using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace ADGBravoImport
{
    public static class LogWriter
    {
        private static string m_exePath = string.Empty;
        public static void Writer(string logMessage)
        {
            LogWrite(logMessage);
        }
        public static void LogWrite(string logMessage)
        {
            m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
                using (StreamWriter w = File.AppendText(m_exePath + "\\" + "log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt"))
                {
                    Log(logMessage, w);
                }            
        }

        public static void Log(string logMessage, TextWriter txtWriter)
        {            
                txtWriter.Write("Log Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                //txtWriter.WriteLine("  :");
                txtWriter.WriteLine("{0}", logMessage);
                txtWriter.WriteLine("=============================================");
            
        }
    }
}
