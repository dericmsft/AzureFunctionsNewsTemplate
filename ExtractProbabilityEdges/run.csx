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

    DataTable memoryTable = new DataTable("CallProbabilityEdgeList");
    memoryTable.Columns.Add(new DataColumn("Product", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("Api", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("Operation", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("RelatedProduct", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("RelatedApi", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("RelatedOperation", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("IPAddress", typeof(string)));
    memoryTable.Columns.Add(new DataColumn("CallRelationshipCount", typeof(int)));
    memoryTable.Columns.Add(new DataColumn("StartingCallTotalCount", typeof(int)));

    Dictionary<Node, int> nodeCounts = new Dictionary<Node, int>();
    Dictionary<Tuple<Node, Node>, int> edgeCounts = new Dictionary<Tuple<Node, Node>, int>();

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
        if (!nodeCounts.ContainsKey(currentNode))
        {
            nodeCounts.Add(currentNode, 0);
        }
        nodeCounts[currentNode] = nodeCounts[currentNode] + 1;

        if (previous != null)
        {
            if (previous.IsTemporallyRelated(currentNode, seconds))
            {
                Tuple<Node, Node> edge = Tuple.Create(previous, currentNode);
                if (!edgeCounts.ContainsKey(edge))
                {
                    edgeCounts.Add(edge, 0);
                }
                edgeCounts[edge] = edgeCounts[edge] + 1;
            }

        }
        previous = currentNode;
    }

    foreach (KeyValuePair<Tuple<Node, Node>, int> kp in edgeCounts)
    {
        Node requestNode = kp.Key.Item1;
        Node relatedNode = kp.Key.Item2;
        int edgeCount = kp.Value;

        int nodeCount = nodeCounts[requestNode];
        memoryTable.Rows.Add(
            requestNode.Product,
            requestNode.Api,
            requestNode.Operation,
            relatedNode.Product,
            relatedNode.Api,
            relatedNode.Operation,
            requestNode.IPAddress,
            edgeCount,
            nodeCount
        );
    }


    using (var conn = new SqlConnection(connStr))
    {
        conn.Open();
        SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);
        bulkCopy.BulkCopyTimeout = 300; // in seconds
        bulkCopy.DestinationTableName = "pbist_apimgmt.callprobabilityedgelist_staging";
        bulkCopy.WriteToServer(memoryTable);
    }

    return req.CreateResponse(HttpStatusCode.OK, new
    {
        NumberRowsWritten = memoryTable.Rows.Count
    });
}
