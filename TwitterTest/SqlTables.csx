#r "Newtonsoft.Json"
#r "System.Data"

#load "ParseQuery.csx"
#load "SaveData.csx"
#load "Sentiment.csx"
#load "SqlHelpers.csx"
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


    public class SqlTables
    {
        //Create dictionaries for SQL tables
        public Dictionary<string, string> originalTweets = new Dictionary<string, string>()
        {
            {"masterid", null},
            {"tweet", null},
            {"twitterhandle", null},
            {"sentiment", null},
            {"lang", null},
            {"sentimentBin", null},
            {"sentimentPosNeg", null},
            {"accounttag", "Unknown"}
        };

        public Dictionary<string, string> processedTweets = new Dictionary<string, string>()
        {
            {"tweetid", null},
            {"masterid", null},
            {"image_url", null},
            {"dateorig", null},
            {"authorimage_url", null},
            {"username", null},
            {"hourofdate", null},
            {"minuteofdate", null},
            {"direction", null},
            {"favorited", "1"},
            {"retweet", "False"},
            {"user_followers", null},
            {"user_friends", null},
            {"user_favorites", null},
            {"user_totaltweets", null}
        };

        public Dictionary<string, string> hashtagSlicer = new Dictionary<string, string>()
        {
            {"tweetid", null},
            {"facet", null}
        };

        public Dictionary<string, string> mentionSlicer = new Dictionary<string, string>()
        {
            {"tweetid", null},
            {"facet", null}
        };

        public Dictionary<string, string> authorHashtagGraph = new Dictionary<string, string>()
        {
            {"tweetid", null},
            {"author", null},
            {"authorColor", "#01B8AA"},
            {"hashtag", null},
            {"hashtagColor", "#374649"},
        };

        public Dictionary<string, string> authorMentionGraph = new Dictionary<string, string>()
        {
            {"tweetid", null},
            {"author", null},
            {"authorColor", "#01B8AA"},
            {"mention", null},
            {"mentionColor", "#374649"},
        };

        public Dictionary<string, string> searchTerms = new Dictionary<string, string>()
        {
            {"tweetid", null},
            {"searchterm", null},
            {"accountid", null},
            {"direction", null} };
    }
