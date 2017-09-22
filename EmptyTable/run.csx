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
        using (SqlCommand cmd = new SqlCommand("TRUNCATE TABLE pbist_apimgmt.[GeoLite2-City-Blocks-IPv4];" +
                                                "ALTER INDEX [IX_GeoLite2-City-Blocks-IPv4] ON pbist_apimgmt.[GeoLite2-City-Blocks-IPv4] DISABLE;" +
                                                "ALTER INDEX [IX_GeoLite2-City-Blocks-IPv4_IPPart] ON pbist_apimgmt.[GeoLite2-City-Blocks-IPv4] DISABLE;", targetConnection) { CommandTimeout = 0 })
        {
            cmd.ExecuteNonQuery();
        }
    }
   return req.CreateResponse(HttpStatusCode.OK, "");
}
