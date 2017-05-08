#r "Newtonsoft.Json"
#r "System.Data"

#load "ParseQuery.csx"
#load "Sentiment.csx"
#load "SqlHelpers.csx"
#load "SqlTables.csx"
#load "TweetHandler.csx"

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


    public class SaveData
    {
        //Write original tweets into SQL
        public void SaveTweets(dynamic tweetType, SqlTables sqlTable, string connectionString)
        {
            SqlHelpers sqlHelper = new SqlHelpers(connectionString);
            sqlTable.originalTweets["masterid"] = tweetType.TweetId.ToString();
            sqlTable.processedTweets["masterid"] = tweetType.TweetId.ToString();
            sqlTable.originalTweets["tweet"] = tweetType.TweetText.ToString();
            int response = 0;
            response = sqlHelper.ExecuteSqlScalar(
                $"Select count(1) FROM pbist_twitter.tweets_normalized WHERE masterid = '{sqlTable.originalTweets["masterid"]}'");
            if (response == 0)
            {
                try
                {
                    sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.tweets_normalized", sqlTable.originalTweets));
                }
                catch (Exception e)
                {
                    //log.Info("******************Exception******************** Message:" + e);
                }
            }
        }

        //Write processed tweets into SQL
        public void SaveProcessedTweets(dynamic tweet, JObject tweetObj, SqlTables sqlTables, string connectionString)
        {
            SqlHelpers sqlHelper = new SqlHelpers(connectionString);
            sqlTables.processedTweets["tweetid"] = tweet.TweetId;
            //Save time metadata about processed tweets
            string createdat = tweet.CreatedAt.ToString();
            DateTime ts = DateTime.ParseExact(createdat, "ddd MMM dd HH:mm:ss +ffff yyyy", CultureInfo.CurrentCulture);
            sqlTables.processedTweets["dateorig"] = DateTime.Parse(ts.Year.ToString() + " " + ts.Month.ToString() + " " + ts.Day.ToString() + " " + ts.Hour.ToString() + ":" + ts.Minute.ToString() + ":" + ts.Second.ToString()).ToString(CultureInfo.InvariantCulture);
            sqlTables.processedTweets["minuteofdate"] = DateTime.Parse(ts.Year.ToString() + " " + ts.Month.ToString() + " " + ts.Day.ToString() + " " + ts.Hour.ToString() + ":" + ts.Minute.ToString() + ":00").ToString(CultureInfo.InvariantCulture);
            sqlTables.processedTweets["hourofdate"] = DateTime.Parse(ts.Year.ToString() + " " + ts.Month.ToString() + " " + ts.Day.ToString() + " " + ts.Hour.ToString() + ":00:00").ToString(CultureInfo.InvariantCulture);

            //Save media and follower metadata about processed tweets
            sqlTables.processedTweets["authorimage_url"] = tweet.UserDetails.ProfileImageUrl;
            sqlTables.processedTweets["username"] = tweet.UserDetails.UserName;
            sqlTables.processedTweets["user_followers"] = tweet.UserDetails.FollowersCount;
            sqlTables.processedTweets["user_friends"] = tweet.UserDetails.FavouritesCount;
            sqlTables.processedTweets["user_favorites"] = tweet.UserDetails.FriendsCount;
            sqlTables.processedTweets["user_totaltweets"] = tweet.UserDetails.StatusesCount;

            string firstUrl = String.Empty;

            if (tweetObj.SelectToken("MediaUrls") != null && tweetObj.SelectToken("MediaUrls").HasValues)
            {
                firstUrl = tweet.MediaUrls[0];
                if (firstUrl != String.Empty)
                {
                    sqlTables.processedTweets["image_url"] = firstUrl;
                }
            }

            if (tweet.favorited != "true")
            {
                sqlTables.processedTweets["favorited"] = "1";
            }

            if (tweet.OriginalTweet != null)
            {
                sqlTables.processedTweets["retweet"] = "True";
            }

            //Save processed tweets into SQL
            int response = 0;
            response = sqlHelper.ExecuteSqlScalar(
                $"Select count(1) FROM pbist_twitter.tweets_processed WHERE tweetid = '{sqlTables.processedTweets["tweetid"]}'");
            if (response == 0)
            {
                try
                {
                    sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.tweets_processed", sqlTables.processedTweets));
                }
                catch (Exception e) { }
            }

        }

        public void HashtagMentions(string text, char delimiter, string field, string sqlTable, Dictionary<string, string> dictionary, string connectionString)
        {
            SqlHelpers sqlHelper = new SqlHelpers(connectionString);
            var regex = new Regex(@"(?<=" + delimiter + @")\w+");
            var matches = regex.Matches(text);
            foreach (Match match in matches)
            {
                dictionary[field] = match.ToString();
                sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery(sqlTable, dictionary));
            }
        }

    }

