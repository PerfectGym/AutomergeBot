using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json.Linq;

namespace PerfectGym.AutomergeBot.Models
{
    public class InfoModelBase
    {
        protected static TValue SafeGet<TValue>(JObject jObject, string path)
        {
            var props = path.Split('.');

            JObject current = jObject;
            JToken lastJToken = null;

            foreach (var prop in props)
            {
                if (current != null && current.TryGetValue(prop, out lastJToken))
                {
                    current = lastJToken as JObject;
                }
                else
                {
                    return default(TValue);
                }
            }

            return lastJToken.Value<TValue>();
        }


        protected static List<TValue> SafeGetList<TValue>(JObject jObject, string path) 
        {
            var result = new List<TValue>();

            var props = path.Split('.');

            JObject current = jObject;
            JToken lastJToken = null;

            var pastPath = "";

            foreach (var prop in props)
            {

                pastPath += "."+prop;

                if (current != null && current.TryGetValue(prop, out lastJToken))
                {
                    if (lastJToken is JArray)
                    {
                        var jarray = (JArray)lastJToken;
                        foreach (var obj in jarray.Children())
                        {
                            result.Add(InfoModelBase.SafeGet<TValue>((JObject)obj, path.Substring(pastPath.Length)));
                        }
                    }

                    current = lastJToken as JObject;
                }
            }

            return result;
        }

      



    }
}