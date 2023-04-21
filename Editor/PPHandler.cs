using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        public Func<string, string> Func;
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
        public static void Subscribe(Func<string, string> func, int priority = 1, string name = "")
        {
            // I don't like doing these checks
            if (func == null)
            {
                Debug.LogError("Function was null");
                return;
            }
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Name was null or empty, please specify a name");
                return;
            }
            if (PreProcessors.Any(s => s.Func == func))
            {
                Debug.LogError($"PreProcessor: {name} was already subscribed");
                return;
            }

            var subscriber = new PPSubscriber
            {
                Name = name,
                Priority = priority,
                Func = func
            };
            
            PreProcessors.Add(subscriber);

            // Sort the PreProcessors list by priority.
            PreProcessors.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        }

        public static string Parse(string program)
        {
            // Loop through every PreProcessor and call their Parse function
            foreach (var PPs in PreProcessors)
            {
                Debug.Log("Running: " + PPs.Name);
                try
                {
                    var tempProg = PPs.Func(program);
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