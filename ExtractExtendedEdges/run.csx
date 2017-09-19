#r "Newtonsoft.Json"
#r "System.Data"

#load "../Shared/Node.csx"

using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Net;
using Newtonsoft.Json;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    var connStr = System.Configuration.ConfigurationManager.ConnectionStrings["TemplatesSQL"].ConnectionString;
    const int LINK_WITHIN_SECONDS_DEFAULT = 1;
    dynamic jsonContent = await req.Content.ReadAsStringAsync();

    // rudimentary validation
    if (jsonContent == null)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest);
    }
    var data = JsonConvert.DeserializeObject(jsonContent);
    if (data == null || data.ResultSet == null)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest);
    }

    DataTable memoryTable = new DataTable("CallExtendedEdgeList");
    memoryTable.Columns.Add(new DataColumn("RequestId", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("Product", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("Api", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("Operation", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("CreatedDate", typeof(DateTime)));
    memoryTable.Columns.Add(new DataColumn("RelatedRequestId", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("RelatedProduct", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("RelatedApi", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("RelatedOperation", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("RelatedCreatedDate", typeof(DateTime)));
    memoryTable.Columns.Add(new DataColumn("IPAddress", typeof(string)));

    int seconds = data.LinkWithinSeconds ?? LINK_WITHIN_SECONDS_DEFAULT;

    Node previous = null;

    foreach (var row in data.ResultSet)
    {
        Node currentNode = new Node(
            requestId: row.RequestId.Value,
            product: row.Product.Value,
            operation: row.Operation.Value,
            api: row.Api.Value,
            ipAddress: row.IPAddress.Value,
            createdDate: row.CreatedDate.Value
        );
        if (previous != null)
        {
            if (previous.IsTemporallyRelated(currentNode, seconds))
            {
                memoryTable.Rows.Add(
                    previous.RequestId,
                    previous.Product,
                    previous.Api,
                    previous.Operation,
                    previous.CreatedDate,
                    currentNode.RequestId,
                    currentNode.Product,
                    currentNode.Api,
                    currentNode.Operation,
                    currentNode.CreatedDate,
                    previous.IPAddress
                );
            }

        }
        previous = currentNode;
    }

    using (var conn = new SqlConnection(connStr))
    {
        conn.Open();
        SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);
        bulkCopy.BulkCopyTimeout = 300; // in seconds
        bulkCopy.DestinationTableName = "pbist_apimgmt.CallExtendedEdgeList_STAGE";
        bulkCopy.WriteToServer(memoryTable);
    }

    return req.CreateResponse(HttpStatusCode.OK, new
    {
        NumberRowsWritten = memoryTable.Rows.Count
    });
}
