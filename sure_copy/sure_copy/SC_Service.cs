using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Support;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Management;
using System.Data.SQLite;

namespace sure_copy
{
    #region Delegates
    //intError = 0, miscellaneous message
    //intError = 1, miscellaneous mesage - Always display
    //intError = 2, error 
    //intError = 3, error - Always display
    public delegate void DelegateWriteToLog(string stringLogText, LogMessageType MessageType);

    public delegate object DelegateWorkFunction(object objParameter);

    #endregion

    public enum LogMessageType
    {
        Miscellaneous, MiscellaneousAlwaysDisplay, HighPriorityAlwaysDisplay, Error, ErrorAlwaysDisplay
    }


    public partial class SC_Service : ServiceBase
    {
        #region Instance Variables

        Thread m_threadMainWorkerThread;
        public bool m_boolServiceRunning = false;
        
        string m_stringLocalHostName;
        string m_stringApplicationTitle = string.Empty;
        string m_stringVersionInfo = string.Empty;
        string m_stringLogFilePath = string.Empty;

        string m_stringSourcePath = string.Empty;
        string m_stringDestinationPath = string.Empty;

        string m_stringDatabaseFile = string.Empty;

        string m_stringSourcePathChangingTo = string.Empty;
        string m_stringDestinationPathChangingTo = string.Empty;
        string m_stringWindowsSystemDirectory = string.Empty;

        string m_stringCurrentFileBeingCopied = string.Empty;
        string m_stringElapsedTime = string.Empty;

        bool m_boolCheckMD5 = false;
        bool m_boolCheckLastWriteTime = false;

        TimeSpan m_timeSpanDeleteLogFilesOlderThan = new TimeSpan(30,0,0,0);
        TimeSpan m_timeSpanSleepLength = new TimeSpan(0, 0, 20);

        DateTime m_dateTimeOfDayToRun = new DateTime(2016, 1, 1, 0, 0, 0);
        DateTime m_dateToday;
        DateTime m_dateTimeLastRun;

        bool m_boolDetailedLogging = false;
        bool m_boolUSER_HALT_OPERATIONS = false;
        bool m_boolRunCompletedToday = false ;

        bool m_boolSourcePathChanged = false;
        bool m_boolDestinationPathChanged = false;
        bool m_boolAllowDestinationPathChangeViaHTTP = false;
        int m_intTotalCopyAttempts = 0;
        int m_intTotalCopyOpertionsNotNeeded = 0;
        int m_intSuccessfulCopyAttempts = 0;
        int m_intFailedCopyAttempts = 0;
        int m_intFailedMD5Checks = 0;
        int m_intLastWriteTimeChecks = 0;
        long m_longTotalBytesCopied = -1;

        int m_intBufferSizeMB = 1;

        HttpServer m_HttpServer;
        int m_intHttpPort = 8181;
        int m_intHttpPortChangeTo = 8181;
        public event DelegateWriteToLog m_eventWriteToLog;

        SQLiteConnection m_dbConnection;

        object lockObject = new object();

        #endregion

        public SC_Service()
        {
            InitializeComponent();
        }

        #region Service Start and Stop overrides

        protected override void OnStart(string[] args)
        {
            //Create the thread and set the work function
            ThreadStart threadStartObject = new ThreadStart(MainWorkFunction);
            m_threadMainWorkerThread = new Thread(threadStartObject);

            //start it up
            m_threadMainWorkerThread.Start();
            m_boolServiceRunning = true;
        }

        internal void OnStart_Debug(string[] values)
        {
            //Create the thread and set the work function
            ThreadStart threadStartObject = new ThreadStart(MainWorkFunction);
            m_threadMainWorkerThread = new Thread(threadStartObject);

            //start it up
            m_threadMainWorkerThread.Start();
            m_boolServiceRunning = true;        
        }

        protected override void OnStop()
        {
            StopWorkProcessing();
            m_boolServiceRunning = false;
            m_threadMainWorkerThread.Join(new TimeSpan(0, 0, 5)); //wait five seconds to shut down
        }

        #endregion

        #region Work Functions

