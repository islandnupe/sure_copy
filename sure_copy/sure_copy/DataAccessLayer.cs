using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
        string ConnectionString = string.Empty ;
        public event DelegateWriteToLog m_eventWriteToLog;

        public DataAccessLayer(string DatabaseFile = "default_db.sqlite")
        {
            m_stringDatabaseFile = DatabaseFile;
            SetupDatabase();
        }

        public int Add(string SourcePath, string SourceMD5 = "", string DestinationPath = "",
                            string DestinationMD5 = "", string DateModified = "", string DateCreated = "",
                            string DateCopyStarted = "", string DateCopyCompleted = "")
        {
            if(string.IsNullOrEmpty(SourcePath))
                throw new DataAccessLayerException("SourcePath Must not be NULL or empty");

            int ID = 0;
            try
            {
                string parameters_clause = string.Empty;
                string values_clause = string.Empty;
                
                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnectionString))
                {
                    dbConnection.Open();
                    SQLiteCommand command = new SQLiteCommand(dbConnection);

                    parameters_clause = "(SourcePath, DateFound";
                    values_clause = "(@SourcePath, @DateFound";
                    command.Parameters.Add(new SQLiteParameter("@SourcePath", SourcePath));

                    command.Parameters.Add(new SQLiteParameter("@DateFound", DateTime.Now));

                    if (!string.IsNullOrEmpty(DestinationPath))
                    {
                        parameters_clause += ", DestinationPath";
                        values_clause += ", @DestinationPath";
                        command.Parameters.Add(new SQLiteParameter("@DestinationPath", DestinationPath));
                    }
                    if (!string.IsNullOrEmpty(SourceMD5))
                    {
                        parameters_clause += ", SourceMD5";
                        values_clause += ", @SourceMD5";
                        command.Parameters.Add(new SQLiteParameter("@SourceMD5", SourceMD5));
                    } if (!string.IsNullOrEmpty(DestinationMD5))
                    {
                        parameters_clause += ", DestinationMD5";
                        values_clause += ", @DestinationMD5";
                        command.Parameters.Add(new SQLiteParameter("@DestinationMD5", DestinationMD5));
                    }
                    if (!string.IsNullOrEmpty(DateModified))
                    {
                        parameters_clause += ", DateModified";
                        values_clause += ", @DateModified";
                        command.Parameters.Add(new SQLiteParameter("@DateModified", DateModified));
                    }
                    if (!string.IsNullOrEmpty(DateCreated))
                    {
                        parameters_clause += ", DateCreated";
                        values_clause += ", @DateCreated";
                        command.Parameters.Add(new SQLiteParameter("@DateCreated", DateCreated));
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
                    parameters_clause += ")";
                    values_clause += ")";

                    string sql = string.Format(@"insert into FilesFound {0} values {1}", parameters_clause, values_clause);
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
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

        public void Update(int ID, string SourcePath, string SourceMD5 = "", string DestinationPath = "",
                            string DestinationMD5 = "", string DateModified = "", string DateCreated = "",
                            string DateCopyStarted = "", string DateCopyCompleted = "")
        {
            if (string.IsNullOrEmpty(SourcePath) == true)
                throw new DataAccessLayerException("Source ID Must not be NUll or empty");

            try
            {
                string parameters_clause = string.Empty;

                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnectionString))
                {
                    dbConnection.Open();
                    SQLiteCommand command = new SQLiteCommand(dbConnection);
                    parameters_clause = string.Empty;
                    /*
                    if (!string.IsNullOrEmpty(SourcePath))
                    {
                        parameters_clause += "SourcePath = @SourcePath";
                        command.Parameters.Add(new SQLiteParameter("@SourcePath", SourcePath));
                    }
                    */
                    if (!string.IsNullOrEmpty(SourceMD5))
                    {
                        parameters_clause += "SourceMD5 = @SourceMD5,";
                        command.Parameters.Add(new SQLiteParameter("@SourceMD5", SourceMD5));
                    }
                    if (!string.IsNullOrEmpty(DestinationPath))
                    {
                        parameters_clause += "DestinationPath = @DestinationPath,";
                        command.Parameters.Add(new SQLiteParameter("@DestinationPath", DestinationPath));
                    }
                    if (!string.IsNullOrEmpty(DestinationMD5))
                    {
                        parameters_clause += "DestinationMD5 = @DestinationMD5,";
                        command.Parameters.Add(new SQLiteParameter("@DestinationMD5", DestinationMD5));
                    }

                    if (!string.IsNullOrEmpty(DateModified))
                    {
                        parameters_clause += "DateModified = @DateModified,";
                        command.Parameters.Add(new SQLiteParameter("@DateModified", DateModified));
                    }
                    if (!string.IsNullOrEmpty(DateCreated))
                    {
                        parameters_clause += "DateCreated = @DateCreated,";
                        command.Parameters.Add(new SQLiteParameter("@DateCreated", DateCreated));
                    }
                    if (!string.IsNullOrEmpty(DateCopyStarted))
                    {
                        parameters_clause += "DateCopyStarted = @DateCopyStarted,";
                        command.Parameters.Add(new SQLiteParameter("@DateCopyStarted", DateCopyStarted));
                    }
                    if (!string.IsNullOrEmpty(DateCopyCompleted))
                    {
                        parameters_clause += "DateCopyCompleted = @DateCopyCompleted,";
                        command.Parameters.Add(new SQLiteParameter("@DateCopyCompleted", DateCopyCompleted));
                    }

                    parameters_clause = parameters_clause.Trim(',');
                    string sql = string.Format(@"UPDATE FilesFound SET {0} where SourcePath=@SourcePath", parameters_clause);
                    command.Parameters.Add(new SQLiteParameter("@SourcePath", SourcePath));

                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException Ex)
            {
                throw new DataAccessLayerException(Ex.Message);
            }

            finally
            {
            }
        }


        public void Upsert(string SourcePath, string SourceMD5 = "", string DestinationPath = "",
                            string DestinationMD5 = "", string DateModified = "", string DateCreated = "",
                            string DateCopyStarted = "", string DateCopyCompleted = "")
        {
            if (string.IsNullOrEmpty(SourcePath))
                throw new DataAccessLayerException("SourcePath Must not be non Zero");

            try
            {
                string values_clause = string.Empty;
                string parameters_clause = string.Empty;

                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnectionString))
                {
                    dbConnection.Open();
                    SQLiteCommand command = new SQLiteCommand(dbConnection);

                    parameters_clause += "(SourcePath";
                    values_clause += "(COALESCE((SELECT SourcePath FROM FilesFound WHERE SourcePath = @SourcePath), @SourcePath)";
                    command.Parameters.Add(new SQLiteParameter("@SourcePath", SourcePath));

                    parameters_clause += ", DateFound";
                    values_clause += ", COALESCE((SELECT DateFound FROM FilesFound WHERE SourcePath = @SourcePath), @DateFound)";

                    command.Parameters.Add(new SQLiteParameter("@DateFound", DateTime.Now));

                    if (!string.IsNullOrEmpty(DestinationPath))
                    {
                        parameters_clause += ", DestinationPath";
                        values_clause += ", COALESCE((SELECT DestinationPath FROM FilesFound WHERE SourcePath = @SourcePath), @DestinationPath)";
                        command.Parameters.Add(new SQLiteParameter("@DestinationPath", DestinationPath));
                    }
                    if (!string.IsNullOrEmpty(SourceMD5))
                    {
                        parameters_clause += ", SourceMD5";
                        values_clause += ", COALESCE((SELECT SourceMD5 FROM FilesFound WHERE SourcePath = @SourcePath), @SourceMD5)";
                        command.Parameters.Add(new SQLiteParameter("@SourceMD5", SourceMD5));
                    } if (!string.IsNullOrEmpty(DestinationMD5))
                    {
                        parameters_clause += ", DestinationMD5";
                        values_clause += ", COALESCE((SELECT DestinationMD5 FROM FilesFound WHERE SourcePath = @SourcePath), @DestinationMD5)";
                        command.Parameters.Add(new SQLiteParameter("@DestinationMD5", DestinationMD5));
                    }
                    if (!string.IsNullOrEmpty(DateModified))
                    {
                        parameters_clause += ", DateModified";
                        values_clause += ", COALESCE((SELECT DateModified FROM FilesFound WHERE SourcePath = @SourcePath), @DateModified)";
                        command.Parameters.Add(new SQLiteParameter("@DateModified", DateTime.Parse(DateModified)));
                    }
                    if (!string.IsNullOrEmpty(DateCreated))
                    {
                        parameters_clause += ", DateCreated";
                        values_clause += ", COALESCE((SELECT DateCreated FROM FilesFound WHERE SourcePath = @SourcePath), @DateCreated)";
                        command.Parameters.Add(new SQLiteParameter("@DateCreated", DateTime.Parse(DateCreated)));
                    }
                    if (!string.IsNullOrEmpty(DateCopyStarted))
                    {
                        parameters_clause += ", DateCopyStarted";
                        values_clause += ", COALESCE((SELECT DateCopyStarted FROM FilesFound WHERE SourcePath = @SourcePath), @DateCopyStarted)";
                        command.Parameters.Add(new SQLiteParameter("@DateCopyStarted", DateTime.Parse(DateCopyStarted)));
                    }
                    if (!string.IsNullOrEmpty(DateCopyCompleted))
                    {
                        parameters_clause += ", DateCopyCompleted";
                        values_clause += ", COALESCE((SELECT DateCopyCompleted FROM FilesFound WHERE SourcePath = @SourcePath), @DateCopyCompleted)";
                        command.Parameters.Add(new SQLiteParameter("@DateCopyCompleted", DateTime.Parse(DateCopyCompleted)));
                    }


                    parameters_clause += ", TimesModified";
                    values_clause += ", COALESCE((SELECT TimesModified+1 FROM FilesFound WHERE SourcePath = @SourcePath), @TimesModified)";
                    command.Parameters.Add(new SQLiteParameter("@TimesModified", 1));
                   
                    parameters_clause += ")";
                    values_clause += ")";

                    string sql = string.Format(@"INSERT or REPLACE INTO FilesFound 
                                               {0}
                                               VALUES {1}", parameters_clause, values_clause);

                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException Ex)
            {
                throw new DataAccessLayerException(Ex.Message);
            }

            finally
            {
            }
        }

        public void Delete(int ID)
        {
            if (ID == 0)
                throw new DataAccessLayerException("ID Must not be non Zero");

            try
            {
                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnectionString))
                {
                    dbConnection.Open();
                    SQLiteCommand command = new SQLiteCommand(dbConnection);

                    string sql = string.Format(@"DELETE FilesFound where ID=@ID");
                    command.Parameters.Add(new SQLiteParameter("@ID", ID));

                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception Ex)
            {
                throw new DataAccessLayerException(Ex.Message);
            }
            finally
            {
            }
        }

        public DataSet Get(string SourcePath)
        {
            if (string.IsNullOrEmpty(SourcePath))
                throw new DataAccessLayerException("SourcePath Must not be NULL or empty");

            DataSet data_set = new DataSet();

            try
            {
                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnectionString))
                {
                    dbConnection.Open();
                    string sql = string.Format(@"SELECT ID,
                                                   SourcePath,
                                                   DestinationPath,
                                                   DateFound,
                                                   TimesModified,
                                                   SourceMD5,
                                                   DestinationMD5,
                                                   DateModified,
                                                   DateCreated,
                                                   DateCopyStarted,
                                                   DateCopyCompleted FROM FilesFound where SourcePath=@SourcePath");

                    sql = string.Format(@"SELECT DateModified FROM FilesFound where SourcePath=@SourcePath");
                    SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                    command.Parameters.Add(new SQLiteParameter("@SourcePath", SourcePath));

                    using (SQLiteDataAdapter sqlDataAdapter = new SQLiteDataAdapter(command))
                    {
                        sqlDataAdapter.Fill(data_set);
                    }
                }
            }
            catch (Exception Ex)
            {
                throw new DataAccessLayerException(Ex.Message);
            }
            finally
            {
            }
            return data_set;
        }

        private void SetupDatabase()
        {
            string stringMsg = string.Empty;
            try
            {
                if (!System.IO.File.Exists(m_stringDatabaseFile))
                    SQLiteConnection.CreateFile(m_stringDatabaseFile);
                ConnectionString = string.Format("Data Source={0};Version=3;Pooling=True;Max Pool Size=20;datetimeformat=CurrentCulture", m_stringDatabaseFile);
                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnectionString))
                {
                    dbConnection.Open();
                    string sql = @"CREATE TABLE IF NOT EXISTS
                                    FilesFound 
                                    (
                                    ID                INTEGER PRIMARY KEY UNIQUE,
                                    SourcePath        VARCHAR (4096) NOT NULL UNIQUE,
                                    DestinationPath   VARCHAR (4096) ,
                                    DateFound         DATETIME,
                                    TimesModified     INT            DEFAULT (0),
                                    SourceMD5         VARCHAR (512),
                                    DestinationMD5           VARCHAR (512),
                                    DateModified      DATETIME,
                                    DateCreated       DATETIME,
                                    DateCopyStarted   DATETIME,
                                    DateCopyCompleted DATETIME
                                    )";
                    SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                    command.ExecuteNonQuery();
                    sql = @"CREATE INDEX IF NOT EXISTS Idx_SourcePath ON FilesFound (SourcePath);";
                    command = new SQLiteCommand(sql, dbConnection);
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
                throw new DataAccessLayerException(Ex.Message);
            }
        }

    }
}
