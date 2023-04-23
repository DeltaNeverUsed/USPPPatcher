using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = System.Random;

namespace USPPPatcher.Helpers
{
    public class Analyzer
    {
        private List<Variable> _vars = new List<Variable>();
        private List<Function> _funcs = new List<Function>();
        private List<Class> _classes = new List<Class>();

        private List<VarSpace> _spaces = new List<VarSpace>();
        
        private string _program;
        
        private Random _rand = new Random();

        public void Analyze(string program)
        {
            _program = program;
            RemoveComments();

            GetClasses();

            foreach (var c in _classes)
            {
                GetFunctions(c.Index, c);
                GetVariables(c.Index, c, c.ParentSpace);
            }

            foreach (var func in _funcs)
            {
                GetVariables(func.Index, func, func.ParentSpace);
            }
        }
        
        public void OffsetEverything(int start, string original, string newString)
        {
            OffsetEverything(start, newString.Length - original.Length);
        }
        public void OffsetEverything(int start, int original, string newString)
        {
            OffsetEverything(start, newString.Length - original);
        }
        public void OffsetEverything(int start, int offset)
        {
            _funcs.ForEach(x => x.Index += x.Index >= start ? offset : 0);
            _classes.ForEach(x => x.Index += x.Index >= start ? offset : 0);
            _spaces.ForEach(x => x.Index += x.Index >= start ? offset : 0);

            foreach (var v in _vars)
            {
                v.Index += v.Index >= start ? offset : 0;
                for (var index = 0; index < v.Uses.Count; index++)
                {
                    v.Uses[index] += v.Uses[index] >= start ? offset : 0;
                }
            }
        }

        /// <summary>
        /// Get space by index
        /// </summary>
        /// <param name="index">Index in program</param>
        /// <returns>The variable space</returns>
        public VarSpace GetSpace(int index)
        {
            var valid = _spaces.Where(s => s.Index >= index);
            return !valid.Any() ? null : valid.Min();
        }
        /// <summary>
        /// Get space by name
        /// </summary>
        /// <param name="name">Name of class/function in program</param>
        /// <returns>The variable space</returns>
        public VarSpace GetSpace(string name)
        {
            var valid = _classes.Where(c => c.Name == name);
            if (valid.Any())
                return valid.First();
            var valid2 = _funcs.Where(f => f.Name == name);
            return valid2.Any() ? valid2.First() : null;
        }

        /// <summary>
        /// Gets a variable by name in a space and all parents spaces
        /// </summary>
        /// <param name="varName">Name of the variable</param>
        /// <param name="varSpace">The space to start the search in</param>
        /// <returns>The variable</returns>
        public Variable GetVariableInSpace(string varName, VarSpace varSpace)
        {
            var vars = _vars.Where(v => v.Name == varName && v.SpaceId == varSpace.SpaceId);
            if (vars.Any())
                return vars.First();
            if (varSpace.ParentSpace != null)
                return GetVariableInSpace(varName, varSpace.ParentSpace);
            return null;
        }
        /// <summary>
        /// Gets a variable by name in all spaces
        /// </summary>
        /// <param name="varName">Name of the variable</param>
        /// <returns>The variable</returns>
        public Variable GetVariableInSpace(string varName)
        {
            var vars = _vars.Where(v => v.Name == varName);
            if (vars.Any())
                return vars.First();
            return null;
        }
        
        /// <summary>
        /// Gets a variable by regex type in a space and all parents spaces
        /// </summary>
        /// <param name="varType">regex thingy</param>
        /// <param name="varSpace">The space to start the search in</param>
        /// <returns>The variable</returns>
        public Variable[] GetVariablesInSpaceByType(string varType, VarSpace varSpace)
        {
            var vars = _vars.Where(v => v.SpaceId == varSpace.SpaceId && Regex.IsMatch(v.Type, varType));
            Variable[] temp = new Variable[] { };
            var nn = varSpace.ParentSpace != null;
            if (nn)
                temp = GetVariablesInSpaceByType(varType, varSpace.ParentSpace);
            if (vars.Any())
                return nn ? vars.ToArray().Concat(temp).ToArray() : vars.ToArray();
            return null;
        }
        /// <summary>
        /// Gets a variable by regex type in all spaces
        /// </summary>
        /// <param name="varType">regex thingy</param>
        /// <returns>The variable</returns>
        public Variable[] GetVariablesInSpaceByType(string varType)
        {
            var vars = _vars.Where(v => Regex.IsMatch(v.Type, varType));
            if (vars.Any())
                return vars.ToArray();
            return null;
        }

        // Internal functions

        private string GetSubStrInsideCBrack(ref int StartIndex, out int len)
        {
            var bracks = 1;
            StartIndex = _program.IndexOf('{', StartIndex);
            var index = StartIndex+1;
            len = StartIndex;

            var its = 0;

            while (bracks != 0)
            {
                var open = _program.IndexOf('{', index);
                var close = _program.IndexOf('}', index);
                
                if (close == -1)
                    break;
                if (open == -1)
                    open = int.MaxValue;

                if (open < close)
                {
                    bracks++;
                    len = open - StartIndex;
                    index = open+1;
                }
                else
                {
                    bracks--;
                    len = close - StartIndex;
                    index = close+1;
                }
                
                // Make sure unity doesn't lockup
                if (its > 200)
                    break;
                
                its++;
            }
            return _program.Substring(StartIndex, len+1);
        }

