#r "Newtonsoft.Json"
#r "System.Data"

#load "SaveData.csx"
#load "Sentiment.csx"
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



    public class ParseQuery
    {
        public List<int> GetAllIndexes(string query, string tomatch)
        {
            List<int> indexes = new List<int>();

            if (String.IsNullOrEmpty(query))
                throw new ArgumentException("The query is null or empty");

            char[] specialCharacters = { '(', ')', ' ', '"' };


            foreach (Match match in Regex.Matches(query, tomatch))
            {
                bool startsOr = false;
                bool endsOr = false;
                if (match.Index != 0)
                {
                    int startingIndex = query[match.Index - 1];
                    foreach (char special in specialCharacters)
                    {
                        if (startingIndex == special)
                        {
                            startsOr = true;
                            continue;
                        }
                    }
                }
                else
                {
                    startsOr = true;
                }

                if (query.Length > match.Index + match.Length)
                {
                    int endingIndex = query[match.Index + match.Length];
                    foreach (char special in specialCharacters)
                    {
                        if (endingIndex == special)
                        {
                            endsOr = true;
                            continue;
                        }
                    }
                }
                else
                {
                    endsOr = true;
                }

                if (startsOr && endsOr)
                {
                    indexes.Add(match.Index);
                }

            }

            return indexes;

        }

        public List<string> SplitQuery(List<int> indexes, string query)
        {
            List<string> words = new List<string>();

            int length;
            int currentIndex;
            int previousItem;
            int lengthOfPrevious;

            foreach (int i in indexes)
            {
                if (query[i] == 'O')
                {
                    length = 2;
                }
                else
                {
                    length = 3;
                }


                if (query.Length == length)
                {
                    break;
                }
                else if (indexes[0] == i)
                {
                    if (i != 0)
                    {
                        words.Add(query.Substring(0, i));
                    }

                    if (indexes.Count == 1)
                    {
                        words.Add(query.Substring(i + length, query.Length - (i + length)));
                        break;
                    }
                    continue;
                }
                else if (indexes.Last() == i)
                {
                    currentIndex = indexes.IndexOf(i);
                    previousItem = indexes[currentIndex - 1];

                    if (query[previousItem] == 'O')
                    {
                        lengthOfPrevious = 2;
                    }
                    else
                    {
                        lengthOfPrevious = 3;
                    }

                    words.Add(query.Substring(previousItem + lengthOfPrevious, i - previousItem - lengthOfPrevious));

                    if (query.Length > i + length)
                    {
                        words.Add(query.Substring(i + length, query.Length - (i + length)));
                    }
                    continue;
                }
                else
                {
                    //Works out words to the left
                    currentIndex = indexes.IndexOf(i);
                    previousItem = indexes[currentIndex - 1];

                    if (query[previousItem] == 'O')
                    {
                        lengthOfPrevious = 2;
                    }
                    else
                    {
                        lengthOfPrevious = 3;
                    }

                    words.Add(query.Substring(previousItem + lengthOfPrevious, i - previousItem - lengthOfPrevious));
                }
            }
            return words;
        }

        public List<string> CleanWords(string word)
        {
            List<string> words = new List<string>();

            StringBuilder sb = new StringBuilder();

            bool isOpenQuote = false;
            bool isExclusion = false;

            foreach (char c in word)
            {
                switch (c)
                {
                    case '-':
                        isExclusion = !isExclusion;
                        break;
                    case '(':
                        break;
                    case ')':
                        if (!isExclusion)
                        {
                            if (sb.Length > 0)
                            {
                                words.Add(sb.ToString());
                                sb.Clear();
                                break;
                            }
                            break;
                        }
                        break;
                    case '"':
                        isOpenQuote = !isOpenQuote;
                        if (!isExclusion)
                        {
                            if (!isOpenQuote)
                            {
                                if (sb.Length > 0)
                                {
                                    words.Add(sb.ToString());
                                    sb.Clear();
                                }
                                break;
                            }
                        }
                        break;
                    case ' ':
                        if (isOpenQuote)
                        {
                            if (!isExclusion)
                            {
                                sb.Append(c);
                                break;
                            }
                            break;
                        }
                        if (!isExclusion)
                        {
                            if (sb.Length > 0)
                            {
                                words.Add(sb.ToString());
                                sb.Clear();
                                break;
                            }
                            break;
                        }
                        else
                        {
                            isExclusion = !isExclusion;
                            break;
                        }
                    default:
                        if (!isExclusion)
                        {
                            sb.Append(c);
                            break;
                        }
                        break;
                }
            }
            if (sb.Length > 0)
            {
                words.Add(sb.ToString());
                sb.Clear();
            }
            return words;
        }


    public async Task<Dictionary<string, string>> checkIfAccount(List<string> keywords, string connectionString)
        {
            Dictionary<string, string> words = new Dictionary<string, string>();
            SqlHelpers sqlHelper = new SqlHelpers(connectionString);
            SqlTables sqlTables = new SqlTables();

            foreach (var item in keywords)
            {
                var account = item.ToString().Trim();
                var accountTrimmed = account.Replace("@", "");

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://www.twitter.com/" + accountTrimmed);
                    client.DefaultRequestHeaders.Add("X-Push-State-Request", "true");
                    HttpResponseMessage response = await client.GetAsync("https://www.twitter.com/" + accountTrimmed);

                    var responseString = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var obj = JObject.Parse(responseString);
                        var id = obj.SelectToken("init_data")?.SelectToken("profile_user")?.SelectToken("id_str")?.ToString();
                        words.Add(item, id);
                        sqlTables.accounts["accountname"] = item;
                        sqlTables.accounts["accountid"] = id;
                        int response = 0;
                        response = sqlHelper.ExecuteSqlScalar(
                        $"Select count(1) FROM pbist_twitter.accounts WHERE accountid = '{sqlTables.accounts["accountid"]}'");
                        if (response == 0)
                        {
                            try
                            {
                                sqlHelper.ExecuteSqlNonQuery(sqlHelper.generateSQLQuery("pbist_twitter.accounts", sqlTables.accounts));
                            }
                            catch (Exception e) { }
                        }
                    }
                    else
                    {
                        words.Add(item, null);
                    }
                }
            }

            return words;
        }




    }
