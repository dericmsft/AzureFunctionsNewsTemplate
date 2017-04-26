#r "Newtonsoft.Json"
#r "System.Data"

#load "ParseQuery.csx"
#load "SaveData.csx"
#load "SqlHelpers.csx"
#load "SqlTables.csx"
#load "TweetHandler.csx"

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



    public class Sentiment
    {
        public async Task<string> MakeSentimentRequest(dynamic tweet)
        {
            string result = string.Empty;

            dynamic objResult = null;

            //log.Info("*************MakeSentimentRequest***************** TweetText: " + tweet.TweetText.ToString());
            HttpResponseMessage response = new HttpResponseMessage();
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {
                    Inputs = new Dictionary<string, List<Dictionary<string, string>>>() {
                        {
                            "input1",
                            new List<Dictionary<string, string>>(){new Dictionary<string, string>(){
                                    {
                                        "Text", tweet.TweetText.ToString()
                                    },
                                }
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>() { }
                };

                //Request headers
				string apiKey = System.Configuration.ConfigurationManager.ConnectionStrings["apiKey"].ConnectionString;
				string url = System.Configuration.ConfigurationManager.ConnectionStrings["webserviceUrl"].ConnectionString;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.BaseAddress = new Uri(url);

                response = await client.PostAsJsonAsync(url, scoreRequest);
                if (response.IsSuccessStatusCode)
                {
                    result = await response.Content.ReadAsStringAsync();
                    objResult = JsonConvert.DeserializeObject(result);
                    //log.Info("*************MakeSentimentRequest***************** Score: " + objResult.Results.output1[0].score);
                }
                else
                {
                    //log.Info(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    //log.Info(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                }
            }

            if (objResult == null)
            {
                return string.Empty;
            }
            return objResult.Results.output1[0].score;
        }
    }
