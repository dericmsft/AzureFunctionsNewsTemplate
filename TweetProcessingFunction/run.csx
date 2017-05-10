#r "Newtonsoft.Json"
#r "System.Data"

#load "ParseQuery.csx"
#load "SaveData.csx"
#load "Sentiment.csx"
#load "SqlHelpers.csx"
#load "SqlTables.csx"
#load "TweetHandler.csx"
#load "DirectionChecks.csx"


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


	public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
        {
            // Initialize SQL connection string and bring in tweets
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;
			string jsonContent = await req.Content.ReadAsStringAsync();
            var tweets = JsonConvert.DeserializeObject(jsonContent);
            int i = 0;

            //Initialize tweet handler
            TweetHandler tweetHandler = new TweetHandler(connectionString);

            // Check if tweet is in the form of json array or json object
            if (tweets is JArray)
            {
                foreach (var item in (JArray)tweets)
                {
                    var individualtweet = item.ToString();
                    await tweetHandler.ParseTweet(individualtweet, i);
                    i = i + 1;
                }
            }
            else
            {
                await tweetHandler.ParseTweet(jsonContent, i);
            }
	    return req.CreateResponse(HttpStatusCode.OK, "");
        }
