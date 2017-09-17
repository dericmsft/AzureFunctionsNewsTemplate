using System;
using System.Globalization;

public class Node
{
    public String RequestId { get; }
    public String Product { get; }
    public String Api { get; }
    public String Operation { get; }
    public String IPAddress { get; }
    public DateTime CreatedDate { get; }

    public Node(
        String requestId,
        String product,
        String api,
        String operation,
        String ipAddress,
        DateTime createdDate
    )
    {
        RequestId = requestId;
        Product = product;
        Api = api;
        Operation = operation;
        IPAddress = ipAddress;
        CreatedDate = createdDate;
    }

    public bool IsTemporallyRelated(
        Node other,
        int seconds
    )
    {
        return (other.CreatedDate - CreatedDate).TotalSeconds <= seconds;
    }

    public override int GetHashCode()
    {
        return Tuple.Create(Product, Api, Operation).GetHashCode();
    }

    public override bool Equals(Object obj)
    {
        if (!(obj is Node)) return false;
        Node node = (Node)obj;
        return node.Product == this.Product && node.Api == this.Api && node.Operation == this.Operation;
    }

}