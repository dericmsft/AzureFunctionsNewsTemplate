#r "Newtonsoft.Json"
#r "System.Data"

#load "ParseQuery.csx"
#load "SaveData.csx"
#load "Sentiment.csx"
#load "SqlHelpers.csx"
#load "SqlTables.csx"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

    public class DirectionChecks
    {
        //Figure out if hashtag is in tweet
        public void HashtagDirectionCheck(KeyValuePair<string, string> currentEntry, SqlTables sqlTable, dynamic tweet, string connectionString, string retweet = "")
        {
            int response = 0;
            SqlHelpers sqlHelper = new SqlHelpers(connectionString);
            string tweetText = tweet.TweetText.ToString();
            bool result = Regex.IsMatch(tweetText, @"\\b" + currentEntry.Key + @"\\b", RegexOptions.IgnoreCase);
            if(result == true)
            {
                sqlTable.searchTerms["direction"] = "Text" + retweet;
                sqlTable.searchTerms["tweetid"] = tweet.TweetId;
                response = sqlHelper.ExecuteSqlScalar(
                $"Select count(1) FROM pbist_twitter.search_terms WHERE tweetid = '{sqlTables.search_terms["tweetid"]}'AND direction = '{"sqlTables.search_terms["direction"]) AND searchterm = '{currentEntry.Key}'");
                if (response == 0)
                {
                    try
                    {
                        sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.search_terms", sqlTables.searchTerms));
                    }
                    catch (Exception e) { }
                }
            }
            string hashtag = "#" + currentEntry.Key.ToLower();
            if (tweetText.ToLower().Contains(hashtag))
            {
                sqlTable.searchTerms["direction"] = "Hashtag" + retweet;
                sqlTable.searchTerms["tweetid"] = tweet.TweetId;
                response = sqlHelper.ExecuteSqlScalar(
                $"Select count(1) FROM pbist_twitter.search_terms WHERE tweetid = '{sqlTables.search_terms["tweetid"]}'AND direction = '{sqlTables.search_terms["direction"]) AND searchterm = '{currentEntry.Key}'");
                if (response == 0)
                {
                    try
                    {
                        sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.search_terms", sqlTables.searchTerms));
                    }
                    catch (Exception e) { }
                }
            }
            }
        }

        //Figure out direction of tweet
        public void MessageDirectionCheck(KeyValuePair<string, string> currentEntry, JToken userMentions, SqlTables sqlTable, dynamic tweet, string connectionString, string retweet = "")
        {
            int response = 0;
            SqlHelpers sqlHelper = new SqlHelpers(connectionString);
            if (currentEntry.Value == (tweet.UserDetails.Id.ToString()))
            {
                sqlTable.searchTerms["direction"] = "Outbound" + retweet;
                if (tweet.TweetInReplyToUserId != null)
                {
                    sqlTable.searchTerms["direction"] = "Outbound Reply" + retweet;
                }
                response = sqlHelper.ExecuteSqlScalar(
                $"Select count(1) FROM pbist_twitter.search_terms WHERE tweetid = '{sqlTables.search_terms["tweetid"]}'AND direction = '{sqlTables.search_terms["direction"]) AND searchterm = '{currentEntry.Key}'");
                if (response == 0)
                {
                    try
                    {
                        sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.search_terms", sqlTables.searchTerms));
                    }
                    catch (Exception e) { }
                }
            }
            else if (tweet.TweetInReplyToUserId != null)
            {
                if (currentEntry.Value == (tweet.TweetInReplyToUserId.ToString()))
                {
                    sqlTable.searchTerms["direction"] = "Inbound Reply" + retweet;
                response = sqlHelper.ExecuteSqlScalar(
                $"Select count(1) FROM pbist_twitter.search_terms WHERE tweetid = '{sqlTables.search_terms["tweetid"]}'AND direction = '{sqlTables.search_terms["direction"]) AND searchterm = '{currentEntry.Key}'");
                if (response == 0)
                {
                    try
                    {
                        sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.search_terms", sqlTables.searchTerms));
                    }
                    catch (Exception e) { }
                }
                }
            }
            else if (retweet != "")
            {
                if (currentEntry.Value == (tweet.OriginalTweet.UserDetails.Id.ToString()))
                {
               sqlTable.searchTerms["direction"] = "Retweet of Outbound";
               response = sqlHelper.ExecuteSqlScalar(
                $"Select count(1) FROM pbist_twitter.search_terms WHERE tweetid = '{sqlTables.search_terms["tweetid"]}'AND direction = '{sqlTables.search_terms["direction"]) AND searchterm = '{currentEntry.Key}'");
                if (response == 0)
                {
                    try
                    {
                        sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.search_terms", sqlTables.searchTerms));
                    }
                    catch (Exception e) { }
                }                }
            }
            if (userMentions != null && userMentions.HasValues)
            {
                foreach (var usermentionItem in userMentions)
                {
                    string uid = usermentionItem.SelectToken("Id").ToString();
                    if (currentEntry.Value == (uid.ToString()))
                    {
                        sqlTable.searchTerms["direction"] = "Inbound" + retweet;
                response = sqlHelper.ExecuteSqlScalar(
                $"Select count(1) FROM pbist_twitter.search_terms WHERE tweetid = '{sqlTables.search_terms["tweetid"]}'AND direction = '{sqlTables.search_terms["direction"]) AND searchterm = '{currentEntry.Key}'");
                if (response == 0)
                {
                    try
                    {
                        sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.search_terms", sqlTables.searchTerms));
                    }
                    catch (Exception e) { }
                }                    }
                }
            }
        }

    }
