using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyServices
{
    public class DataServices
    {
        DBservices dbservice;
        public DataServices()
        {
            dbservice = new DBservices();
        }
        public bool processAttendanceFromFile(string ConnectionString, string filepath, out string errorLog)
        {
            errorLog = "";
            DataTable table = new DataTable();
            table.Columns.Add("EmployeeID", typeof(int));
            table.Columns.Add("DateTime", typeof(DateTime));


            StreamReader objInput = new StreamReader(filepath, System.Text.Encoding.Default);
            string contents = objInput.ReadToEnd().Trim();
            objInput.Close();
            string[] split = System.Text.RegularExpressions.Regex.Split(contents, "\\r\n  ", RegexOptions.None);
            foreach (string s in split)
            {
                string[] finalData = System.Text.RegularExpressions.Regex.Split(s, "\\t", RegexOptions.None);
                table.Rows.Add(Convert.ToInt32(finalData[0]), Convert.ToDateTime(finalData[1]));
            }

            return dbservice.processAndSave(ConnectionString, table,out errorLog);
        }

        public DataTable getRecordsFor(string ConnectionString, DateTime date, out string errorLog)
        {
            errorLog = "";
            DataSet ds = dbservice.getRecordsForTheDate(ConnectionString, date, out errorLog);
            return ds.Tables[0];
        }

        public DataTable getRecordsFor(string ConnectionString, int month, int year, out string errorLog)
        {
            errorLog = "";
            DataSet ds = dbservice.getRecordsForTheMonth(ConnectionString, month, year, out errorLog);
            return ds.Tables[0];
        }

        public DataTable getRecordsFor(string ConnectionString, DateTime fromDate, DateTime toDate, out string errorLog)
        {
            errorLog = "";
            DataSet ds = dbservice.getRecordsForRange(ConnectionString, fromDate, toDate, out errorLog);
            return ds.Tables[0];
        }


    }
}