        protected void MainWorkFunction()
        {
            try
            {
                m_stringWindowsSystemDirectory =  Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                LoadConfigurationSettings();
                SetupDelegates();
                
                Assembly asm = Assembly.GetExecutingAssembly();
                string stringVersion = asm.GetName().Version.ToString();
                AssemblyTitleAttribute TitleAttribute = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(asm, typeof(AssemblyTitleAttribute));
                m_stringApplicationTitle = TitleAttribute.Title;
#if (DEBUG)                
                m_stringApplicationTitle = TitleAttribute.Title+"Debug";
#endif

                m_stringVersionInfo = m_stringApplicationTitle + " v" + stringVersion;

                m_stringLocalHostName = Dns.GetHostName();
                                
                string stringMsg = string.Format("[{0}] Starting up on [{1}] at {2}", m_stringVersionInfo, m_stringLocalHostName, DateTime.Now.ToString());
                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

                /*
                                 stringMsg = string.Format("\r\tSourcePath [{0}]\r\n\r\t\t\tDestinationPath [{1}]\r\n\r\t\t\tLogFilePath [{2}]", m_stringSourcePath, m_stringDestinationPath, m_stringLogFilePath);
                                //m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                                stringMsg += string.Format("\r\n\r\t\t\tDetailedLogging [{0}]\r\n\r\t\t\tHALT_OPERATIONS [{1}]\r\n\r\t\t\tCheckMD5 [{2}]", m_boolDetailedLogging, m_boolUSER_HALT_OPERATIONS, m_boolCheckMD5);
                                stringMsg += string.Format("\r\n\r\t\t\tRunCompletedToday [{0}]\r\n\r\t\t\tAllowDestinationPathChangeViaHTTP [{1}]", m_boolRunCompletedToday, m_boolAllowDestinationPathChangeViaHTTP);
                                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                                stringMsg = string.Format("\r\tTime of Day to Run [{0}]", m_dateTimeOfDayToRun.ToShortTimeString());

                 */


                stringMsg = string.Format("\r\tSourcePath [{0}]\r\n\r\t\t\tDestinationPath [{1}]\r\n\r\t\t\tLogFilePath [{2}]", m_stringSourcePath, m_stringDestinationPath, m_stringLogFilePath);
                //m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                stringMsg += string.Format("\r\n\r\t\t\tDetailedLogging [{0}]\r\n\r\t\t\tHALT_OPERATIONS [{1}]\r\n\r\t\t\tCheckMD5 [{2}]", m_boolDetailedLogging, m_boolUSER_HALT_OPERATIONS, m_boolCheckMD5);
                stringMsg += string.Format("\r\n\r\t\t\tRunCompletedToday [{0}]\r\n\r\t\t\tAllowDestinationPathChangeViaHTTP [{1}]", m_boolRunCompletedToday, m_boolAllowDestinationPathChangeViaHTTP);
                stringMsg += string.Format("\r\n\r\t\t\tCopy Buffer Size [{0}mb]", m_intBufferSizeMB);
                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                stringMsg = string.Format("\r\tTime of Day to Run [{0}]", m_dateTimeOfDayToRun.ToShortTimeString());
                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                StartHttpServer(m_intHttpPort);

                ReportDestinationDriveSpace();

                m_dateToday = DateTime.Now;

                SetupDatabase();
 
                while (m_boolServiceRunning)
                {
                    try
                    {
                        if (m_dateToday.Date != DateTime.Now.Date)
                        {
                            m_dateToday = DateTime.Now;
                            m_boolRunCompletedToday = false;

                            stringMsg = string.Format("New Day. [{0}] Running on [{1}] at {2}", m_stringVersionInfo, m_stringLocalHostName, DateTime.Now.ToString());
                            m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

                            stringMsg = string.Format("\r\tSourcePath [{0}]\r\n\r\t\t\tDestinationPath [{1}]\r\n\r\t\t\tLogFilePath [{2}]", m_stringSourcePath, m_stringDestinationPath, m_stringLogFilePath);
                            //m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                            stringMsg += string.Format("\r\n\r\t\t\tDetailedLogging [{0}]\r\n\r\t\t\tHALT_OPERATIONS [{1}]\r\n\r\t\t\tCheckMD5 [{2}]", m_boolDetailedLogging, m_boolUSER_HALT_OPERATIONS, m_boolCheckMD5);
                            stringMsg += string.Format("\r\n\r\t\t\tRunCompletedToday [{0}]\r\n\r\t\t\tAllowDestinationPathChangeViaHTTP [{1}]", m_boolRunCompletedToday, m_boolAllowDestinationPathChangeViaHTTP);
                            stringMsg += string.Format("\r\n\r\t\t\tCopy Buffer Size [{0}mb]", m_intBufferSizeMB);                            
                            m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                            stringMsg = string.Format("\r\tTime of Day to Run [{0}]", m_dateTimeOfDayToRun.ToShortTimeString());
                            m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                            FileSystemLogger.CleanUpOldLogFiles();

                            ReportDestinationDriveSpace();
                        }

                        if (m_boolUSER_HALT_OPERATIONS != true)
                        {
                            //if (Math.Abs((m_dateTimeOfDayToRun.TimeOfDay - DateTime.Now.TimeOfDay).TotalMinutes) < 2 && (DateTime.Now.TimeOfDay > m_dateTimeOfDayToRun.TimeOfDay) && m_boolRunCompletedToday == false)
                            {
                                m_intTotalCopyAttempts = 0;
                                m_intSuccessfulCopyAttempts = 0;
                                m_intFailedCopyAttempts = 0;
                                m_intFailedMD5Checks = 0;
                                m_intTotalCopyOpertionsNotNeeded = 0;

                                stringMsg = string.Format("Starting Copy Operation...");
                                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                                m_dateTimeLastRun = DateTime.Now;
                                Stopwatch stopWatch = new Stopwatch();
                                
                                stopWatch.Restart();
                                m_longTotalBytesCopied = 0;
                                DoWork();
                                stopWatch.Stop();
                                m_stringElapsedTime = stopWatch.Elapsed.ToString("hh\\:mm\\:ss\\.ff");
                                m_boolRunCompletedToday = true;

                                if (m_stringElapsedTime != string.Empty)
                                {
                                    stringMsg = string.Format("Last Elapsed Copy Time [{0}]", m_stringElapsedTime);
                                    m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                                }

                                stringMsg = string.Format("Total Copy Attempts [{0}] Successful Copy Attempts [{1}] Failed Copy Attempts [{2}] Failed MD5 Checks [{3}] Copy Attempts Not Needed [{4}]"
                                        , m_intTotalCopyAttempts, m_intSuccessfulCopyAttempts, m_intFailedCopyAttempts, m_intFailedMD5Checks, m_intTotalCopyOpertionsNotNeeded);
                                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                                if (m_longTotalBytesCopied != -1)
                                {
                                    stringMsg = string.Format("Bytes Copied [ {0} ]", BytesToString(m_longTotalBytesCopied));
                                    m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                                }
                            }
                            System.Environment.Exit(0);
                        }
                    }
                    catch (Exception Ex)
                    {
                        string stringMsgEx = string.Format("An Exception Fired. {0}", Ex.ToString());
                        m_eventWriteToLog(stringMsgEx, LogMessageType.ErrorAlwaysDisplay);
                    }
        
                    Thread.Sleep(m_timeSpanSleepLength);
                    lock (lockObject)
                    {
                        CheckConfiguration();
                    }
                }
            }
            catch (Exception Ex)
            {
                string stringMsgEx = string.Format("An Exception Fired. {0}", Ex.ToString());
                m_eventWriteToLog(stringMsgEx, LogMessageType.ErrorAlwaysDisplay);
            }
        }

