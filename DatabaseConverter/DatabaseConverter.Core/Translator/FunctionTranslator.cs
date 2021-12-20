﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using DatabaseConverter.Model;

using DatabaseInterpreter.Core;
using DatabaseInterpreter.Model;

using SqlAnalyser.Model;

namespace DatabaseConverter.Core
{
    public class FunctionTranslator : DbObjectTranslator
    {
        private List<TokenInfo> functions;
        private DatabaseType sourceDbType;
        private DatabaseType targetDbType;

        private Regex parenthesesRegex = new Regex(@"\(.*\)");
        private Regex nameRegex = new Regex(@"\b([a-zA-Z]+)\b", RegexOptions.IgnoreCase);

        private List<FunctionSpecification> sourceFuncSpecs;
        private List<FunctionSpecification> targetFuncSpecs;

        public FunctionTranslator(DbInterpreter sourceInterpreter, DbInterpreter targetInterpreter, List<TokenInfo> functions) : base(sourceInterpreter, targetInterpreter)
        {
            this.functions = functions;
            this.sourceDbType = sourceInterpreter.DatabaseType;
            this.targetDbType = targetInterpreter.DatabaseType;
        }

        public override void Translate()
        {
            if (this.sourceDbType == this.targetDbType)
            {
                return;
            }

            this.LoadMappings();

            this.sourceFuncSpecs = FunctionManager.GetFunctionSpecifications(this.sourceDbType);
            this.targetFuncSpecs = FunctionManager.GetFunctionSpecifications(this.targetDbType);

            foreach (TokenInfo token in this.functions)
            {
                List<FunctionFomular> fomulars = this.GetFunctionFomulars(token.Symbol);

                foreach (FunctionFomular fomular in fomulars)
                {
                    string name = fomular.Name;

                    bool useBrackets = false;

                    MappingFunctionInfo targetFunctionInfo = this.GetMappingFunctionInfo(name, out useBrackets);

                    if (!string.IsNullOrEmpty(targetFunctionInfo.Name))
                    {
                        if (targetFunctionInfo.Name.ToUpper().Trim() != name.ToUpper().Trim())
                        {
                            string oldExp = fomular.Expression;
                            string newExp = this.ReplaceValue(fomular.Expression, name, targetFunctionInfo.Name);

                            fomular.Expression = newExp;

                            token.Symbol = this.ReplaceValue(token.Symbol, oldExp, newExp);
                        }
                    }

                    Dictionary<string, string> dictDataType = null;
                    string newExpression = this.ParseFomular(this.sourceFuncSpecs, this.targetFuncSpecs, fomular, targetFunctionInfo, out dictDataType);

                    if (newExpression != fomular.Expression)
                    {
                        token.Symbol = this.ReplaceValue(token.Symbol, fomular.Expression, newExpression);
                    }
                }
            }
        }

        private List<FunctionFomular> GetFunctionFomulars(string value)
        {
            List<FunctionFomular> functions = new List<FunctionFomular>();

            string innerContent = value;

            while (parenthesesRegex.IsMatch(innerContent))
            {
                int firstLeftParenthesesIndex = innerContent.IndexOf('(');
                int lastRightParenthesesIndex = -1;

                int leftParenthesesCount = 0;
                int rightParenthesesCount = 0;

                for (int i = 0; i < innerContent.Length; i++)
                {
                    if (innerContent[i] == '(')
                    {
                        leftParenthesesCount++;
                    }
                    else if (innerContent[i] == ')')
                    {
                        rightParenthesesCount++;

                        if (rightParenthesesCount == leftParenthesesCount)
                        {
                            lastRightParenthesesIndex = i;
                            break;
                        }
                    }
                }

                if (lastRightParenthesesIndex == -1)
                {
                    break;
                }

                string leftContent = innerContent.Substring(0, firstLeftParenthesesIndex);

                var matches = nameRegex.Matches(leftContent);

                Match nameMatch = matches.Cast<Match>().LastOrDefault();

                if (nameMatch != null)
                {
                    string name = nameMatch.Value;

                    int startIndex = nameMatch.Index;

                    int length = lastRightParenthesesIndex - startIndex + 1;

                    string expression = innerContent.Substring(startIndex, length);

                    FunctionFomular func = new FunctionFomular(name, expression)
                    {
                        StartIndex = startIndex,
                        StopIndex = lastRightParenthesesIndex
                    };

                    functions.Add(func);

                    innerContent = innerContent.Substring(firstLeftParenthesesIndex + 1, func.StopIndex - 1 - (firstLeftParenthesesIndex + 1) + 1);
                }
                else
                {
                    if (functions.Count == 0 && !leftContent.Contains("("))
                    {
                        string name = value.Substring(0, firstLeftParenthesesIndex);

                        FunctionFomular func = new FunctionFomular(name, value)
                        {
                            StartIndex = 0,
                            StopIndex = value.Length - 1
                        };

                        functions.Add(func);
                    }

                    break;
                }
            }

            return functions;
        }
    }
}
