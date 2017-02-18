#r "Newtonsoft.Json"

#load "EntityResult.csx"

using System;
using System.Configuration;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

/*
Configuration:
NOTE: Regular expression parser is configured to be case-insensitive
RegularExpressions: {"entities":[{"regex":"Google", "type":"Company", "canonicalValue": "Google, Inc."}]}

Input:
{
    text: "My Document Here"
}

Output:
{
    "entities":[
        {"value":"Google, Inc","type":"Company","position":0,"lengthInText":6}
        , etc
    ]
}
*/

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"Webhook was triggered!");

    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);

    dynamic regularExpressions;

    try
    {
        regularExpressions = JsonConvert.DeserializeObject(ConfigurationManager.AppSettings["RegularExpressions"]);
    }
    catch (Exception e)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, new {
            error = "Error retrieving appsetting 'RegularExpressions'.  Please ensure that the setting is defined and in the correct format. " + e.ToString()});
    }

    if (data.text == null) {
        return req.CreateResponse(HttpStatusCode.BadRequest, new {
            error = "Please pass text property in the input object"
        });
    }

    var resultList = new LinkedList<EntityResult>();

    foreach (dynamic byoEntity in regularExpressions.entities)
    {
        var regex = new Regex(byoEntity.regex.ToString(), RegexOptions.IgnoreCase);

        foreach( Match match in regex.Matches(data.text.ToString()))
        {
            var entity = new EntityResult() {
                value = byoEntity.canonicalValue.ToString(),
                type = byoEntity.type.ToString(),
                position = match.Index,
                lengthInText = match.Value.Length
            };

            resultList.AddLast(entity);

            log.Verbose($"Found {match.Value} at position {match.Index}");
        }
    }

    return req.CreateResponse(HttpStatusCode.OK, new {
        entities = resultList
    });
}