        private void SetupDatabase()
        {
            string stringMsg = string.Empty;
            try
            {
                if (!System.IO.File.Exists(m_stringDatabaseFile))
                    SQLiteConnection.CreateFile(m_stringDatabaseFile);
                string ConnectionString = string.Format("Data Source={0};Version=3;Pooling=True;Max Pool Size=20;", m_stringDatabaseFile);
                using (m_dbConnection = new SQLiteConnection(ConnectionString))
                {
                    m_dbConnection.Open();
                    string sql = @"CREATE TABLE IF NOT EXISTS
                                    FilesFound 
                                    (
                                    ID                INTEGER PRIMARY KEY UNIQUE,
                                    SourcePath        VARCHAR (4096) NOT NULL ,
                                    DestinationPath   VARCHAR (4096) ,
                                    DateFound         DATETIME,
                                    TimesModified     INT            DEFAULT (0),
                                    MD5               VARCHAR (512),
                                    DateCopyStarted   DATETIME,
                                    DateCopyCompleted DATETIME
                                    )";
                    SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    /*
                    sql = "insert into FilesFound (SourcePath, DestinationPath) values ('you', 'you too')";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();

                    sql = "select * from FilesFound order by ID desc";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        //stringMsg = "SourcePath: " + reader["SourcePath"] + "\tDestinationPath: " + reader["DestinationPath"];
                        stringMsg = string.Format("ID[{0}] --> SourcePath:[{1}] \tDestinationPath:[{2}]", reader["ID"], reader["SourcePath"], reader["DestinationPath"]);
                        m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                    }
                     */ 
                }
            }
            catch (Exception Ex)
            {
                string stringMsgEx = string.Format("An Exception Fired. {0}", Ex.ToString());
                m_eventWriteToLog(stringMsgEx, LogMessageType.ErrorAlwaysDisplay);
            }
        }


        private void DoWork()
        {            
            CopyDirectories(m_stringSourcePath, m_stringDestinationPath);
        }

