using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FreshTweets
{
    class DirectionChecks
    {
        //Figure out if hashtag is in tweet
        public void HashtagDirectionCheck(KeyValuePair<string, string> currentEntry, SqlTables sqlTable, dynamic tweet, string connectionString, string retweet = "")
        {
            SqlHelpers sqlHelper = new SqlHelpers(connectionString);
            string tweetText = tweet.TweetText.ToString();
            bool result = Regex.IsMatch(tweetText, @"\\b" + currentEntry.Key + @"\\b", RegexOptions.IgnoreCase);
            if(result == true)
            {
                sqlTable.searchTerms["direction"] = "Text" + retweet;
                sqlTable.searchTerms["tweetid"] = tweet.TweetId;
                sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.searchTerms", sqlTable.searchTerms));
            }
            string hashtag = "#" + currentEntry.Key.ToLower();
            if (tweetText.ToLower().Contains(hashtag))
            {
                sqlTable.searchTerms["direction"] = "Hashtag" + retweet;
                sqlTable.searchTerms["tweetid"] = tweet.TweetId;
                sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.searchTerms", sqlTable.searchTerms));
            }
        }

        //Figure out direction of tweet
        public void MessageDirectionCheck(KeyValuePair<string, string> currentEntry, JToken userMentions, SqlTables sqlTable, dynamic tweet, string connectionString, string retweet = "")
        {
            SqlHelpers sqlHelper = new SqlHelpers(connectionString);
            if (currentEntry.Value == (tweet.UserDetails.Id.ToString()))
            {
                sqlTable.searchTerms["direction"] = "Outbound" + retweet;
                if (tweet.TweetInReplyToUserId != null)
                {
                    sqlTable.searchTerms["direction"] = "Outbound Reply" + retweet;
                }
                sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.searchTerms", sqlTable.searchTerms));
            }
            else if (tweet.TweetInReplyToUserId != null)
            {
                if (currentEntry.Value == (tweet.TweetInReplyToUserId.ToString()))
                {
                    sqlTable.searchTerms["direction"] = "Inbound Reply" + retweet;
                    sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.searchTerms", sqlTable.searchTerms));
                }
            }
            else if (retweet != "")
            {
                if (currentEntry.Value == (tweet.OriginalTweet.UserDetails.Id.ToString()))
                {
                    sqlTable.searchTerms["direction"] = "Retweet of Outbound";
                    sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.searchTerms", sqlTable.searchTerms));
                }
            }
            if (userMentions != null && userMentions.HasValues)
            {
                foreach (var usermentionItem in userMentions)
                {
                    string uid = usermentionItem.SelectToken("Id").ToString();
                    if (currentEntry.Value == (uid.ToString()))
                    {
                        sqlTable.searchTerms["direction"] = "Inbound" + retweet;
                        sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.searchTerms", sqlTable.searchTerms));
                    }
                }
            }
        }

    }
}
