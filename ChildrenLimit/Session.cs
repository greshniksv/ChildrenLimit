using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChildrenLimit
{
    public class Session
    {
        private readonly string sessionPath;
        private readonly string startSessionPath;

        public string AppDataPath { get; set; }

        public Session()
        {
            AppDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            sessionPath = Path.Combine(AppDataPath, "sessions.dat");
            startSessionPath = Path.Combine(AppDataPath, "startSessions.dat");

            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }

            if (!File.Exists(sessionPath))
            {
                using (File.Create(sessionPath)) { }
            }

            if (!File.Exists(startSessionPath))
            {
                using (File.Create(startSessionPath)) { }
            }
        }

        public void SaveStartSession()
        {
            using (TextWriter writer = new StreamWriter(startSessionPath, false, Encoding.UTF8))
            {
                writer.WriteLine(((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds());
            }
        }

        public void SaveSession()
        {
            using (TextWriter writer = new StreamWriter(sessionPath, false, Encoding.UTF8))
            {
                writer.WriteLine(((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds());
            }
        }

        public IEnumerable<DateTime> LoadStartSessions()
        {
            using (TextReader reader = new StreamReader(startSessionPath, Encoding.UTF8))
            {
                string buf;
                while ((buf = reader.ReadLine()) != null)
                {
                    if (long.TryParse(buf, out long data))
                    {
                        yield return UnixTimeStampToDateTime(data);
                    }
                }
            }

        }

        public IEnumerable<DateTime> LoadSessions()
        {
            using (TextReader reader = new StreamReader(sessionPath, Encoding.UTF8))
            {
                string buf;
                while ((buf = reader.ReadLine())!= null)
                {
                    if (long.TryParse(buf, out long data))
                    {
                        yield return UnixTimeStampToDateTime(data);
                    }
                }
            }
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}
