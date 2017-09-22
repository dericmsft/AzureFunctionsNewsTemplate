#r "System.Data"
#r "System.IO"
#r "System.IO.Compression"
#r "System.IO.Compression.FileSystem"
#r "System.Net"

using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Data.SqlClient;
// using System.IO.Compression.FileSystem;
using System.Net;


public static async Task<int> Run(HttpRequestMessage req, TraceWriter log)
{
    
    dynamic data = await req.Content.ReadAsAsync<object>();
    int index = (int)data;

    var lines = File.ReadAllLines("d:/local/temp.csv");
    int terminationIndex = 0;
    string sqlConnection = System.Configuration.ConfigurationManager.ConnectionStrings["TemplatesSQL"].ConnectionString;

    if (lines.Length - index > 100000)
    {
        terminationIndex = index + 100000;
    }
    else
    {
        terminationIndex = lines.Length;
    }

    using (SqlConnection targetConnection = new SqlConnection(sqlConnection))
    {
        targetConnection.Open();
        SqlBulkCopy bulkCopy = new SqlBulkCopy(targetConnection) { BulkCopyTimeout = 0 };
        DataTable blocksTable = GetBlocksTable();
        for (int i = index; i < terminationIndex; i++)
        {
            var splitLine = lines[i].Split(',');
            DataRow dr = blocksTable.NewRow();
            for (int j = 0; j < blocksTable.Columns.Count; j++)
            {       
                dr[j] = string.IsNullOrEmpty(splitLine[j]) ? DBNull.Value : (object)splitLine[j];
            }

            blocksTable.Rows.Add(dr);
        }

        bulkCopy.DestinationTableName = "pbist_apimgmt.[GeoLite2-City-Blocks-IPv4]";
        bulkCopy.WriteToServer(blocksTable);
        bulkCopy.Close();
    }

    if (terminationIndex == lines.Length)
    {
        return -1;
    }

    return terminationIndex;
}

 private static DataTable GetBlocksTable()
{
    DataTable dt = new DataTable();

    dt.Columns.Add("network", typeof(string));
    dt.Columns.Add("geoname_id", typeof(int));
    dt.Columns.Add("registered_country_geoname_id", typeof(int));
    dt.Columns.Add("represented_country_geoname_id", typeof(int));
    dt.Columns.Add("is_anonymous_proxy", typeof(byte));
    dt.Columns.Add("is_satellite_provider", typeof(byte));
    dt.Columns.Add("postal_code", typeof(string));
    dt.Columns.Add("latitude", typeof(string));
    dt.Columns.Add("longitude", typeof(string));
    dt.Columns.Add("accuracy_radius", typeof(int));
    return dt;
}