        private void ReportDestinationDriveSpace()
        {
            string stringMsg = string.Empty;
            ConnectionOptions options = new ConnectionOptions();
            //ManagementScope scope = new ManagementScope("\\\\localhost\\root\\cimv2", options); 
            ManagementScope scope = new ManagementScope("\\\\localhost\\root\\cimv2", options);
            scope.Connect();
            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            //Only get Local drives
            SelectQuery query1 = new SelectQuery("Select * from Win32_LogicalDisk Where DriveType = 3");

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection queryCollection = searcher.Get();
            ManagementObjectSearcher searcher1 = new ManagementObjectSearcher(scope, query1);
            ManagementObjectCollection queryCollection1 = searcher1.Get();

            string pathRoot = Path.GetPathRoot(m_stringDestinationPath).Replace(@"\", "").ToUpper() ;

            foreach (ManagementObject m in queryCollection)
            {
                // Display the remote computer information
                
                m_stringWindowsSystemDirectory = m["WindowsDirectory"].ToString();

                stringMsg = string.Format("Computer Name : {0}", m["csname"]);
                stringMsg += string.Format("\r\n\t\t\tWindows Directory : {0}", m["WindowsDirectory"]);
                stringMsg += string.Format("\r\n\t\t\tOperating System: {0}", m["Caption"]);
                stringMsg += string.Format("\r\n\t\t\tVersion: {0}", m["Version"]);
                stringMsg += string.Format("\r\n\t\t\tManufacturer : {0}", m["Manufacturer"]);

                m_eventWriteToLog(stringMsg, LogMessageType.Miscellaneous);
            }

            foreach (ManagementObject mo in queryCollection1)
            {
                // Display Logical Disks information


                if (pathRoot == mo["DeviceID"].ToString())
                    stringMsg = string.Format("Disk Name : {0} <--- Destination", mo["Name"]);
                else
                    stringMsg = string.Format("Disk Name : {0}", mo["Name"]);
                stringMsg += string.Format("\r\n\t\t\tDisk Size : {0}", BytesToString(long.Parse(mo["Size"].ToString())));
                stringMsg += string.Format("\r\n\t\t\tFreeSpace : {0}", BytesToString(long.Parse(mo["FreeSpace"].ToString())));
                stringMsg += string.Format("\r\n\t\t\tDisk DeviceID : {0}", mo["DeviceID"]);
                stringMsg += string.Format("\r\n\t\t\tDisk VolumeName : {0}", mo["VolumeName"]);
                stringMsg += string.Format("\r\n\t\t\tDisk SystemName : {0}", mo["SystemName"]);
                stringMsg += string.Format("\r\n\t\t\tDisk VolumeSerialNumber : {0}", mo["VolumeSerialNumber"]);
                if (pathRoot == mo["DeviceID"].ToString())
                    m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                else
                    m_eventWriteToLog(stringMsg, LogMessageType.Miscellaneous);
            }
        }

        static String BytesToString(long byteCount)
        {
            string[] suf = { " B", " KB", " MB", " GB", " TB", " PB", " EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }


        private void CheckConfiguration()
        {
            string stringMsg = string.Empty;

            //If the source path or destination path has changed via HTTP interface, now is the time to set them
            if (m_boolSourcePathChanged)
            {
                m_stringSourcePath = m_stringSourcePathChangingTo;
                m_boolSourcePathChanged = false;
            }

            if (m_boolDestinationPathChanged)
            {
                m_stringDestinationPath = m_stringDestinationPathChangingTo;
                m_boolDestinationPathChanged = false;
            }

            int intHttpPort = m_intHttpPort;
            DateTime dateTimeOfDayToRun = m_dateTimeOfDayToRun;
            string stringSourcePath = m_stringSourcePath;
            string stringDestinationPath = m_stringDestinationPath;
            string stringLogFilePath = m_stringLogFilePath;
            bool boolDetailedLogging = m_boolDetailedLogging;
            bool boolUSER_HALT_OPERATIONS = m_boolUSER_HALT_OPERATIONS;

            //Load configuration settings
            LoadConfigurationSettings();

            if (intHttpPort != m_intHttpPort)
            {
                stringMsg = string.Format("HTTP Port changed from [{0}] to [{1}], Restarting HTTP Server", intHttpPort, m_intHttpPort);
                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

                StartHttpServer(m_intHttpPort);
            }

            if (dateTimeOfDayToRun.TimeOfDay != m_dateTimeOfDayToRun.TimeOfDay)
            {
                stringMsg = string.Format("Time Of Day To Run changed from [{0}] to [{1}]", dateTimeOfDayToRun.ToShortTimeString(), m_dateTimeOfDayToRun.ToShortTimeString());
                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

                m_boolRunCompletedToday = false;
            }

            if (stringSourcePath != m_stringSourcePath)
            {
                stringMsg = string.Format("Source Path changed from [{0}] to [{1}]", stringSourcePath, m_stringSourcePath);
                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
            }

            if (stringDestinationPath != m_stringDestinationPath)
            {
                stringMsg = string.Format("Destination Path changed from [{0}] to [{1}]", stringDestinationPath, m_stringDestinationPath);
                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
            }

            if (stringLogFilePath != m_stringLogFilePath)
            {
                stringMsg = string.Format("Log File Path changed from [{0}] to [{1}]", stringLogFilePath, m_stringLogFilePath);
                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
            }

            if (boolDetailedLogging != m_boolDetailedLogging)
            {
                stringMsg = string.Format("Detailed Logging Flag changed from [{0}] to [{1}]", boolDetailedLogging, m_boolDetailedLogging);
                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
            }

            if (boolUSER_HALT_OPERATIONS != m_boolUSER_HALT_OPERATIONS)
            {
                stringMsg = string.Format("User Halt Operations Flag changed from [{0}] to [{1}]", boolUSER_HALT_OPERATIONS, m_boolUSER_HALT_OPERATIONS);
                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
            }
        }
        
        private void StartHttpServer(int intPort)
        {
            try
            {
                if (m_HttpServer == null)
                {
                    m_HttpServer = new HttpServer(5);
                    m_HttpServer.ProcessRequest += m_HttpServer_ProcessRequest;
                    m_HttpServer.m_eventWriteToLog += SC_Service_m_eventWriteToLog;
                }
                else
                    m_HttpServer.Stop();

                m_HttpServer.Start(intPort);
                string stringMsg = string.Format("HTTP Server Listening on Port {0}", intPort);
                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
            }
            catch (Exception Ex)
            {
                string stringMsgEx = string.Format("An Exception Fired. {0}", Ex.ToString());
                m_eventWriteToLog(stringMsgEx, LogMessageType.ErrorAlwaysDisplay);
            }
        }

        private void CopyFile(string stringSourceFileName, string stringDestinationPath)
        {
            try
            {
                m_intTotalCopyAttempts++;

                m_stringCurrentFileBeingCopied = stringSourceFileName;
                //Ignore directories
                FileAttributes attr = File.GetAttributes(stringSourceFileName);
                if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                {                    
                    string stringDestinationFileName = string.Format(@"{0}\{1}", stringDestinationPath, Path.GetFileName(stringSourceFileName));

                    bool boolPerformCopy = false;
                    bool boolFileExists = false;

                    //If file exists at destination, don't copy it..unless we need to do an MD5 
                    if (File.Exists(stringDestinationFileName))
                    {
                        boolPerformCopy = false;
                        boolFileExists = true;
                    }
                    else
                        boolPerformCopy = true;

                    //If we need to do a LastWriteTime check
                    if (m_boolCheckLastWriteTime == true && boolFileExists == true)
                    {
                        //If LastWriteTime check fails, perform copy
                        DateTime dateTimeLastModifiedSource = File.GetLastWriteTimeUtc(stringSourceFileName);
                        DateTime dateTimeLastModifiedDestination = File.GetLastWriteTimeUtc(stringDestinationFileName);

                        bool boolLastWriteTime = dateTimeLastModifiedSource == dateTimeLastModifiedDestination;
                        if (boolLastWriteTime == false)
                        {
                            m_intLastWriteTimeChecks++;
                            boolPerformCopy = true;
                            string stringMsg = string.Format("Last Write Times differ for [{0}] and [{1}] [{2}]-[{3}]", stringSourceFileName, stringDestinationFileName, dateTimeLastModifiedSource, dateTimeLastModifiedDestination);
                            m_eventWriteToLog(stringMsg, LogMessageType.Error);
                        }
                        else
                        {
                            boolPerformCopy = false;
                        }
                    }

                    //If we need to do a CRC check
                    if (m_boolCheckMD5 == true && boolFileExists == true)
                    {
                        //If MD5 check fails, perform copy
                        bool boolMD5Passed = CheckMD5(stringSourceFileName, stringDestinationFileName);
                        if (boolMD5Passed == false)
                        {
                            m_intFailedMD5Checks++;
                            boolPerformCopy = true;
                            string stringMsg = string.Format("MD5 Check failed for [{0}] and [{1}]", stringSourceFileName, stringDestinationFileName);
                            m_eventWriteToLog(stringMsg, LogMessageType.Error);
                        }
                        else
                        {
                            boolPerformCopy = false;
                        }
                    }
                    
                    //if file doesn't exist at destination, check if directory exists. If directory doesn't exist, create it
                    if (boolFileExists != true && boolPerformCopy == true)
                        if (Directory.Exists(m_stringDestinationPath) == false)
                            Directory.CreateDirectory(m_stringDestinationPath);

                    if (boolPerformCopy == true)
                    {
                        CustomFileCopier myCopier = new CustomFileCopier(stringSourceFileName, stringDestinationFileName, m_intBufferSizeMB);
                        myCopier.OnComplete += CopyCompleted;
                        myCopier.OnProgressChanged += CopyProgress;
                        myCopier.Copy();
                        
                        m_intSuccessfulCopyAttempts++;
                    }
                    else
                    {
                        m_intTotalCopyOpertionsNotNeeded++;
                    }

                }
            }
            catch (Exception Ex)
            {
                m_intFailedCopyAttempts++;
                string stringMsg = string.Format("An Exception Fired while copying {0} to {1}. {2}", stringSourceFileName, stringDestinationPath, Ex.ToString());
                m_eventWriteToLog(stringMsg, LogMessageType.ErrorAlwaysDisplay);
            }      
            finally
            {
                m_stringCurrentFileBeingCopied = string.Empty;
            }
        }

        private bool CheckMD5(string stringSourceFileName, string stringDestinationFileName)
        {
            using (var md5 = MD5.Create())
            {
                string stringMD5SourceFileName = string.Empty;
                string stringMD5DestinationFileName = string.Empty;
                using (var stream = File.OpenRead(stringSourceFileName))
                {
                    stringMD5SourceFileName =  Encoding.Default.GetString(md5.ComputeHash(stream));
                }
                using (var stream = File.OpenRead(stringDestinationFileName))
                {
                    stringMD5DestinationFileName= Encoding.Default.GetString(md5.ComputeHash(stream));
                }

                return stringMD5SourceFileName == stringMD5DestinationFileName;
            }
        }

        private int GetDirectories(string myBaseDirectory)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(myBaseDirectory);
            return dirInfo.EnumerateDirectories()
                       .AsParallel()
                       .SelectMany(di => di.EnumerateFiles("*.*", SearchOption.AllDirectories))
                       .Count();
        }

        private void CopyDirectories2(string stringSourcePath, string stringDestinationPath)
        {

        }

        private void CopyDirectories(string stringSourcePath, string stringDestinationPath)
        {
            string[] SourceFiles = Directory.GetFiles(stringSourcePath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string stringSourceFileFullPath in SourceFiles)
            {
                try
                {
                    CopyFile(stringSourceFileFullPath, stringDestinationPath);
                    if (m_boolUSER_HALT_OPERATIONS == true)
                    {
                        string stringMsg = string.Format("Halting Operations...");
                        m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                        break;
                    }
                }
                catch (Exception Ex)
                {
                    string stringMsg = string.Format("An Exception Fired while copying {0} to {1}. {2}", stringSourceFileFullPath, stringDestinationPath, Ex.ToString());
                    m_eventWriteToLog(stringMsg, LogMessageType.ErrorAlwaysDisplay);
                }
            }

            //Copy sub directories
            string[] SourceDirectories = Directory.GetDirectories(stringSourcePath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string stringSourceDirectoryFullPath in SourceDirectories)
            {
                string stringSubDestinationFullPath = string.Empty;
                try
                {
                    if (m_boolUSER_HALT_OPERATIONS == true)
                    {
                        string stringMsg = string.Format("Halting Operations...");
                        m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                        break;
                    }

                    string stringSourceSubDirectoryName = Path.GetFileName(stringSourceDirectoryFullPath);
                    //string stringSubDirectoryName = Path.GetDirectoryName(SourceDirectoryFullPath);
                    stringSubDestinationFullPath = string.Format(@"{0}\{1}", stringDestinationPath, stringSourceSubDirectoryName);
                    if (Directory.Exists(stringSubDestinationFullPath) == false)
                        Directory.CreateDirectory(stringSubDestinationFullPath);
                    CopyDirectories(stringSourceDirectoryFullPath, stringSubDestinationFullPath);
                }
                catch (Exception Ex)
                {
                    string stringMsg = string.Format("An Exception Fired while copying Sub directory {0} to {1}. {2}",
                        stringSourceDirectoryFullPath, stringSubDestinationFullPath, Ex.ToString());
                    m_eventWriteToLog(stringMsg, LogMessageType.ErrorAlwaysDisplay);
                }
            }
        }

        internal void StopWorkProcessing()
        {
            m_eventWriteToLog("StopWorkProcessing Invoked. Service Shutting down...", LogMessageType.MiscellaneousAlwaysDisplay);
            if (m_HttpServer != null)
            {
                m_eventWriteToLog("Stopping HTTP server...", LogMessageType.MiscellaneousAlwaysDisplay);
                m_HttpServer.Stop();
            }
        }

        #endregion

        #region Setup

        private void SetupDelegates()
        {
            m_eventWriteToLog += SC_Service_m_eventWriteToLog;
        }

        private void LoadConfigurationSettings()
        {
            try
            {                
                ConfigurationManager.RefreshSection("appSettings");
                if (ConfigurationManager.AppSettings["LogFilePath"].ToString() != string.Empty)
                    m_stringLogFilePath = ConfigurationManager.AppSettings["LogFilePath"].ToString();
                else
                    m_stringLogFilePath = @"c:\logs";

                if (bool.TryParse(ConfigurationManager.AppSettings["AllowDestinationPathChangeViaHTTP"].ToString(), out m_boolAllowDestinationPathChangeViaHTTP) == false)
                    m_boolAllowDestinationPathChangeViaHTTP = false;

                if (bool.TryParse(ConfigurationManager.AppSettings["DetailedLogging"].ToString(), out m_boolDetailedLogging) == false)
                    m_boolDetailedLogging = true;

                if (bool.TryParse(ConfigurationManager.AppSettings["HALT_OPERATIONS"].ToString(), out m_boolUSER_HALT_OPERATIONS) == false)
                    m_boolUSER_HALT_OPERATIONS = false;

                if (bool.TryParse(ConfigurationManager.AppSettings["CheckMD5"].ToString(), out m_boolCheckMD5) == false)
                    m_boolCheckMD5 = true;

                if (bool.TryParse(ConfigurationManager.AppSettings["CheckLastWriteTime"].ToString(), out m_boolCheckLastWriteTime) == false)
                    m_boolCheckLastWriteTime = true;                

                if (DateTime.TryParse(ConfigurationManager.AppSettings["TIME_OF_DAY_TO_RUN"].ToString(), out m_dateTimeOfDayToRun) == false)
                    m_dateTimeOfDayToRun = new DateTime(2016, 1, 1, 0, 0, 0);

                if (int.TryParse(ConfigurationManager.AppSettings["HTTP_PORT"].ToString(), out m_intHttpPort) == false)
                    m_intHttpPort = 8181;

                m_stringSourcePath = ConfigurationManager.AppSettings["SourcePath"].ToString();

                string stringDestinationPath = ConfigurationManager.AppSettings["DestinationPath"].ToString();

                if (ConfigurationManager.AppSettings["BufferSizeMB"] != null)
                    if (int.TryParse(ConfigurationManager.AppSettings["BufferSizeMB"].ToString(), out m_intBufferSizeMB) == false)
                        m_intBufferSizeMB = 1;


                if (!stringDestinationPath.ToUpper().Contains(m_stringWindowsSystemDirectory.ToUpper()))
                    m_stringDestinationPath = stringDestinationPath;
                else
                {
                    string stringMsg = string.Format("The directory specified [{0}] can not be the copy destination. Windows System Directory is [{1}]", stringDestinationPath, m_stringWindowsSystemDirectory);
                    if (m_eventWriteToLog == null) 
                        m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                }

                if (ConfigurationManager.AppSettings["DatabaseFile"] != null)
                    m_stringDatabaseFile = ConfigurationManager.AppSettings["DatabaseFile"].ToString();
                else
                    m_stringDatabaseFile = "MyDatabase.sqlite";
            }
            catch (Exception Ex)
            {
                if (m_eventWriteToLog == null)
                    m_eventWriteToLog("Error in LoadConfigurationsSettings - " + Ex.ToString(), LogMessageType.MiscellaneousAlwaysDisplay);
                else
                    throw new Exception("Item missing from config file. Check it.", Ex);
            }

        }

        #endregion

        #region Event/Delegate Hanlders

        private void m_HttpServer_ProcessRequest(HttpListenerContext context)
        {
            if (context.Request.RawUrl.Contains("favicon"))
                return;
            string stringMsg = string.Empty;
            string stringCheckQueryStringReturn = string.Empty;

            stringMsg = string.Format("Connection Made from [{0}] UserAgent [{1}]", context.Request.RemoteEndPoint.Address, context.Request.UserAgent);
            m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

            if (context.Request.QueryString.Count > 0)
            {
                lock (lockObject)
                {
                    stringCheckQueryStringReturn = CheckQueryString(context);
                }
            }

            StringBuilder sb = new StringBuilder();

            stringMsg = string.Format("[{0}] Running on [{1}]. HTTP Server Listening on Port [{2}] Current Time {3}",
                m_stringVersionInfo, m_stringLocalHostName, m_intHttpPort, DateTime.Now.ToString());
            sb.Append(@"<html><head><style type='text/css' media='screen'>
                            pre {
                              font-family: Arial, sans-erif;
                              font-size: 12px;
                              background-color: #EBECE4; 
                                white-space: pre-wrap;       /* Since CSS 2.1 */
                                white-space: -moz-pre-wrap;  /* Mozilla, since 1999 */
                                white-space: -pre-wrap;      /* Opera 4-6 */
                                white-space: -o-pre-wrap;    /* Opera 7 */
                                word-wrap: break-word;       /* Internet Explorer 5.5+ */
                                background: #555;
                                background-image: -webkit-linear-gradient(#555 50%, #505050 50%);
                                background-image:    -moz-linear-gradient(#555 50%, #505050 50%);
                                background-image:     -ms-linear-gradient(#555 50%, #505050 50%);
                                background-image:      -o-linear-gradient(#555 50%, #505050 50%);
                                background-image:         linear-gradient(#555 50%, #505050 50%);
                                background-position: 0 0;
                                background-repeat: repeat;
                                background-size: 4.5em 4.5em;
                            }
                                </style></head>");
            sb.Append("<body><h3>" + stringMsg + "</h3>");

            string stringSourcePath = m_stringSourcePath;
            string stringDestinationPath = m_stringDestinationPath;
            if (m_boolSourcePathChanged)
                stringSourcePath = m_stringSourcePathChangingTo;
            if (m_boolDestinationPathChanged)
                stringDestinationPath = m_stringDestinationPathChangingTo;

            stringMsg = string.Format("SourcePath [{0}]<br>DestinationPath [{1}]<br>LogFilePath [{2}]", stringSourcePath, stringDestinationPath, m_stringLogFilePath);
            sb.Append(stringMsg);

            stringMsg = string.Format("<br><br>DetailedLogging [{0}]<br>HALT_OPERATIONS [{1}]<br>CheckMD5 [{2}]<br>RunCompletedToday [{3}]", m_boolDetailedLogging, m_boolUSER_HALT_OPERATIONS, m_boolCheckMD5, m_boolRunCompletedToday);
            sb.Append(stringMsg);
            stringMsg = string.Format("<br>AllowDestinationPathChangeViaHTTP [{0}]", m_boolAllowDestinationPathChangeViaHTTP);
            sb.Append(stringMsg);

            stringMsg = string.Format("<h3>Time_of_Day_to_Run [{0}]</h3>", m_dateTimeOfDayToRun.ToShortTimeString());
            sb.Append(stringMsg);

            if (m_dateTimeLastRun != DateTime.MinValue)
            {
                stringMsg = string.Format("<h3>The last run occurred at [{0}]</h3>", m_dateTimeLastRun);
                sb.Append(stringMsg);
            }

            //if(m_boolRunCompletedToday == false) 
            //{
            //    stringMsg = string.Format("<h3>No run has yet occurred for the day</h3>");
            //    sb.Append(stringMsg);
            //}

            if (m_stringElapsedTime != string.Empty)
            {
                stringMsg = string.Format("Last Elapsed Copy Time [{0}]", m_stringElapsedTime);
                sb.Append(stringMsg);
            }

            stringMsg = string.Format("<h4>Total Copy Attempts [{0}]<br> Successful Copy Attempts [{1}]<br> Failed Copy Attempts [{2}]<br> Failed MD5 Checks [{3}]<br> Copy Attempts Not Needed [{4}]</h4>"
            , m_intTotalCopyAttempts, m_intSuccessfulCopyAttempts, m_intFailedCopyAttempts, m_intFailedMD5Checks, m_intTotalCopyOpertionsNotNeeded);
            sb.Append(stringMsg);
            if (m_longTotalBytesCopied != -1)
            {
                stringMsg = string.Format("<h4>Bytes Copied [ {0} ]</h4>", BytesToString(m_longTotalBytesCopied));
                sb.Append(stringMsg);
            }

            if (m_stringCurrentFileBeingCopied != string.Empty)
            {
                stringMsg = string.Format("<br>The current file being copied is [{0}]", m_stringCurrentFileBeingCopied);
                sb.Append(stringMsg);
            }

            if (stringCheckQueryStringReturn != string.Empty)
            {
                stringCheckQueryStringReturn = stringCheckQueryStringReturn.Replace("\n\n", "\n");
                string cha = string.Format("{0}", (char)0x0D, (char)0x0A);
                //stringCheckQueryStringReturn = stringCheckQueryStringReturn.Replace("\n", "<>");
                //stringCheckQueryStringReturn = stringCheckQueryStringReturn.Replace("<>", "\n");
                //stringCheckQueryStringReturn = stringCheckQueryStringReturn.Replace("<br>", "gg<br>");
                //stringCheckQueryStringReturn = stringCheckQueryStringReturn.Replace((char)0x0A, ' ');
                //stringCheckQueryStringReturn = stringCheckQueryStringReturn.Replace("\n", string.Empty);
                //stringCheckQueryStringReturn = RemoveLineEndings(stringCheckQueryStringReturn);
                // Regex.Replace(stringCheckQueryStringReturn, @"\s+", " ");
                stringMsg = string.Format("<br><pre>{0}</pre>", stringCheckQueryStringReturn);
                sb.Append(stringMsg);
            }

            sb.Append("</body></html>");

            byte[] b = Encoding.UTF8.GetBytes(sb.ToString());
            
            context.Response.ContentLength64 = b.Length;
            context.Response.OutputStream.Write(b, 0, b.Length);
            context.Response.OutputStream.Close();
        }

        public static string RemoveLineEndings(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();

            return value.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace(lineSeparator, string.Empty).Replace(paragraphSeparator, string.Empty);
        }

        private string CheckQueryString(HttpListenerContext context)
        {
            string returnValue = string.Empty;
            string stringMsg = string.Empty;

            string value = string.Empty;
            try
            {
                //if (context.Request.QueryString["SourcePath"] != null)
                //    value = context.Request.QueryString["SourcePath"];

                foreach (string parameter in context.Request.QueryString)
                {
                    switch (parameter.ToLower())
                    {
                        case "sourcepath":
                            value = context.Request.QueryString[parameter];
                            string stringSourcePath = m_stringSourcePath;
                            m_stringSourcePathChangingTo = value;
                            if (stringSourcePath != m_stringSourcePathChangingTo)
                            {
                                m_boolUSER_HALT_OPERATIONS = true;
                                m_boolSourcePathChanged = true;
                                stringMsg = string.Format("Halting Operations, Source File Path to be changed from [{0}] to [{1}] via HTTP", stringSourcePath, m_stringSourcePathChangingTo);
                                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

                                saveToConfigFile("SourcePath", m_stringSourcePathChangingTo);
                            }
                            break;
                        case "destinationpath":
                            if (m_boolAllowDestinationPathChangeViaHTTP == true)
                            {
                                    value = context.Request.QueryString[parameter];
                                    if (!value.ToUpper().Contains(m_stringWindowsSystemDirectory.ToUpper()))
                                    {
                                        string stringDestinationPath = m_stringDestinationPath;
                                        m_stringDestinationPathChangingTo = value;
                                        if (stringDestinationPath != m_stringDestinationPathChangingTo)
                                        {
                                            m_boolUSER_HALT_OPERATIONS = true;
                                            m_boolDestinationPathChanged = true;
                                            stringMsg = string.Format("Halting Operations, Destination File Path to be changed from [{0}] to [{1}] via HTTP", stringDestinationPath, m_stringDestinationPathChangingTo);
                                            m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

                                            saveToConfigFile("DestinationPath", m_stringDestinationPathChangingTo);
                                        }
                                    }
                                    else
                                    {
                                        stringMsg = string.Format("The directory specified [{0}] via HTTP can not be the copy destination. Windows System Directory is [{1}]", value, m_stringWindowsSystemDirectory);
                                        m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                                        returnValue = stringMsg + "<br>" + returnValue;
                                    }
                            }
                            else
                            {
                                stringMsg = string.Format("The setting AllowDestinationPathChangeViaHTTP = {0}, parameter ignored.", m_boolAllowDestinationPathChangeViaHTTP);
                                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                                returnValue = stringMsg + "<br>" + returnValue;
                            }
                            break;
                        case "logfilepath":
                            value = context.Request.QueryString[parameter];
                            string stringLogFilePath = m_stringLogFilePath;
                            m_stringLogFilePath = value;
                            if (m_stringLogFilePath != stringLogFilePath)
                            {
                                stringMsg = string.Format("Log File Path changed from [{0}] to [{1}] via HTTP", stringLogFilePath, m_stringLogFilePath);
                                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

                                saveToConfigFile("LogFilePath", m_stringLogFilePath);
                            }
                            break;

                        case "detailedlogging":
                            value = context.Request.QueryString[parameter];
                            bool boolDetailedLogging = m_boolDetailedLogging;
                            if (bool.TryParse(value, out m_boolDetailedLogging) && m_boolDetailedLogging != boolDetailedLogging)
                            {
                                stringMsg = string.Format("Detailed Logging changed from [{0}] to [{1}] via HTTP", boolDetailedLogging, m_boolDetailedLogging);
                                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

                                saveToConfigFile("DetailedLogging", m_boolDetailedLogging.ToString());
                            }
                            break;

                        case "halt_operations":
                            value = context.Request.QueryString[parameter];
                            bool boolUSER_HALT_OPERATIONS = m_boolUSER_HALT_OPERATIONS;

                            if (bool.TryParse(value, out m_boolUSER_HALT_OPERATIONS) && m_boolUSER_HALT_OPERATIONS != boolUSER_HALT_OPERATIONS)
                            {
                                stringMsg = string.Format("Halt Operations Flag changed from [{0}] to [{1}] via HTTP", boolUSER_HALT_OPERATIONS, m_boolUSER_HALT_OPERATIONS);
                                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

                                saveToConfigFile("HALT_OPERATIONS", m_boolUSER_HALT_OPERATIONS.ToString());
                            }
                            break;

                        case "checkmd5":
                            value = context.Request.QueryString[parameter];
                            bool boolCheckMD5 = m_boolCheckMD5;

                            if (bool.TryParse(value, out m_boolCheckMD5) && m_boolCheckMD5 != boolCheckMD5)
                            {
                                stringMsg = string.Format("Check MD5 Flag changed from [{0}] to [{1}] via HTTP", boolCheckMD5, m_boolCheckMD5);
                                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

                                saveToConfigFile("CheckMD5", m_boolCheckMD5.ToString());
                            }
                            break;

                        case "checklastwritetime":
                            value = context.Request.QueryString[parameter];
                            bool boolCheckLastWriteTime = m_boolCheckLastWriteTime;

                            if (bool.TryParse(value, out m_boolCheckLastWriteTime) && m_boolCheckLastWriteTime != boolCheckLastWriteTime)
                            {
                                stringMsg = string.Format("Check Last Write time Flag changed from [{0}] to [{1}] via HTTP", boolCheckLastWriteTime, m_boolCheckLastWriteTime);
                                m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);

                                saveToConfigFile("CheckLastWriteTime", m_boolCheckLastWriteTime.ToString());
                            }
                            break;

                        case "time_of_day_to_run":
                            value = context.Request.QueryString["time_of_day_to_run"];
                            DateTime dateTimeOfDayToRun;
                            if (DateTime.TryParse(value, out dateTimeOfDayToRun))
                            {
                                if (dateTimeOfDayToRun.TimeOfDay != m_dateTimeOfDayToRun.TimeOfDay)
                                {
                                    stringMsg = string.Format("Time of Day to Run changed from [{0}] to [{1}] via HTTP", m_dateTimeOfDayToRun.ToShortTimeString(), dateTimeOfDayToRun.ToShortTimeString());
                                    m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                                    m_dateTimeOfDayToRun = dateTimeOfDayToRun;

                                    saveToConfigFile("TIME_OF_DAY_TO_RUN", m_dateTimeOfDayToRun.ToString());
                                    m_boolRunCompletedToday = false;

                                }
                            }

                            break;
                        case "http_port":
                            value = context.Request.QueryString["http_port"];
                            if (int.TryParse(value, out m_intHttpPortChangeTo))
                            {
                                if (m_intHttpPortChangeTo != m_intHttpPort)
                                {
                                    stringMsg = string.Format("HTTP Port changed from [{0}] to [{1}] via HTTP, Restarting HTTP Server", m_intHttpPort, m_intHttpPortChangeTo);
                                    m_eventWriteToLog(stringMsg, LogMessageType.MiscellaneousAlwaysDisplay);
                                    m_intHttpPort = m_intHttpPortChangeTo;

                                    saveToConfigFile("HTTP_PORT", m_intHttpPortChangeTo.ToString());

                                    StartHttpServer(m_intHttpPort);
                                }
                            }
                            break;
                        case "show_log":
                            //Read in few lines of the log file
                            value = context.Request.QueryString[parameter];
                            int intNumberOfLinesToPull = 100;
                            if (int.TryParse(value, out intNumberOfLinesToPull) == false)
                                intNumberOfLinesToPull = 100;
                            string stringFullPathToLogFile = FileSystemLogger.ActualLogFile;
                            int i = 0;
                            List<string> listOfLines = new List<string>();
                            foreach (var line in File.ReadLines(stringFullPathToLogFile).Reverse())
                            {
                                listOfLines.Add(line + "\n");
                                //returnValue += line + "\n";

                                if (i > intNumberOfLinesToPull)
                                    break;
                                i++;
                            }
                            for (int ii = listOfLines.Count; ii > 0; ii--)
                            {
                                string line = listOfLines[ii - 1].Replace("\r\t", "");
                                returnValue += line;
                            }
                            /*
                            using (StreamReader fs = new StreamReader(stringFullPathToLogFile, Encoding.UTF8))
                            {
                                
                                returnValue = fs.ReadToEnd();
                            }
                            */
                            break;
                    }
                }
            }
            catch (Exception Ex)
            {
                stringMsg = string.Format("An Exception Fired {0}", Ex.ToString());
                m_eventWriteToLog(stringMsg, LogMessageType.ErrorAlwaysDisplay);
                stringMsg = string.Format("Error {0}", Ex.Message);

                returnValue = stringMsg;
            }
            return returnValue;
        }
       
        private void saveToConfigFile(string stringSetting, string stringValue)
        {
            //Open config file
            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            //Remove setting and add setting with new value
            config.AppSettings.Settings.Remove(stringSetting);
            config.AppSettings.Settings.Add(stringSetting, stringValue);
            //Save the new WebService address to the config file on disk
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        void CopyProgress(string FilePath, double Percentage, ref bool Cancel)
        {
            try
            {
                string stringMsg = string.Format("Copying of File {0} at {1:N2}% ", FilePath, Percentage);
                m_eventWriteToLog(stringMsg, LogMessageType.Miscellaneous);
            }
            catch (Exception Ex)
            {
                //Eat the error :)
            }
        }

        void CopyCompleted(string FilePath, long TotalBytes)
        {
            string stringMsg = string.Format("Copying of File {0} completed. File Size {1}", FilePath, BytesToString(TotalBytes));
            m_eventWriteToLog(stringMsg, LogMessageType.Miscellaneous);
            m_longTotalBytesCopied += TotalBytes;
        }

        void SC_Service_m_eventWriteToLog(string stringLogText, LogMessageType MessageType)
        {
            FileSystemLogger.ApplicationTitle = m_stringApplicationTitle;
            FileSystemLogger.LogFileName = m_stringApplicationTitle;

            FileSystemLogger.LogFilePath = m_stringLogFilePath;

            FileSystemLogger.DeleteOlderThan = m_timeSpanDeleteLogFilesOlderThan;

            Debug.WriteLine(stringLogText);

            if ((MessageType == LogMessageType.Error || MessageType == LogMessageType.Miscellaneous) && m_boolDetailedLogging == false)
                return;

            FileSystemLogger.LogInformationDaily(stringLogText);
        }

        #endregion

    }
}
