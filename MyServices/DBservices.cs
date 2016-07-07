using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyServices
{
    public class DBservices
    {
        public bool backupMyDatabase(string ConnectionString, string location)
        {

            SqlConnection conn = new SqlConnection(ConnectionString);
            var sqlConStrBuilder = new SqlConnectionStringBuilder(ConnectionString);
            SqlCommand cmd = conn.CreateCommand();
            //string backupFileName = String.Format("{0}{1}-{2}.bak", location, sqlConStrBuilder.InitialCatalog, DateTime.Now.ToString("yyyy-MM-dd"));
            cmd.CommandText = String.Format("BACKUP DATABASE {0} TO DISK='{1}'", sqlConStrBuilder.InitialCatalog, location);
            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (SqlException)
            {
                conn.Close();
                return false;
            }

            return true;
        }

        public bool restoreMyDB(string ConnectionString, string location)
        {
            SqlConnection conn = new SqlConnection(ConnectionString);
            var sqlConStrBuilder = new SqlConnectionStringBuilder(ConnectionString);
            SqlCommand cmd = conn.CreateCommand();
            string st = @"D:\Development\Testing\DB_Tester\DB_Tester\mytest.mdf";
            string jk = @"D:\Development\Testing\DB_Tester\DB_Tester\mytest_log.ldf";
            cmd.CommandText = String.Format("USE master RESTORE DATABASE {0} FROM DISK='{1}' WITH MOVE '{5}' TO '{2}', MOVE '{3}' TO '{4}', REPLACE", sqlConStrBuilder.InitialCatalog, location, st, "mytest_log", jk, "mytest");
            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (SqlException)
            {
                conn.Close();
                return false;
            }

            return true;
        }

        public bool processAndSave(string ConnectionString, DataTable dt, out string errorlog)
        {
            bool result = true;
            errorlog ="";
            result = saveRecords(ConnectionString, dt, out errorlog);
            if (result)
            {
                foreach (DataRow row in dt.Rows)
                {
                    DataSet currentDateRecords = getRecordsForEmployeeForDay(ConnectionString, Convert.ToInt32(row[0]), Convert.ToDateTime(row[1]), out  errorlog);
                    if (currentDateRecords.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow availableRow in currentDateRecords.Tables[0].Rows)
                        {
                            if (availableRow[4].ToString() == "")//if sign out is null
                            {
                                result = signOutEmployeeFortheDay(ConnectionString, Convert.ToInt32(availableRow[0]), Convert.ToDateTime(row[1]), out errorlog);
                                if(!result)
                                {
                                    return result;
                                }
                            }
                            else
                            {
                                result = signInEmployeeFortheDay(ConnectionString, Convert.ToInt32(row[0]), Convert.ToDateTime(row[1]), out errorlog);
                                if (!result)
                                {
                                    return result;
                                }
                            }
                        }
                    }
                    else
                    {
                        result = signInEmployeeFortheDay(ConnectionString, Convert.ToInt32(row[0]), Convert.ToDateTime(row[1]), out errorlog);
                        if (!result)
                        {
                            return result;
                        }
                    }
                }
            }

            return result;
        }

        public bool signOutEmployeeFortheDay(string ConnectionString, int recordID, DateTime date, out string errorLog)
        {
            errorLog = "";
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    try
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;
                        command.CommandText = "UPDATE att_records SET sign_out_time = @signouttime WHERE record_id = @recid";
                        connection.Open();
                        command.Parameters.Add(new SqlParameter("@recid", SqlDbType.Int));
                        command.Parameters.Add(new SqlParameter("@signouttime", SqlDbType.DateTime));

                        command.Parameters["@recid"].Value = recordID;
                        command.Parameters["@signouttime"].Value = date;
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        errorLog = "Error occurred : \nMessage: " + ex.Message + "\nAt:  Project:- MyServices. Class:- DBServices. Function:- signOutEmployeeFortheDay.\nData: Record ID:-"+recordID;
                        connection.Close();
                        return false;
                    }
                }
            }

            return true;
        }

        public bool signInEmployeeFortheDay(string ConnectionString, int employeeNumber, DateTime date, out string errorLog)
        {
            errorLog = "";
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    try
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;
                        command.CommandText = "INSERT into att_records (employee_id, record_date,sign_in_time) VALUES (@emp,@recorddate,@signintime)";
                        connection.Open();
                        command.Parameters.Add(new SqlParameter("@emp", SqlDbType.Int));
                        command.Parameters.Add(new SqlParameter("@recorddate", SqlDbType.Date));
                        command.Parameters.Add(new SqlParameter("@signintime", SqlDbType.DateTime));

                        command.Parameters["@emp"].Value = employeeNumber;
                        command.Parameters["@recorddate"].Value = date;
                        command.Parameters["@signintime"].Value = date;
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        errorLog = "Error occurred : \nMessage: " + ex.Message + "\nAt:  Project:- MyServices. Class:- DBServices. Function:- signInEmployeeFortheDay.\nData: Employee ID:-" + employeeNumber + " Date:-" + date;
                        connection.Close();
                        return false;
                    }
                }
            }

            return true;
        }

        public DataSet getRecordsForEmployeeForDay(string ConnectionString, int employeeNumber, DateTime day, out string errorLog)
        {
            errorLog = "";
            SqlConnection conn = new SqlConnection(ConnectionString);
            SqlDataAdapter dataAdap = new SqlDataAdapter();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "Select * from att_records Where employee_id =" + employeeNumber + " AND record_date ='" + day.Date.ToString("yyyy-MM-dd") + "' order by record_id desc";
            dataAdap.SelectCommand = cmd;
            DataSet ds = new DataSet();

            try
            {
                conn.Open();
                dataAdap.Fill(ds);
            }
            catch (Exception ex)
            {
                errorLog = "Error occurred : \nMessage: " + ex.Message + "\nAt:  Project:- MyServices. Class:- DBServices. Function:- getRecordsForEmployeeForDay.\nData: Employee ID:-" + employeeNumber + " Date:-" + day;
            }
            conn.Close();

            return ds;
        }
        public bool saveRecords(string ConnectionString, DataTable dt, out string errorLog)
        {
            errorLog = "";
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    try
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;
                        command.CommandText = "INSERT into att_row_data (employee_id, date_time, created_date_time) VALUES (@emp,@record,@addedtime)";
                        connection.Open();
                        command.Parameters.Add(new SqlParameter("@emp", SqlDbType.NVarChar));
                        command.Parameters.Add(new SqlParameter("@record", SqlDbType.DateTime));
                        command.Parameters.Add(new SqlParameter("@addedtime", SqlDbType.DateTime));
                        foreach (DataRow row in dt.Rows)
                        {
                            command.Parameters["@emp"].Value = row[0];
                            command.Parameters["@record"].Value = row[1];
                            command.Parameters["@addedtime"].Value = DateTime.Now;
                            command.ExecuteNonQuery();
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        errorLog = "Error occurred : \nMessage: " + ex.Message + "\nAt:  Project:- MyServices. Class:- DBServices. Function:- saveRecords.";
                        connection.Close();
                        return false;
                    }
                }
            }

            return true;
        }

        internal DataSet getRecordsForTheDate(string ConnectionString, DateTime date, out string errorLog)
        {
            errorLog = "";
            SqlConnection conn = new SqlConnection(ConnectionString);
            SqlDataAdapter dataAdap = new SqlDataAdapter();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "Select * from att_records Where record_date ='" + date.Date.ToString("yyyy-MM-dd") + "' order by record_id";
            dataAdap.SelectCommand = cmd;
            DataSet ds = new DataSet();

            try
            {
                conn.Open();
                dataAdap.Fill(ds);
            }
            catch (Exception ex)
            {
                errorLog = "Error occurred : \nMessage: " + ex.Message + "\nAt:  Project:- MyServices. Class:- DBServices. Function:- getRecordsForTheDate.\nData: Date:-" + date;
            }
            conn.Close();

            return ds;
        }

        internal DataSet getRecordsForTheMonth(string ConnectionString, int month, int year, out string errorLog)
        {
            errorLog = "";
            SqlConnection conn = new SqlConnection(ConnectionString);
            SqlDataAdapter dataAdap = new SqlDataAdapter();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "Select * from att_records Where MONTH(record_date) =" + month + " AND YEAR(record_date) =" + year + " order by record_id";
            dataAdap.SelectCommand = cmd;
            DataSet ds = new DataSet();

            try
            {
                conn.Open();
                dataAdap.Fill(ds);
            }
            catch (Exception ex)
            {
                errorLog = "Error occurred : \nMessage: " + ex.Message + "\nAt:  Project:- MyServices. Class:- DBServices. Function:- getRecordsForTheMonth.\nData: Month:-" + month + ", Year:- "+year;
            }
            conn.Close();

            return ds;
        }

        internal DataSet getRecordsForRange(string ConnectionString, DateTime fromDate, DateTime toDate, out string errorLog)
        {
            errorLog = "";
            SqlConnection conn = new SqlConnection(ConnectionString);
            SqlDataAdapter dataAdap = new SqlDataAdapter();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "Select * from att_records Where record_date BETWEEN" + fromDate.Date.ToString("yyyy-MM-dd") + " AND " + toDate.Date.ToString("yyyy-MM-dd") + " order by record_id";
            dataAdap.SelectCommand = cmd;
            DataSet ds = new DataSet();

            try
            {
                conn.Open();
                dataAdap.Fill(ds);
            }
            catch (Exception ex)
            {
                errorLog = "Error occurred : \nMessage: " + ex.Message + "\nAt:  Project:- MyServices. Class:- DBServices. Function:- getRecordsForRange.\nData: From date:-" + fromDate + ", To date:- " + toDate;
            }
            conn.Close();

            return ds;
        }
    }
}
