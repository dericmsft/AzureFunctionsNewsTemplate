#r "Newtonsoft.Json"
#r "System.Data"

#load "ParseQuery.csx"
#load "SaveData.csx"
#load "Sentiment.csx"
#load "SqlHelpers.csx"
#load "SqlTables.csx"
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


    public class TweetHandler
    {
        public TweetHandler(string connection)
        {
            if (string.IsNullOrEmpty(connection))
            {
                throw new ArgumentNullException("connection", "Connection string is null or empty.");
            }
            this.connectionString = connection;
        }

        private dynamic tweet = string.Empty;
        private JObject tweetObj = new JObject();
        private string connectionString;

        public async Task<bool> ParseTweet(string entireTweet, bool i)
        {
            // Initialize SQL helpers and tables
            SqlHelpers sqlHelpers = new SqlHelpers(connectionString);
            SqlTables sqlTables = new SqlTables();

            // Convert JSON to dynamic C# object
            tweetObj = JObject.Parse(entireTweet);
            tweet = tweetObj;

            //Connect to Azure SQL Database & bring in Search Terms
            string searchTerms = sqlHelpers.ExecuteSqlQuery("select QueryString FROM pbist_twitter.twitter_query where Id = \'1\'", "QueryString");

            //Update tweet ID to ensure we pick up onlu new tweets
            if (i == true)
            {
                sqlHelpers.ExecuteSqlNonQuery($"UPDATE pbist_twitter.twitter_query SET TweetId='{tweet["TweetId"]}' WHERE Id = 1");
                i = false;
            }

            //Split out search term into separate keywords
            ParseQuery parseQuery = new ParseQuery();
            List<int> splitOnKeyWord = parseQuery.GetAllIndexes(searchTerms, @"OR|AND");
            List<string> words = new List<string>();

            if (splitOnKeyWord.Count != 0)
            {
                List<string> roughWords = parseQuery.SplitQuery(splitOnKeyWord, searchTerms);

                foreach (string word in roughWords)
                {
                    List<string> phrases = parseQuery.CleanWords(word);
                    foreach (string phrase in phrases)
                    {
                        words.Add(phrase);
                    }
                }
            }
            else
            {
                List<string> phrases = parseQuery.CleanWords(searchTerms);
                foreach (string phrase in phrases)
                {
                    words.Add(phrase);
                }
            }

            //Figure out if keywords are search terms or accounts
            Dictionary<string, string> searchTermWithId = await parseQuery.checkIfAccount(words, connectionString);

            //Work out sentiment of tweet
            Sentiment sentimentCheck = new Sentiment();
            sqlTables.originalTweets["lang"] = tweet.TweetLanguageCode.ToString();
            if (sqlTables.originalTweets["lang"] == "en")
            {
                //log.Info("********************ParseTweet**************************" + tweet.TweetId.ToString());
                string sentiment = await sentimentCheck.MakeSentimentRequest(tweet);
                //log.Info("********************ParseTweet************************** Sentiment: " + sentiment);
                string sentimentBin = (Math.Floor(double.Parse(sentiment) * 10) / 10).ToString(CultureInfo.InvariantCulture);
                string sentimentPosNeg = String.Empty;
                if (double.Parse(sentimentBin) > 0.1)
                {
                    sentimentPosNeg = "Positive";
                }
                else if (double.Parse(sentimentBin) < -0.1)
                {
                    sentimentPosNeg = "Negative";
                }
                else
                {
                    sentimentPosNeg = "Neutral";
                }

                //Save sentiment and language metadata into dictionary
                sqlTables.originalTweets["sentiment"] = sentiment;
                sqlTables.originalTweets["sentimentBin"] = sentimentBin;
                sqlTables.originalTweets["sentimentPosNeg"] = sentimentPosNeg;
            }
            else
            {
                sqlTables.originalTweets["sentimentPosNeg"] = "Undefined";
            }

            sqlTables.searchTerms["tweetid"] = tweet.TweetId;
            SaveData saveData = new SaveData();
            DirectionChecks directionCheck = new DirectionChecks();
            // Work out account and tweet direction for retweets
            if (tweet.OriginalTweet != null)
            {
                sqlTables.originalTweets["twitterhandle"] = tweet.OriginalTweet.UserDetails.UserName;
                if (searchTermWithId.Count > 0)
                {                    
                    foreach (var entry in searchTermWithId)
                    {
                        sqlTables.searchTerms["direction"] = "null";
                        sqlTables.searchTerms["searchterm"] = entry.Key;
                        sqlTables.searchTerms["accountid"] = entry.Value;
                        directionCheck.HashtagDirectionCheck(entry, sqlTables, tweet, connectionString, " Retweet");
                        if (entry.Value != null)
                        {
                            directionCheck.MessageDirectionCheck(entry, tweetObj.SelectToken("OriginalTweet.UserMentions"), sqlTables, tweet, connectionString, " Retweet");
                        }
                    }
                }

                // Save retweets into SQL table
                saveData.SaveTweets(tweet.OriginalTweet, sqlTables, connectionString);
            }

            // Works out the tweet direction for original tweets (not retweets)
            else
            {
                if (searchTermWithId.Count > 0)
                {
                    foreach (var entry in searchTermWithId)
                    {
                        sqlTables.searchTerms["direction"] = "null";
                        sqlTables.searchTerms["searchterm"] = entry.Key;
                        sqlTables.searchTerms["accountid"] = entry.Value;
                        directionCheck.HashtagDirectionCheck(entry, sqlTables, tweet, connectionString);
                        if (entry.Value != null)
                        {
                            directionCheck.MessageDirectionCheck(entry, tweetObj.SelectToken("UserMentions"), sqlTables, tweet, connectionString);
                        }
                    }
                }

                // Save original tweets into SQL Table
                saveData.SaveTweets(tweet, sqlTables, connectionString);
            }

            //Save processed tweets into SQL Table
            saveData.SaveProcessedTweets(tweet, tweetObj, sqlTables, connectionString);

            string text = tweet.TweetText.ToString();

            //Populate hashtag slicer table
            if (text.Contains("#"))
            {
                sqlTables.hashtagSlicer["tweetid"] = tweet.TweetId;
                saveData.HashtagMentions(text, '#', "facet", "pbist_twitter.hashtag_slicer", sqlTables.hashtagSlicer, connectionString);
            }

            //Populate author hashtag network table
            if (text.Contains("#"))
            {
                sqlTables.authorHashtagGraph["tweetid"] = tweet.TweetId;
                sqlTables.authorHashtagGraph["author"] = tweet.UserDetails.UserName;
                saveData.HashtagMentions(text, '#', "hashtag", "pbist_twitter.authorhashtag_graph", sqlTables.authorHashtagGraph, connectionString);
            }

            //Populate mention slicer table
            if (text.Contains("@"))
            {
                sqlTables.mentionSlicer["tweetid"] = tweet.TweetId;
                saveData.HashtagMentions(text, '@', "facet", "pbist_twitter.mention_slicer", sqlTables.mentionSlicer, connectionString);
            }

            //Populate author mention network table
            if (text.Contains("@"))
            {
                sqlTables.authorMentionGraph["tweetid"] = tweet.TweetId;
                sqlTables.authorMentionGraph["author"] = tweet.UserDetails.UserName;
                saveData.HashtagMentions(text, '@', "mention", "pbist_twitter.authormention_graph", sqlTables.authorMentionGraph, connectionString);
            }

            return true;
        }
    }
