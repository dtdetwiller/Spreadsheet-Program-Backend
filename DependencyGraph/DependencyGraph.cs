// Skeleton implementation written by Joe Zachary for CS 3500, September 2013.
// Version 1.1 (Fixed error in comment for RemoveDependency.)
// Version 1.2 - Daniel Kopta 
//               (Clarified meaning of dependent and dependee.)
//               (Clarified names in solution/project structure.)
// Completely implemented by Daniel Detwiller

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpreadsheetUtilities
{

    /// <summary>
    /// (s1,t1) is an ordered pair of strings
    /// t1 depends on s1; s1 must be evaluated before t1
    /// 
    /// A DependencyGraph can be modeled as a set of ordered pairs of strings.  Two ordered pairs
    /// (s1,t1) and (s2,t2) are considered equal if and only if s1 equals s2 and t1 equals t2.
    /// Recall that sets never contain duplicates.  If an attempt is made to add an element to a 
    /// set, and the element is already in the set, the set remains unchanged.
    /// 
    /// Given a DependencyGraph DG:
    /// 
    ///    (1) If s is a string, the set of all strings t such that (s,t) is in DG is called dependents(s).
    ///        (The set of things that depend on s)    
    ///        
    ///    (2) If s is a string, the set of all strings t such that (t,s) is in DG is called dependees(s).
    ///        (The set of things that s depends on) 
    //
    // For example, suppose DG = {("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}
    //     dependents("a") = {"b", "c"}
    //     dependents("b") = {"d"}
    //     dependents("c") = {}
    //     dependents("d") = {"d"}
    //     dependees("a") = {}
    //     dependees("b") = {"a"}
    //     dependees("c") = {"a"}
    //     dependees("d") = {"b", "d"}
    /// </summary>
    public class DependencyGraph
    {
        /// <summary>
        /// The number of ordered pairs in the dependency graph.
        /// </summary>
        private int size;
        /// <summary>
        /// The mapping of a string to a list of its dependents.
        /// </summary>
        private Dictionary<string, List<string>> dependents;
        /// <summary>
        /// The mapping of a string to a list of its dependees.
        /// </summary>
        private Dictionary<string, List<string>> dependees;

        /// <summary>
        /// Creates an empty DependencyGraph. I used two dictionarys with the <Key, Value> as <string, List<string> 
        /// to represent the dependents and dependees. 
        /// </summary>
        public DependencyGraph()
        {
            dependents = new Dictionary<string, List<string>>();
            dependees = new Dictionary<string, List<string>>();
            size = 0;
        }


        /// <summary>
        /// The number of ordered pairs in the DependencyGraph.
        /// </summary>
        public int Size
        {
            get { return size; }
        }


        /// <summary>
        /// The size of dependees(s).
        /// This property is an example of an indexer.  If dg is a DependencyGraph, you would
        /// invoke it like this:
        /// dg["a"]
        /// It should return the size of dependees("a")
        /// </summary>
        public int this[string s]
        {
            get {
                if (!dependees.ContainsKey(s))
                    return 0;
                return dependees[s].Count;
            }
        }


        /// <summary>
        /// Reports whether dependents(s) is non-empty.
        /// </summary>
        public bool HasDependents(string s)
        {
            if (!dependents.ContainsKey(s))
                return false;
            return dependents[s].Count > 0;
        }


        /// <summary>
        /// Reports whether dependees(s) is non-empty.
        /// </summary>
        public bool HasDependees(string s)
        {
            if (!dependees.ContainsKey(s))
                return false;
            return dependees[s].Count > 0;
        }


        /// <summary>
        /// Enumerates dependents(s).
        /// </summary>
        public IEnumerable<string> GetDependents(string s)
        {
            if (!dependents.ContainsKey(s))
                return new List<string>();
            return dependents[s];
        }

        /// <summary>
        /// Enumerates dependees(s).
        /// </summary>
        public IEnumerable<string> GetDependees(string s)
        {
            if (!dependees.ContainsKey(s))
                return new List<string>();
            return dependees[s];
        }


        /// <summary>
        /// <para>Adds the ordered pair (s,t), if it doesn't exist</para>
        /// 
        /// <para>This should be thought of as:</para>   
        /// 
        ///   t depends on s
        ///
        /// </summary>
        /// <param name="s"> s must be evaluated first. T depends on S</param>
        /// <param name="t"> t cannot be evaluated until s is</param>        /// 
        public void AddDependency(string s, string t)
        {
            List<string> dents = new List<string>();
            List<string> dees = new List<string>();

            // If s is already in the graph, but does not have t as a dependent.
            if (dependents.ContainsKey(s) && !dependents[s].Contains(t))
            {
                dependents[s].Add(t);
                if (dependees.ContainsKey(t))
                {
                    dependees[t].Add(s);
                }
                else
                {
                    dees.Add(s);
                    dependees.Add(t, dees);
                }
                size++;
            }
            // If s is not in the graph.
            else if (!dependents.ContainsKey(s))
            {
                dents.Add(t);
                dependents.Add(s, dents);
                if (dependees.ContainsKey(t))
                {
                    if (!dependees[t].Contains(s))
                        dependees[t].Add(s);
                }
                else
                {
                    dees.Add(s);
                    dependees.Add(t, dees);
                }
                size++;
            }

            // Makes an empty dependees list for s if it doesn't have one.
            if (!dependees.ContainsKey(s))
                dependees.Add(s, new List<string>());
            // Makes an empyty dependents list for t if it doesn't have one.
            if (!dependents.ContainsKey(t))
                dependents.Add(t, new List<string>());
        }


        /// <summary>
        /// Removes the ordered pair (s,t), if it exists
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public void RemoveDependency(string s, string t)
        {
            if (dependents.ContainsKey(s) && dependents[s].Contains(t))
            {
                dependents[s].Remove(t);
                if (dependees.ContainsKey(t) && dependees[t].Contains(s))
                    dependees[t].Remove(s);
                size--;
            }
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (s,r).  Then, for each
        /// t in newDependents, adds the ordered pair (s,t).
        /// </summary>
        public void ReplaceDependents(string s, IEnumerable<string> newDependents)
        {
            if (dependents.ContainsKey(s))
            {
                // Makes a copy of the dependents list.
                List<string> copy = new List<string>(dependents[s]);
                foreach (string oldDents in copy)
                    RemoveDependency(s, oldDents);
                foreach (string t in newDependents)
                    AddDependency(s, t);
            }
            else
            {
                foreach (string t in newDependents)
                    AddDependency(s, t);
            }
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (r,s).  Then, for each 
        /// t in newDependees, adds the ordered pair (t,s).
        /// </summary>
        public void ReplaceDependees(string s, IEnumerable<string> newDependees)
        {
            if (dependees.ContainsKey(s))
            {
                // Makes a copy of the dependees list.
                List<string> copy = new List<string>(dependees[s]);
                foreach (string oldDees in copy)
                    RemoveDependency(oldDees, s);
                foreach (string t in newDependees)
                    AddDependency(t, s);
            }
            else
            {
                foreach (string t in newDependees)
                    AddDependency(t, s);
            }
        }

    }

}
