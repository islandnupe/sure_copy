using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;

namespace Support
{
    public class FileSystemLogger
    {
        #region Instance vars

        static StreamWriter LogOutfile;
        static FileStream LogOutfilefs;
        static string m_stringLogPath;
        static string m_stringLogFileName;
        static string m_stringActualLogFile = string.Empty;
        static long m_intFileLength = 0;
        static string m_stringApplicationTitle = string.Empty;
        static TimeSpan m_timeSpanDeleteOlderThan = new TimeSpan(180, 0, 0, 0);
        #endregion

        #region Properties

        /// <summary>
        /// For Application Title
        /// </summary>
        public static string ApplicationTitle
        {
            get { return m_stringApplicationTitle; }
            set { m_stringApplicationTitle = value; }
        }


        /// <summary>
        /// For Path
        /// </summary>
        public static string LogFilePath
        {
            get { return m_stringLogPath; }
            set { m_stringLogPath = value; }
        }

        /// <summary>
        /// For LogFile Name
        /// </summary>
        public static string LogFileName
        {
            get { return m_stringLogFileName; }
            set { m_stringLogFileName = value; }
        }

        /// <summary>
        /// For Actual Log File Name
        /// </summary>
        public static string ActualLogFile
        {
            get { return m_stringActualLogFile; }
        }

        /// <summary>
        /// For Actual Log File Length
        /// </summary>
        public static long LogFileLength
        {
            get { return m_intFileLength; }
        }

        /// <summary>
        /// For Deleting Older Than
        /// </summary>
        public static TimeSpan DeleteOlderThan
        {
            get { return m_timeSpanDeleteOlderThan; }
            set { m_timeSpanDeleteOlderThan = value; }
        }

        #endregion

        //<summary>
        //Logs debug output to text file
        //</summary>
        //<param name="txt">Info to Log</param>
        public static void LogInformation(string txt)
        {
            try
            {
                if (LogOutfile == null)
                {
                    CreateDebugLogFile();
                }
                //DateTime dtNow = DateTime.Now;
                LogOutfile.WriteLine(txt);
                LogOutfile.Flush();

                FileInfo fileInformation = new FileInfo(m_stringActualLogFile);
                m_intFileLength = fileInformation.Length;
            }
            catch (Exception ex)
            {
                throw ex;
                //MessageBox.Show(ex.Message);
            }

        }

        public static void LogInformationDaily(string stringLogText)
        {
            try
            {
                if (Directory.Exists(m_stringLogPath) != true)
                    System.IO.Directory.CreateDirectory(m_stringLogPath);

                DateTime dateTimeNow = DateTime.Now;
                string stringDateTime;
                stringDateTime = dateTimeNow.ToString("\n[MM-dd-yy hh:mm:ss tt] ");

                DateTime dtNow = DateTime.Now;
                string fileout = string.Format("{0}_{1}-{2}-{3}.txt", m_stringApplicationTitle, dtNow.Year.ToString(), dtNow.Month.ToString("00"), dtNow.Day.ToString("00"));
                fileout = Path.Combine(m_stringLogPath, fileout);
                if (File.Exists(fileout) != true)
                {
                    dtNow = DateTime.Now;
                    fileout = string.Format("{0}_{1}-{2}-{3}.txt", m_stringApplicationTitle, dtNow.Year.ToString(), dtNow.Month.ToString("00"), dtNow.Day.ToString("00"));
                    fileout = Path.Combine(m_stringLogPath, fileout);
                }

                m_stringActualLogFile = fileout;

                using (FileStream LogOutfilefs = new FileStream(fileout,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite))
                {
                    StreamWriter LogOutfile = new StreamWriter(LogOutfilefs);

                    LogOutfile.WriteLine(stringDateTime + stringLogText);
                    LogOutfile.Flush();
                }
            }
            catch (Exception Ex)
            {
                throw new Exception("ERROR  " + Ex.ToString());
            }
        }

