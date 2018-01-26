using SharpRaven.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace ServiceVerifier
{
    public class Log
    {

        private void CreateFile(string fileNew)
        {
            if (!File.Exists(fileNew))
            {
                FileStream file = File.Create(fileNew);
                file.Close();
            }
        }

        private void CreateDirectory(string exePath)
        {
            if (!Directory.Exists(exePath))
            {
                Directory.CreateDirectory(exePath);
            }
        }

        public void LogServiceWrite(string message, string level)
        {
            string fileNew = "C:\\LogSAP\\Recovery.log";

            try
            {
                CreateDirectory("C:\\LogSAP");
                CreateFile(fileNew);

                using (StreamWriter w = File.AppendText(fileNew))
                    AppendLogServicce(message, w, level);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Program.ravenClient.Capture(new SentryEvent(ex));
            }
        }

        private void AppendLogServicce(object logMessage, StreamWriter w, string level)
        {
            DateTime oDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            w.WriteLine("{0} {1} {2} {3}", oDate.ToLongTimeString(), oDate.ToLongDateString(), "[" + level + "]", logMessage);
        }
    }
}
