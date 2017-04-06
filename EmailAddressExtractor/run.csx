#r "Newtonsoft.Json"

using System;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"EmailAddress Webhook was triggered!");

    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);

    List<dynamic> result = new List<dynamic>();

    if (data.toAddresses == null && data.fromAddress == null && data.ccAddresses == null && data.bccAddresses == null) {
        return req.CreateResponse(HttpStatusCode.BadRequest, new {
            error = "Please pass fromAddress, toAddresses, ccAddresses, or bccAddresses in the input object"
        });
    }

    if (data.toAddresses != null) {
        result.AddRange(ParseAddresses(data.toAddresses.ToString(), "TO"));
    }

    if (data.ccAddresses != null) {
        result.AddRange(ParseAddresses(data.ccAddresses.ToString(), "CC"));
    }

    if (data.bccAddresses != null) {
        result.AddRange(ParseAddresses(data.bccAddresses.ToString(), "BCC"));
    }

    if (data.fromAddress != null) {
        result.AddRange(ParseAddresses(data.fromAddress.ToString(), "FROM"));
    }


    return req.CreateResponse(HttpStatusCode.OK, new {
        addresses = result
    });
}

private static dynamic CreateAddress(string address, string addressType)
{
    dynamic addressInstance = new System.Dynamic.ExpandoObject();
    addressInstance.address = address;
    addressInstance.addressType = addressType;

    return addressInstance;
}

private static List<dynamic> ParseAddresses(string input, string addressType)
{
    List<dynamic> result = new List<dynamic>();

    foreach (Match match in Regex.Matches(input, "[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?"))
    {
        result.Add(CreateAddress(match.Value, addressType));
    }

    return result;
}
