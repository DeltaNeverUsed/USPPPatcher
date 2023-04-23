using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using USPPPatcher.Helpers;

namespace USPPPatcher.Editor
{
    public class PPSubscriber
    {
        /// <summary>
        /// Name of the Subscriber.
        /// </summary>
        public string Name;
        /// <summary>
        /// Determines which PreProcessor will run before others. (Higher is earlier)
        /// </summary>
        public int Priority;
        
        /// <summary>
        /// The Function to call to hand the program off the to PreProcessor.
        /// </summary>
        public Func<string, PPInfo, string> Func;

        public bool UsesAnalyzer;
    }

    public class PPInfo
    {
        /// <summary>
        /// the provided Analyze variable
        /// </summary>
        public Analyzer Analyzer;
    }
    
    public static class PPHandler
    {
        private static List<PPSubscriber> PreProcessors = new List<PPSubscriber>();

        /// <summary>
        /// Subscribes your PreProcessor to the patcher.
        /// </summary>
        /// <param name="func">The "Parse" function of your PreProcessor.</param>
        /// <param name="priority">Determines which PreProcessor will run before others. (Higher is earlier)</param>
        /// <param name="name">The Name of your PreProcessor.</param>
        /// <param name="usesAnalyzer">Enables or disables the use of the built in Analyzer</param>
        public static PPSubscriber Subscribe(Func<string, PPInfo, string> func, int priority = 1, string name = "", bool usesAnalyzer = false)
        {
            // I don't like doing these checks
            if (func == null)
            {
                Debug.LogError("Function was null");
                return null;
            }
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Name was null or empty, please specify a name");
                return null;
            }
            if (PreProcessors.Any(s => s.Func == func))
            {
                Debug.LogError($"PreProcessor: {name} was already subscribed");
                return null;
            }

            Patcher.UseAnalyzer |= usesAnalyzer;
            
            var subscriber = new PPSubscriber
            {
                Name = name,
                Priority = priority,
                Func = func,
                UsesAnalyzer = usesAnalyzer
            };
            
            PreProcessors.Add(subscriber);

            // Sort the PreProcessors list by priority.
            PreProcessors.Sort((x, y) => y.Priority.CompareTo(x.Priority));

            return subscriber;
        }

        public static string Parse(string program, ref Analyzer analyzer)
        {
            // Loop through every PreProcessor and call their Parse function
            foreach (var PPs in PreProcessors)
            {
                //Debug.Log("Running: " + PPs.Name);
                try
                {
                    var info = new PPInfo
                    {
                        Analyzer = analyzer
                    };
                    
                    var tempProg = PPs.Func(program, info);
                    if (string.IsNullOrWhiteSpace(tempProg))
                    {
                        Debug.LogError($"PreProcessor: {PPs.Name} Returned no program, return was empty");
                        continue;
                    }

                    program = tempProg;
                }
                catch (Exception e)
                {
                    Debug.LogError($"PreProcessor: {PPs.Name} Produced error: "+e);
                    throw;
                }
            }

            return program;
        }
    }
}