        //<summary>
        //Logs debug output to text file
        //</summary>
        //<param name="txt">Info to Log</param>
        public static void LogInformationSameLine(string txt)
        {
            try
            {
                if (LogOutfile == null)
                {
                    CreateDebugLogFile();
                }
                //DateTime dtNow = DateTime.Now;
                LogOutfile.Write(txt);
                LogOutfile.Flush();

                FileInfo fileInformation = new FileInfo(m_stringActualLogFile);
                m_intFileLength = fileInformation.Length;
            }
            catch (Exception ex)
            {
                throw ex;
                //MessageBox.Show(ex.Message);
            }

        }

        //<summary>
        //Logs debug output to text file
        //</summary>
        //<param name="txt">Info to Log</param>
        public static void CreateNewDayLogFile()
        {
            try
            {
                if (LogOutfile != null)
                {
                    LogOutfile.Flush();

                    LogOutfile.Close();
                    LogOutfilefs.Close();

                    LogOutfile = null;
                    LogOutfilefs = null;
                }

                CreateDebugLogFile();
                //CleanUpOldLogFiles();

                FileInfo fileInformation = new FileInfo(m_stringActualLogFile);
                m_intFileLength = fileInformation.Length;
            }
            catch (Exception ex)
            {
                throw ex;
                //MessageBox.Show(ex.Message);
            }

        }

        //<summary>
        //Cleans up old log files
        //</summary>
        public static void CleanUpOldLogFiles()
        {
            if (m_timeSpanDeleteOlderThan == null)
                return;
            try
            {
                string[] fileEntries = Directory.GetFiles(m_stringLogPath, m_stringLogFileName + "*.txt");
                DateTime dateTimeNow = DateTime.Now;
                DateTime dateTimeOlderThan = dateTimeNow - m_timeSpanDeleteOlderThan;
                LogInformationDaily(dateTimeNow.ToString("[MM-dd-yy hh:mm:ss tt]") + " Deleting Log Files of form [" + m_stringLogFileName + "*.txt] created or modified before " + dateTimeOlderThan.ToShortDateString());
                foreach (string fileName in fileEntries)
                {
                    DateTime dateTimeCreatedTime = File.GetCreationTime(fileName);
                    DateTime dateTimeModifiedTime = File.GetLastWriteTime(fileName);
                    if ((dateTimeNow - dateTimeCreatedTime > m_timeSpanDeleteOlderThan) || (dateTimeNow - dateTimeModifiedTime > m_timeSpanDeleteOlderThan))
                    {
                        File.Delete(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                LogInformation(DateTime.Now.ToString("[MM-dd-yy hh:mm:ss tt]") + " CleanUpOldLogFiles() ERROR Cleaning up Log Files:: " + ex.Message);
            }
        }

        /// <summary>
        /// Creates debug output file
        /// </summary>
        public static void CreateDebugLogFile()
        {
            try
            {
                //string path = System.Web.HttpContext.Current.Server.MapPath("LogOutput");
                string path = LogFilePath;// @"C:\Study_Manager_Logs\";//tbLogFilePath.Text;

                System.IO.Directory.CreateDirectory(path);
                string logFilePrefix = m_stringLogFileName; // tbLogFilePrefix.Text;
                DateTime dtNow = DateTime.Now;
                string fileout = string.Format("{0}_{1}-{2}-{3}_{4}-{5}-{6}.txt", logFilePrefix, dtNow.Year.ToString(), dtNow.Month.ToString("00"), dtNow.Day.ToString("00"), dtNow.Hour.ToString("00"), dtNow.Minute.ToString("00"), dtNow.Second.ToString("00"));
                if (File.Exists(fileout) == true)
                {
                    dtNow = DateTime.Now;
                    fileout = string.Format("{0}_{1}-{2}-{3}_{4}-{5}-{6}.txt", logFilePrefix, dtNow.Year.ToString(), dtNow.Month.ToString("00"), dtNow.Day.ToString("00"), dtNow.Hour.ToString("00"), dtNow.Minute.ToString("00"), dtNow.Second.ToString("00"));
                }


                fileout = Path.Combine(path, fileout);
                LogOutfilefs = new FileStream(fileout,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite);
                LogOutfile = new StreamWriter(LogOutfilefs);
                m_stringActualLogFile = fileout;
            }
            catch (Exception ex)
            {
                throw ex;
                //MessageBox.Show(ex.Message);
            }
        }
    }
}
