﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tweezers.Schema.Common
{
    public static class ExtensionMethods
    {
        public static bool In<T>(this T obj, IEnumerable<T> collection)
        {
            return collection.Contains(obj);
        }

        public static string ToArrayString<T>(this IEnumerable<T> collection, string delimiter = ", ")
        {
            return string.Join(delimiter, collection);
        }

        public static JObject Just(this JObject obj, params string[] fields)
        {
            return obj.Just(fields.ToList());
        }

        public static JObject Without(this JObject obj, params string[] fields)
        {
            JObject newObj = JObject.FromObject(obj.DeepClone());

            foreach (string field in fields)
            {
                newObj.Remove(field);
            }

            return newObj;
        }

        public static JObject Just(this JObject obj, IEnumerable<string> fields)
        {
            JObject newObj = new JObject();

            foreach (string field in fields)
            {
                if (obj[field] != null)
                    newObj[field] = obj[field];
            }

            return newObj;
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> func)
        {
            foreach (T element in collection)
            {
                func.Invoke(element);
            }
        }

        public static void ForEachWithIndex<T>(this IEnumerable<T> collection, Action<T, int> func)
        {
            int i = 0;
            foreach (T element in collection)
            {
                func.Invoke(element, i);
                i++;
            }
        }

        public static TimeSpan Hours(this int i)
        {
            return TimeSpan.FromHours(i);
        }

        public static T ToStrongType<T>(this JObject jObject)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(jObject));
        }

        public static T DeepClone<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }
    }
}
