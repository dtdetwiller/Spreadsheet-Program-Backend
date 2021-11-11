using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FormulaEvaluator
{
    /// <summary>
    /// This class evaluates arithmetic expressions using standard infix notation.
    /// </summary>
    public static class Evaluator
    {
        private static Stack<int> values;
        private static Stack<string> operators;

        public delegate int Lookup(String v);

        /// <summary>
        /// This is the main function of the class. This function checks each token of the expression and handles each token accordingling.
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="variableEvaluator"></param>
        /// <returns> The value of the given expression. </returns> 
        public static int Evaluate(String exp, Lookup variableEvaluator)
        {
            values = new Stack<int>();
            operators = new Stack<string>();
            int expressionValue = 0;

            string[] substrings = Regex.Split(exp, "(\\()|(\\))|(-)|(\\+)|(\\*)|(/)");

            
            foreach (string t in substrings)
            {
                if (t.Trim().Equals(""))
                    continue;
                if (t.Trim().All(char.IsDigit))
                {
                    IsAnInteger(int.Parse(t.Trim()));
                }
                else if (IsValidVar(t.Trim()))
                {
                    int varInt = variableEvaluator(t.Trim());

                    IsAnInteger(varInt);
                    
                }
                else if (t.Trim().Equals("+") || t.Trim().Equals("-"))
                {
                    IsPlusOrMinus(t.Trim());
                }
                else if (t.Trim().Equals("*") || t.Trim().Equals("/"))
                {
                    operators.Push(t.Trim());
                }
                else if (t.Trim().Equals("("))
                {
                    operators.Push(t.Trim());
                }
                else if (t.Trim().Equals(")"))
                {
                    IsClosedParenthisis(t.Trim());
                }
                else
                {
                    Console.WriteLine("Invalid token.");
                    throw new ArgumentException();
                }
            }

            if (operators.Count == 0)
            {
                if (values.Count == 1)
                {
                    expressionValue = values.Pop();
                }
                else
                {
                    Console.WriteLine("Value stack has more than one number.");
                    throw new ArgumentException();
                }
            }
            else if (operators.Count == 1 && values.Count == 2)
            {
                if (operators.Peek().Equals("+") || operators.Peek().Equals("-"))
                {
                    int val1 = values.Pop();
                    int val2 = values.Pop();
                    string op = operators.Pop();

                    if (op.Equals("+"))
                    {
                        expressionValue = val1 + val2;
                    }
                    else
                    {
                        expressionValue = val2- val1;
                    }
                }
                else if (operators.Peek().Equals("*") || operators.Peek().Equals("/"))
                {
                    int val1 = values.Pop();
                    int val2 = values.Pop();
                    string op = operators.Pop();

                    if (op.Equals("*"))
                    {
                        expressionValue = val1 * val2;
                    }
                    else
                    {
                        if (val2 == 0)
                        {
                            Console.WriteLine("Division of {0} by zero", val1);
                            throw new ArgumentException();
                        }
                        else
                        {
                            expressionValue = val2 / val1;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("There isn't exactly one operator on the operator stack or exactly two numbers on the value stack.");
                throw new ArgumentException();
            }

            return expressionValue;
        }

        /// <summary>
        /// This function checks if the given token follows the correct variable syntax.
        /// </summary>
        /// <param name="var"></param>
        /// <returns> true if t is a valid variable, false otherwise. </returns>
        private static bool IsValidVar(string var)
        {
            bool hasLetter = false;
            bool hasDigit = false;
            int i;
            
            for (i = 0; i < var.Length; i++)
            {
                if (Char.IsLetter(var[i]))
                    hasLetter = true;
                else
                    break;
            }

            for (; i < var.Length; i++)
            {
                if (Char.IsDigit(var[i]))
                    hasDigit = true;
                else
                    return false;
            }

            return hasLetter && hasDigit;
        }

        /// <summary>
        /// This function handles the case where the given token is a closed parenthesis.
        /// </summary>
        /// <param name="t"></param>
        private static void IsClosedParenthisis(string t)
        {
            if (operators.Count > 0 && (operators.Peek().Equals("+") || operators.Peek().Equals("-")))
            {
                if (values.Count >= 2)
                {
                    int val1 = values.Pop();
                    int val2 = values.Pop();

                    string op = operators.Pop();
                    int result;

                    if (op.Equals("+"))
                    {
                        result = val1 + val2;
                    }
                    else
                    {
                        result = val2 - val1;
                    }

                    values.Push(result);

                }
                else
                {
                    Console.WriteLine("Value stack contains fewer than two values.");
                    throw new ArgumentException();
                }
            }

            if (operators.Count > 0 && operators.Peek().Equals("("))
            {
                operators.Pop();
            }
            else
            {
                Console.WriteLine("Expected parenthesis was not found.");
                throw new ArgumentException();
            }

            if (operators.Count > 0 && (operators.Peek().Equals("*") || operators.Peek().Equals("/")))
            {
                if (values.Count >= 2)
                {
                    int val1 = values.Pop();
                    int val2 = values.Pop();

                    string op = operators.Pop();
                    int result;

                    if (op.Equals("*"))
                    {
                        result = val1 * val2;
                    }
                    else
                    {
                        if (val2 == 0)
                        {
                            Console.WriteLine("Division of {0} by zero", val1);
                            throw new ArgumentException();
                        }
                        else
                        {
                            result = val2 / val1;
                        }
                    }

                    values.Push(result);

                }
                else
                {
                    Console.WriteLine("Value stack contains fewer than two values.");
                    throw new ArgumentException();
                }
            }

        }

        /// <summary>
        /// This function handles the case where the given token is a "+" or "-".
        /// </summary>
        /// <param name="t"></param>
        private static void IsPlusOrMinus(string t)
        {
            if (operators.Count > 0 && (operators.Peek().Trim().Equals("+") || operators.Peek().Trim().Equals("-")))
            {
                if (values.Count >= 2)
                {
                    int val1 = values.Pop();
                    int val2 = values.Pop();

                    string op = operators.Pop();

                    int result;

                    if (op.Equals("+"))
                    {
                        result = val1 + val2;
                    }
                    else
                    {
                        result = val2 - val1;
                    }

                    values.Push(result);
                }
                else
                {
                    Console.WriteLine("Value stack contains fewer than 2 values.");
                    throw new ArgumentException();
                }

            }

            operators.Push(t);
        }

        /// <summary>
        /// This function handles the case where the given token is an integer.
        /// </summary>
        /// <param name="n"></param>
        private static void IsAnInteger(int n)
        {
            if (operators.Count > 0 && (operators.Peek().Equals("*") || operators.Peek().Equals("/")))
            {
                if (values.Count > 0)
                {
                    int val = values.Pop();
                    string op = operators.Pop();

                    int result;

                    if (op.Trim().Equals("*"))
                    {
                        result = val * n;
                    }
                    else
                    {
                        if (n == 0)
                        {
                            Console.WriteLine("Division of {0} by zero", val);
                            throw new ArgumentException();
                        }
                        else
                        {
                            result = val / n;
                        }
                    }

                    values.Push(result);
                }
                else
                {
                    Console.WriteLine("The value stack is empty.");
                    throw new ArgumentException();
                }
            }
            else
            {
                values.Push(n);
            }
        }
    }
}
