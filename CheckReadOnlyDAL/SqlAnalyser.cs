using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CheckReadOnlyDAL
{
    public class SqlAnalyser
    {
        private SqlConnection _sqlConnection;

        public SqlConnection SqlConnection
        {
            get
            {
                if (_sqlConnection == null)
                {
                    string conxStr = ConfigurationManager.ConnectionStrings["CDISCOUNT_CATALOG_SYNCHRO"].ConnectionString;
                    _sqlConnection = new SqlConnection(conxStr);
                }
                return _sqlConnection;
            }
        }

        public string getStoredProcedureSourceCode(string spName)
        {
            try
            {
                string queryString = string.Format(@"USE CDISCOUNT_CATALOG;
                                       EXEC sp_helptext '{0}';", spName);

                SqlCommand command = new SqlCommand();
                command.Connection = SqlConnection;
                command.CommandTimeout = 15;
                command.CommandType = CommandType.Text;
                command.CommandText = queryString;

                string result = "";
                SqlConnection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result += reader[0];
                }

                return result;
            }
            finally
            {
                SqlConnection.Close();
            }
        }

        public bool spIsReadOnly(string spName)
        {
            string storedProcedureSourceCode = getStoredProcedureSourceCode(spName);

            SQLVisitor myVisitor = new SQLVisitor(storedProcedureSourceCode);

            Tuple<int,int,int,int> result = myVisitor.DumpStatistics();

            return result.Item1>0 && result.Item2==0 && result.Item3==0 && result.Item4==0;
        }
    }
}
