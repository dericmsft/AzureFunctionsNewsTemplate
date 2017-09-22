#r "System.Data"
using System.Net;
using System;
using System.Data;
using System.Data.SqlClient;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
      var connStr = System.Configuration.ConfigurationManager.ConnectionStrings["TemplatesSQL"].ConnectionString;
    using (SqlConnection targetConnection = new SqlConnection(connStr))
    {
        targetConnection.Open();
        
        // TODO: move out to their own action
        using (SqlCommand cmd = new SqlCommand("UPDATE pbist_apimgmt.[GeoLite2-City-Blocks-IPv4] SET IPpart=LEFT([network], CHARINDEX('.', [network]) - 1)", targetConnection) { CommandTimeout = 0 })
        {
            cmd.ExecuteNonQuery();
            cmd.CommandText = "ALTER INDEX [IX_GeoLite2-City-Blocks-IPv4] ON pbist_apimgmt.[GeoLite2-City-Blocks-IPv4] REBUILD";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "ALTER INDEX [IX_GeoLite2-City-Blocks-IPv4_IPPart] ON pbist_apimgmt.[GeoLite2-City-Blocks-IPv4] REBUILD";
            cmd.ExecuteNonQuery();
        }
    }
   return req.CreateResponse(HttpStatusCode.OK, "");
}
