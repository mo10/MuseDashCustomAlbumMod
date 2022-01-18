﻿using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using UnhollowerBaseLib;

namespace CustomAlbums
{
    public static class Utils
    {
        unsafe public static IntPtr NativeMethod(Type type, string name, Type[] parameters = null, Type[] generics = null)
        {
            var method = AccessTools.Method(type, name, parameters, generics);

            var methodPtr = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method)
                .GetValue(null);
            return methodPtr;
        }
        /// <summary>
        /// Read embedded file from this assembly.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static byte[] ReadEmbeddedFile(string file)
        {
            var assembly = Assembly.GetExecutingAssembly();
            byte[] buffer;
            using (var stream = assembly.GetManifestResourceStream($"{Assembly.GetExecutingAssembly().GetName().Name}.{file}"))
            {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }
            return buffer;
        }
        /// <summary>
        /// Load json from stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="steamReader"></param>
        /// <returns></returns>
        public static T JsonDeserialize<T>(this Stream steamReader)
        {
            var buffer = new byte[steamReader.Length];
            steamReader.Read(buffer, 0, buffer.Length);
            return JsonConvert.DeserializeObject<T>(Encoding.Default.GetString(buffer));
        }
        /// <summary>
        /// Load json from string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <returns></returns>
        public static T JsonDeserialize<T>(this string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }
        public static JObject ToJObject(this Il2CppSystem.Object o)
        {
            JToken token;
            Newtonsoft.Json.JsonSerializer jsonSerializer = Newtonsoft.Json.JsonSerializer.CreateDefault();
            JTokenWriter jtokenWriter = new JTokenWriter();
            jsonSerializer.Serialize(jtokenWriter, o);
            token = jtokenWriter.Token;
            return (JObject)token;
        }
        /// <summary>
        /// Convert a object to json string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string JsonSerialize(this Il2CppSystem.Object obj)
        {
            var settings = new JsonSerializerSettings();
            settings._formatting = new Il2CppSystem.Nullable<Formatting>(Formatting.Indented);
            return JsonConvert.SerializeObject(obj, settings);
        }
        public static string JsonSerialize<T>(this T obj) where T : JsonNode
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            return System.Text.Json.JsonSerializer.Serialize(obj, options);
        }
        /// <summary>
        /// Get the specified non-public type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Type GetNestedNonPublicType(this Type type, string name)
        {
            return type.GetNestedType(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }
        public static string RemoveFromEnd(this string str, IEnumerable<string> suffixes)
        {
            foreach (var suffix in suffixes)
            {
                if (str.EndsWith(suffix))
                {
                    return str.Substring(0, str.Length - suffix.Length);
                }
            }
            return str;
        }
        public static string RemoveFromEnd(this string str, string suffix)
        {
            if (str.EndsWith(suffix))
            {
                return str.Substring(0, str.Length - suffix.Length);
            }
            return str;
        }
        public static string RemoveFromStart(this string str, IEnumerable<string> suffixes)
        {
            foreach (var suffix in suffixes)
            {
                if (str.StartsWith(suffix))
                {
                    return str.Substring(suffix.Length);
                }
            }
            return str;
        }
        public static string RemoveFromStart(this string str, string suffix)
        {
            if (str.StartsWith(suffix))
            {
                return str.Substring(suffix.Length);
            }
            return str;
        }
        /// <summary>
        /// Read all bytes from a Stream.
        /// </summary>
        /// <param name="steamReader"></param>
        /// <returns></returns>
        public static byte[] ToArray(this Stream steamReader)
        {
            var buffer = new byte[steamReader.Length];
            steamReader.Read(buffer, 0, buffer.Length);
            return buffer;
        }
        /// <summary>
        /// Put all bytes into Stream.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static Il2CppSystem.IO.MemoryStream ToIl2CppStream(this byte[] bytes)
        {
            return new Il2CppSystem.IO.MemoryStream(bytes);
        }
        public static System.IO.MemoryStream ToStream(this byte[] bytes)
        {
            return new System.IO.MemoryStream(bytes);
        }
        public static string ToString(this IEnumerable<byte> bytes, string format)
        {
            string result = string.Empty;
            foreach (var _byte in bytes)
            {
                result += _byte.ToString(format);
            }
            return result;
        }
        public static byte[] GetMD5(this IEnumerable<byte> bytes)
        {
            return MD5.Create().ComputeHash(bytes.ToArray());
        }
        /// <summary>
        /// Search all implementation classes of interfce
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllImplement(this Type type)
        {
            var classes = Assembly.GetAssembly(type).GetTypes().Where(t =>
                t.GetInterfaces().Contains(type) && t.GetConstructor(Type.EmptyTypes) != null);

            return classes;
        }
        public static IEnumerable<Type> GetAllSublass(this Type type)
        {
            var classes = Assembly.GetAssembly(type).GetTypes().Where(
                t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(type));

            return classes;
        }
    }
}