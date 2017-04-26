#r "Newtonsoft.Json"
#r "System.Data"

#load "ParseQuery.csx"
#load "SaveData.csx"
#load "Sentiment.csx"
#load "SqlTables.csx"
#load "TweetHandler.csx"

using System;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Web;



    public class SqlHelpers
    {

        public SqlHelpers(string connection)
        {
            this.connectionString = connection;
        }
                private string connectionString;

        //Execute SQL query without returning anything
        public void ExecuteSqlNonQuery(string sqlQuery)
        {
            using (SqlConnection connection = new SqlConnection(connectionString.ToString()))
            {
                connection.Open();
                var command = new SqlCommand(sqlQuery, connection);
                //log.Info("*********************************ExecuteSqlNonQuery************************** Query: " + sqlQuery);
                command.ExecuteNonQuery();
            }
        }

        //Execute SQL query returning something 
        public int ExecuteSqlScalar(string sqlQuery)
        {
            using (SqlConnection connection = new SqlConnection(connectionString.ToString()))
            {
                connection.Open();
                //log.Info("*********************************ExecuteSqlScalar************************** Query: " + sqlQuery);
                var command = new SqlCommand(sqlQuery, connection);
                return (int)command.ExecuteScalar();
            }
        }

        //Execute SQL query using a reader
        public string ExecuteSqlQuery(string sqlQuery, string value)
        {
            using (SqlConnection connection = new SqlConnection(connectionString.ToString()))
            {
                connection.Open();
                var command = new SqlCommand(sqlQuery, connection);
                SqlDataReader reader = command.ExecuteReader();
                string returnObject = string.Empty;
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        returnObject = reader[value].ToString();
                    }
                }
                return returnObject;
            }
        }

        //Generate SQL Statement
        public string generateSQLQuery(string tableName, Dictionary<string, string> dictionary)
        {
            string sqlQueryGenerator = $"insert into " + tableName + "(" +
                                       String.Join(", ", dictionary.Select(x => x.Key)) + ")" + " VALUES " + "('" +
                                       String.Join("',N'", dictionary.Select(x => {
                                           if (string.IsNullOrEmpty(x.Value))
                                           {
                                               return x.Value;
                                           }
                                           //log.Info("********************generateSQLQuery************************** Text: " + x.Value);
                                           return x.Value.Replace("'", "''");
                                       })) + "')";
            return sqlQueryGenerator;
        }



    }