        private void RemoveComments()
        {
            // https://stackoverflow.com/questions/3524317/regex-to-strip-line-comments-from-c-sharp/9119583#9119583
            var re = @"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|/\*(?s:.*?)\*/";
            
            var text = Regex.Matches(_program, re);
            foreach (Match match in text)
            {
                var fChar = _program[match.Index];
                var sChar = _program[match.Index+1];
                if (fChar == '\'' || fChar == '"')
                {
                    _program = _program.Remove(match.Index+1, match.Length-2)
                        .Insert(match.Index, "".PadRight(match.Length-2));
                    return;
                }

                if (sChar == '\'' || sChar == '"')
                {
                    _program = _program.Remove(match.Index+2, match.Length-3)
                        .Insert(match.Index, "".PadRight(match.Length-3));
                    return;
                }
                    

                _program = _program.Remove(match.Index, match.Length).Insert(match.Index, "".PadRight(match.Length));
            }
        }

        private void GetClasses()
        {
            var classes = Regex.Matches(_program, "class\\s(?:[\\w\\d]\\w+)+");

            foreach (Match Class in classes)
            {
                
                var c = new Class
                {
                    Name = Class.Value.Substring(6),
                    Index = Class.Index,
                    SpaceId = _rand.Next(),
                    ParentSpace = null
                };

                var s = new VarSpace
                {
                    Index = c.Index,
                    SpaceId = c.SpaceId,
                    ParentSpace = c.ParentSpace
                };
                    
                _spaces.Add(s);
                _classes.Add(c);
            }
        }

        private void GetFunctions(int index, VarSpace parentSpace)
        {
            var curr = GetSubStrInsideCBrack(ref index, out _);

            var re = @"\b((public|private|protected|internal)\s+)?(static\s+)?([A-Za-z0-9]+\s)([A-Za-z0-9]+)\([A-Za-z0-9,\s]*\)?(?=\s*\{)";

            var matches = Regex.Matches(curr, re);
            
            foreach (Match m in matches)
            {
                var argsStart = curr.IndexOf("(", m.Index, StringComparison.Ordinal)+1;
                var argsEnd = curr.IndexOf(")", argsStart, StringComparison.Ordinal);
                
                //var argsSubstring = curr.Substring(argsStart, argsEnd - argsStart);

                // TODO: make it fill out the Type parameters in the Function class

                var lastIndexOf = curr.LastIndexOf(' ', argsStart - 1);
                var lastIndexOf2 = curr.LastIndexOf(' ', lastIndexOf - 1);
                var func = new Function
                {
                    Name = curr.Substring(lastIndexOf+1, argsStart - lastIndexOf - 2),
                    Index = m.Index + index,
                    
                    ReturnType = curr.Substring(lastIndexOf2+1, lastIndexOf - lastIndexOf2 - 1),
                    
                    SpaceId = _rand.Next(),
                    ParentSpace = parentSpace
                };
                
                var s = new VarSpace
                {
                    Index = func.Index,
                    SpaceId = func.SpaceId,
                    ParentSpace = func.ParentSpace
                };
                    
                _spaces.Add(s);
                _funcs.Add(func);
            }
        }

        private void GetVariables(int index, VarSpace space, VarSpace parentSpace)
        {
            var curr = GetSubStrInsideCBrack(ref index, out _);
            curr = curr.Substring(1, curr.Length - 2);
            var currOG = curr;
            index++;
            
            var re = @"\{(?:[^{}]*(?:\{(?<Depth>)|\}(?<-Depth>))*(?(Depth)(?!)))?\}";
            var matches = Regex.Matches(curr, re);
            foreach (Match m in matches)
            {
                curr = curr.Remove(m.Index+1, m.Length-2).Insert(m.Index+1, "".PadRight(m.Length-2));
            }
            
            re = @"\b(?<type>[\w<>]+)\s+(?<name>\w+)\s*(?<assignment>=\s*.+)?\s*;";
            matches = Regex.Matches(curr, re);
            foreach (Match m in matches)
            {
                var variableType = m.Groups["type"].Value;
                var variableName = m.Groups["name"].Value;
                var variableAssignment = m.Groups["assignment"].Value.TrimStart('=').Trim();
                
                if (variableType == "return" || string.IsNullOrWhiteSpace(variableAssignment))
                    continue;

                if (variableAssignment.StartsWith("new "))
                {
                    variableType = variableAssignment.Substring(4, variableAssignment.IndexOf('(') - 4);
                } else if (variableType == "var")
                {
                    var l = variableAssignment.IndexOf('(');
                    if (l < 1)
                        continue;
                    var func = _funcs.FindIndex(f => f.Name == variableAssignment.Substring(0, l));
                    if (func == -1)
                        continue;
                    
                    variableType = _funcs[func].ReturnType;
                }
                var variable = new Variable
                {
                    Name = variableName,
                    Type = variableType,
                    
                    Index = m.Index + index,
                    
                    SpaceId = space.SpaceId,
                    ParentSpace = parentSpace
                };

                var mas = Regex.Matches(currOG, "\\b(list+)");
                foreach (Match m2 in mas)
                {
                    variable.Uses.Add(index + m2.Index);
                }

                _vars.Add(variable);
            }
        }

    }

    public class Variable : Node
    {
        public string Type;
        public List<int> Uses = new List<int>();
    }
    
    public class Function : Node
    {
        public string ReturnType;
        public string[] ParamTypes;
    }
    
    public class FunctionCall : Node
    {
        public string ReturnType;
        public string[] ParamTypes;
    }
    
    public class Class : Node
    {
    }

    public class Node : VarSpace
    {
        public string Name;
    }

    public class VarSpace
    {
        public int Index;
        
        public int SpaceId;
        public VarSpace ParentSpace;
    }
}
