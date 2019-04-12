using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sure_copy
{
    class DataAccessLayerException : Exception
    {
        public DataAccessLayerException()
        {
        }

        public DataAccessLayerException(string expMessage)
            : base(String.Format("DAL Exception: {0}", expMessage))
        {

        }
    }

    class DataAccessLayer
    {
        string m_stringDatabaseFile = string.Empty ;
        SQLiteConnection m_dbConnection;
        public event DelegateWriteToLog m_eventWriteToLog;

        public DataAccessLayer(string DatabaseFile = "default_db.sqlite")
        {
            SetupDatabase();
        }

        public int Add(string SourcePath, string DestinationPath = "", string DateFound = "",
                            string MD5 = "", string DateCopyStarted = "", string DateCopyCompleted = "")
        {
            if(string.IsNullOrEmpty(SourcePath))
                throw new DataAccessLayerException("SourcePath Must not be NULL or empty");

            int ID = 0;
            try
            {
                string parameters_clause = string.Empty;
                string values_clause = string.Empty;

                SQLiteCommand command = new SQLiteCommand(m_dbConnection);

                parameters_clause = "(SourcePath";
                values_clause = "(@SourcePath";
                command.Parameters.Add(new SQLiteParameter("@SourcePath", SourcePath));

                if (!string.IsNullOrEmpty(DestinationPath))
                {
                    parameters_clause += ", DestinationPath";
                    values_clause += ", @DestinationPath";
                    command.Parameters.Add(new SQLiteParameter("@DestinationPath", DestinationPath));
                }
                if (!string.IsNullOrEmpty(DateFound))
                {
                    parameters_clause += ", DateFound";
                    values_clause += ", @DateFound";
                    command.Parameters.Add(new SQLiteParameter("@DateFound", DateFound));
                }
                if (!string.IsNullOrEmpty(MD5))
                {
                    parameters_clause += ", MD5";
                    values_clause += ", @MD5";
                    command.Parameters.Add(new SQLiteParameter("@MD5", MD5));
                }
                if (!string.IsNullOrEmpty(DateCopyStarted))
                {
                    parameters_clause += ", DateCopyStarted";
                    values_clause += ", @DateCopyStarted";
                    command.Parameters.Add(new SQLiteParameter("@DateCopyStarted", DateCopyStarted));
                }
                if (!string.IsNullOrEmpty(DateCopyCompleted))
                {
                    parameters_clause += ", DateCopyCompleted";
                    values_clause += ", @DateCopyCompleted";
                    command.Parameters.Add(new SQLiteParameter("@DateCopyCompleted", DateCopyCompleted));
                }
                parameters_clause = ")";
                values_clause = ")";

                string sql = string.Format(@"insert into FilesFound {0} values {1}", parameters_clause, values_clause);
                command.CommandText = sql;
                command.ExecuteNonQuery();

            }
            catch (SQLiteException Ex)
            {
                throw new DataAccessLayerException(Ex.Message);
            }
            finally
            {

            }

            return ID;
        }

        public void Update(int ID, string SourcePath="", string DestinationPath = "", string DateFound = "",
                            string MD5 = "", string DateCopyStarted = "", string DateCopyCompleted = "")
        {
            try
            {

            }
            catch (Exception Ex)
            {

            }
            finally
            {
            }
        }

        public void Delete(int ID)
        {
            try
            {

            }
            catch (Exception Ex)
            {

            }
            finally
            {
            }
        }

        public DataRow Get(int ID)
        {
            DataRow data_row = null ;
            try
            {

            }
            catch (Exception Ex)
            {

            }
            finally
            {
            }
            return data_row;
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
                if (m_eventWriteToLog != null)
                {
                    string stringMsgEx = string.Format("An Exception Fired. {0}", Ex.ToString());
                    m_eventWriteToLog(stringMsgEx, LogMessageType.ErrorAlwaysDisplay);
                }
            }
        }

    }
}
