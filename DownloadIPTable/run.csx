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


 

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    Uri GEOLITE_CITY = new Uri("https://geolite.maxmind.com/download/geoip/database/GeoLite2-City-CSV.zip");
               
    WebRequest zipFileRequest = WebRequest.Create(GEOLITE_CITY);
    using (HttpWebResponse response = (HttpWebResponse)zipFileRequest.GetResponse())
    {
        if (response.StatusCode != HttpStatusCode.OK)
            throw new Exception("Failed to get Zip file");

        using (Stream zipStream = response.GetResponseStream())
        {
            ZipArchive zipFile = new ZipArchive(zipStream, ZipArchiveMode.Read);
            ZipArchiveEntry blocksIPv4 = null;
            for (int i = 0; i < zipFile.Entries.Count; i++)
            {
                if (zipFile.Entries[i].Name.EndsWith("GeoLite2-City-Blocks-IPv4.csv"))
                {
                    blocksIPv4 = zipFile.Entries[i];
                    break;
                }
            }

            blocksIPv4.ExtractToFile("d:/local/temp.csv", true); 
        }
    }

     return req.CreateResponse(HttpStatusCode.OK, "");
}
