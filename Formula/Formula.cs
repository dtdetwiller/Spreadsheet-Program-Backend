// Skeleton written by Joe Zachary for CS 3500, September 2013
// Read the entire skeleton carefully and completely before you
// do anything else!

// Version 1.1 (9/22/13 11:45 a.m.)

// Change log:
//  (Version 1.1) Repaired mistake in GetTokens
//  (Version 1.1) Changed specification of second constructor to
//                clarify description of how validation works

// (Daniel Kopta) 
// Version 1.2 (9/10/17) 

// Change log:
//  (Version 1.2) Changed the definition of equality with regards
//                to numeric tokens


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// Author: Joe Zachary & Daniel Detwiller
namespace SpreadsheetUtilities
{
    /// <summary>
    /// Represents formulas written in standard infix notation using standard precedence
    /// rules.  The allowed symbols are non-negative numbers written using double-precision 
    /// floating-point syntax (without unary preceeding '-' or '+'); 
    /// variables that consist of a letter or underscore followed by 
    /// zero or more letters, underscores, or digits; parentheses; and the four operator 
    /// symbols +, -, *, and /.  
    /// 
    /// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
    /// a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable; 
    /// and "x 23" consists of a variable "x" and a number "23".
    /// 
    /// Associated with every formula are two delegates:  a normalizer and a validator.  The
    /// normalizer is used to convert variables into a canonical form, and the validator is used
    /// to add extra restrictions on the validity of a variable (beyond the standard requirement 
    /// that it consist of a letter or underscore followed by zero or more letters, underscores,
    /// or digits.)  Their use is described in detail in the constructor and method comments.
    /// </summary>
    public class Formula
    {
        /// <summary>
        /// The formula as a string after being isValid and Normalize.
        /// </summary>
        private string finalFormula;
        /// <summary>
        /// List of valid variable.
        /// </summary>
        private List<string> variableList = new List<string>();
        /// <summary>
        /// Values stack for evaluating the formula.
        /// </summary>
        private Stack<double> values;
        /// <summary>
        /// Operators stack for evaluating the formula.
        /// </summary>
        private Stack<string> operators;
        /// <summary>
        /// FormulaError object being.
        /// </summary>
        private FormulaError fError;
        /// <summary>
        /// Flag to tell evaluate if there is a formula error or not.
        /// </summary>
        private bool isFormulaError;

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically invalid,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer is the identity function, and the associated validator
        /// maps every string to true.  
        /// </summary>
        public Formula(String formula) :
            this(formula, s => s, s => true)
        {
        }

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically incorrect,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer and validator are the second and third parameters,
        /// respectively.  
        /// 
        /// If the formula contains a variable v such that normalize(v) is not a legal variable, 
        /// throws a FormulaFormatException with an explanatory message. 
        /// 
        /// If the formula contains a variable v such that isValid(normalize(v)) is false,
        /// throws a FormulaFormatException with an explanatory message.
        /// 
        /// Suppose that N is a method that converts all the letters in a string to upper case, and
        /// that V is a method that returns true only if a string consists of one letter followed
        /// by one digit.  Then:
        /// 
        /// new Formula("x2+y3", N, V) should succeed
        /// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
        /// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
        /// </summary>
        public Formula(String formula, Func<string, string> normalize, Func<string, bool> isValid)
        {
            List<string> tokens = GetTokens(formula).ToList();
            isFormulaError = false;

            if (!(tokens.Count > 0))
                throw new FormulaFormatException("The formula is empty. There needs to be at least one token in the formula.");

            foreach (string t in tokens)
            {
                if (IsValidToken(t))
                    continue;
            }

            CheckForSyntaxErrors(tokens, normalize, isValid);

            foreach (string t in tokens)
            {
                
                if (IsVariable(t))
                {
                    // Checks if the variable is still valid after normalizing it.
                    if (IsVariable(normalize(t)))
                        addToVariableList(normalize(t));
                    else
                        throw new FormulaFormatException("After normalizing " + t + ", it is not a valid variable.");
                    // Checks if the variable is valid with the users validator after normalizing it.
                    if (!isValid(normalize(t)))
                        throw new FormulaFormatException("After trying to validate " + t + ", it was not a valid variable.");

                    finalFormula = finalFormula + normalize(t);
                }
                else if (double.TryParse(t, out double result))
                    finalFormula = finalFormula + normalize(double.Parse(t).ToString());
                else
                    finalFormula = finalFormula + normalize(t);

            }
        }

        private void CheckForSyntaxErrors(List<string> tokens, Func<string, string> normalize, Func<string, bool> isValid)
        {
            ParenthesesAreBalanced(tokens);
            CorrectStartingToken(tokens);
            CorrectEndingToken(tokens);
            CorrectParenthesesOperatorFollowing(tokens);
            ExtraFollowingRule(tokens);
        }

        /// <summary>
        /// Adds a variable to the list of variables if it is not already in the list.
        /// </summary>
        /// <param name="var"></param>
        private void addToVariableList(string var)
        {
            if (!variableList.Contains(var))
                variableList.Add(var);
        }

        /// <summary>
        /// Checks if a token that follows a number, variable, or closing parenthesis is a operator or closing parenthesis.
        /// Returns true if so, otherwise throws a FormulaFormatException.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private void ExtraFollowingRule(List<string> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                // Checks if token is a number, variable, or closing parenthesis.
                if ((i != tokens.Count - 1) && (double.TryParse(tokens[i], out double result) || IsVariable(tokens[i]) || tokens[i].Equals(")")))
                {
                    // Check cases for the following token.
                    if (tokens[i + 1].Equals("+") || tokens[i + 1].Equals("-") || tokens[i + 1].Equals("*") || tokens[i + 1].Equals("/"))
                        continue;
                    else if (tokens[i + 1].Equals(")"))
                        continue;
                    else
                        throw new FormulaFormatException("There is a token that follows a number, variable, or closing parenthesis that is not a operator or closing parenthesis.");
                }
            }
        }

        /// <summary>
        /// Checks if a token that follows a opening parenthesis or operator is either a number, variable, or opening parenthesis.
        /// Returns true if so, otherwise throws a FormulaFormatException.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private void CorrectParenthesesOperatorFollowing(List<string> tokens)
        {
            // Goes through each token.
            for (int i = 0; i < tokens.Count; i++)
            {
                // Checks if token is a opening parenthesis or operator.
                if ((i != tokens.Count - 1) && (tokens[i].Equals("(") || tokens[i].Equals("+") || tokens[i].Equals("-") || tokens[i].Equals("*") || tokens[i].Equals("/")))
                {
                    // Check cases for the following token.
                    if (double.TryParse(tokens[i + 1], out double result))
                        continue;
                    else if (IsVariable(tokens[i + 1]))
                        continue;
                    else if (tokens[i + 1].Equals("("))
                        continue;
                    else
                        throw new FormulaFormatException("There is a token that follows a opening parenthesis or operator that is not a number, variable, or opening parenthesis.");
                }
            }

        }

        /// <summary>
        /// Checks if the last token in the formula is a number, variable, or closing parenthesis.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private void CorrectEndingToken(List<string> tokens)
        {
            if (double.TryParse(tokens[tokens.Count - 1], out double result))
                return;
            else if (IsVariable(tokens[tokens.Count - 1]))
                return;
            else if (tokens[tokens.Count - 1].Equals(")"))
                return;
            else
                throw new FormulaFormatException("The last token of the formula needs to be either a number, variable, or closing parenthesis");
        }

        /// <summary>
        /// Checks if the first token in the formula is a number, variable, or opening parenthesis
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private void CorrectStartingToken(List<string> tokens)
        {
            if (double.TryParse(tokens[0], out double result))
                return;
            else if (IsVariable(tokens[0]))
                return;
            else if (tokens[0].Equals("("))
                return;
            else
                throw new FormulaFormatException("The first token of the formula needs to be either a number, variable, or opening parenthesis");
        }

        /// <summary>
        /// Checks if the number of closing parentheses seen so far are greater than the number 
        /// of opening parenthesis seen so far.
        /// Also checks if the parentheses are balanced.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private void ParenthesesAreBalanced(List<string> tokens)
        {
            int openP = 0;
            int closeP = 0;

            // Goes through each token and counts the number of closing parentheses and opening parentheses.
            foreach (string t in tokens)
            {
                if (t.Equals("("))
                    openP++;
                if (t.Equals(")"))
                    closeP++;
                // Checks if the number of closing parentheses are greater than the number of opening parentheses.
                if (closeP > openP)
                    throw new FormulaFormatException("There are too many closing parentheses.");
            }

            if (openP != closeP)
                throw new FormulaFormatException("The parentheses are not balanced (number of opening parentheses does not equal number of closing parentheses.");

        }

        /// <summary>
        /// Checks if the given token t is one of these valid tokens (, ), +, -, *, /, variables, and 
        /// decimal real numbers (including scientific notation).
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private bool IsValidToken(string t)
        {
            // If t is a decimal real number (including scientific notation.
            if (double.TryParse(t, out double result))
                return true;
            // If t is one of these tokens: (, ), +, -, *, /
            else if (t.Equals("(") || t.Equals(")") || t.Equals("+") || t.Equals("-") || t.Equals("*") || t.Equals("/"))
                return true;
            // If t is in correct variable form.
            else if (IsVariable(t))
                return true;
            else
                throw new FormulaFormatException(t + " is not a valid token.");
        }

        /// <summary>
        /// Checks if the given string is a variable.
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        private bool IsVariable(string var)
        {
            bool isValidVar = false;

            for (int i = 0; i < var.Length; i++)
            {
                // Checks if the first character is a digit, if so  returns false.
                if (i == 0 && Char.IsDigit(var[i]))
                    return false;
                // Checks if a the character at index i in the string is a letter, digit, or underscore.
                if (Char.IsLetter(var[i]) || Char.IsDigit(var[i]) || Char.Equals(var[i], '_'))
                    isValidVar = true;
                else
                    // Returns false if it runs into anything other than a letter, digit, or underscore.
                    return false;
            }

            return isValidVar;
        }

        /// <summary>
        /// Evaluates this Formula, using the lookup delegate to determine the values of
        /// variables.  When a variable symbol v needs to be determined, it should be looked up
        /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to 
        /// the constructor.)
        /// 
        /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters 
        /// in a string to upper case:
        /// 
        /// new Formula("x+7", N, s => true).Evaluate(L) is 11
        /// new Formula("x+7").Evaluate(L) is 9
        /// 
        /// Given a variable symbol as its parameter, lookup returns the variable's value 
        /// (if it has one) or throws an ArgumentException (otherwise).
        /// 
        /// If no undefined variables or divisions by zero are encountered when evaluating 
        /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
        /// The Reason property of the FormulaError should have a meaningful explanation.
        ///
        /// This method should never throw an exception.
        /// </summary>
        public object Evaluate(Func<string, double> lookup)
        {
            List<string> tokens = GetTokens(finalFormula).ToList();
            double expressionValue = 0.0;
            values = new Stack<double>();
            operators = new Stack<string>();

            // Go through each token
            foreach (string t in tokens)
            {
                // If token is a number.
                if (double.TryParse(t, out double result))
                {
                    DoDoubleMath(result);
                }
                // If token is a variable.
                else if (IsVariable(t))
                {
                    try
                    {
                        double varInt = lookup(t);
                        DoDoubleMath(varInt);
                    }
                    catch (ArgumentException)
                    {
                        return new FormulaError("The variable " + t + " is undefined.");
                    }
                }
                // If token is + or -.
                else if (t.Equals("+") || t.Equals("-"))
                {
                    CheckOperator(t);
                }
                // If token is * or /.
                else if (t.Equals("*") || t.Equals("/"))
                {
                    operators.Push(t);
                }
                // If token is (.
                else if (t.Equals("("))
                {
                    operators.Push(t);
                }
                // If token is ).
                else if (t.Equals(")"))
                {
                    CheckOperator(t);
                }
            }

            // Gets the value of the expression.
            if (operators.Count == 0)
            {
                if (values.Count == 1)
                    expressionValue = values.Pop();
            }

            // If there is one operator and 2 values on the stack
            else if (operators.Count == 1 && values.Count == 2)
            {
                if (operators.Peek().Equals("+") || operators.Peek().Equals("-"))
                {
                    double val1 = values.Pop();
                    double val2 = values.Pop();
                    string op = operators.Pop();

                    if (op.Equals("+"))
                    {
                        expressionValue = val1 + val2;
                    }
                    else
                    {
                        expressionValue = val2 - val1;
                    }
                }
                else if (operators.Peek().Equals("*") || operators.Peek().Equals("/"))
                {
                    double val1 = values.Pop();
                    double val2 = values.Pop();
                    string op = operators.Pop();

                    if (op.Equals("*"))
                    {
                        expressionValue = val1 * val2;
                    }
                    else
                    {
                        if (val2 == 0)
                        {
                            return new FormulaError("Division by 0");
                        }
                        else
                        {
                            expressionValue = val2 / val1;
                        }
                    }
                }
            }

            // If there was a formula error.
            if (isFormulaError)
                return fError;
            return expressionValue; ;
        }

        /// <summary>
        /// This method takes the current operator into account and applies the correct evaluation.
        /// </summary>
        /// <param name="op"></param>
        private void CheckOperator(string op)
        {
            if ((op.Equals("+") || op.Equals("-")))
            {
                // Checks if + or - is on the top of the operators stack.
                if (OperOnTop("+", "-"))
                {
                    DoAddOrSub();
                }
                operators.Push(op);
            }

            // Check is ) is the current operator.
            if (op.Equals(")"))
            {
                // Checks if + or - is on the top of the operators stack.
                if (OperOnTop("+", "-"))
                    // If values is greater than or equal to 2, does expected math.
                    if (values.Count >= 2)
                        DoAddOrSub();
                // If ( is as the top of the operators stack, pops it off.
                if (operators.Count > 0 && operators.Peek().Equals("("))
                    operators.Pop();
                // Checks if * or / is at the top of the operators stack.
                if (OperOnTop("*", "/"))
                    // If values is greater than or equal to 2, does expected math.
                    if (values.Count >= 2)
                        DoMultOrDiv(values.Pop());
            }

        }

        /// <summary>
        /// This method does math for addition and subtraction.
        /// </summary>
        private void DoAddOrSub()
        {
            double val1 = values.Pop();
            double val2 = values.Pop();

            string currOp = operators.Pop();

            double result;

            if (currOp.Equals("+"))
            {
                result = val1 + val2;
            }
            else
            {
                result = val2 - val1;
            }

            values.Push(result);
        }

        /// <summary>
        /// This method does the math for the given double if * or / at the top of the operators
        /// stack, else just adds it to the stack.
        /// </summary>
        /// <param name="d"></param>
        private void DoDoubleMath(double d)
        {
            // If * or / is at the top of the stack.
            if (OperOnTop("*", "/"))
                // Do expected math.
                DoMultOrDiv(d);
            else
                values.Push(d);
        }

        /// <summary>
        /// This method does math for multiplication and division.
        /// </summary>
        /// <param name="d"></param>
        private void DoMultOrDiv(double d)
        {
            double val = values.Pop();
            string op = operators.Pop();

            double result;

            if (op.Trim().Equals("*"))
            {
                result = val * d;
            }
            else
            {
                if (d == 0)
                {
                    isFormulaError = true;
                    fError = new FormulaError("Division by 0");
                    result = 0;
                }
                else
                {
                    result = val / d;
                }
            }

            values.Push(result);
        }

        /// <summary>
        /// Returns true if the one of the two inputed operators is at the top of the stack, else
        /// returns false.
        /// </summary>
        /// <param name="op1"></param>
        /// <param name="op2"></param>
        /// <returns></returns>
        private bool OperOnTop(string op1, string op2)
        {
            if (operators.Count() > 0 && values.Count() > 0)
            {
                if (operators.Peek().Equals(op1) || operators.Peek().Equals(op2))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Enumerates the normalized versions of all of the variables that occur in this 
        /// formula.  No normalization may appear more than once in the enumeration, even 
        /// if it appears more than once in this Formula.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
        /// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
        /// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
        /// </summary>
        public IEnumerable<String> GetVariables()
        {
            return variableList;
        }

        /// <summary>
        /// Returns a string containing no spaces which, if passed to the Formula
        /// constructor, will produce a Formula f such that this.Equals(f).  All of the
        /// variables in the string should be normalized.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x + y", N, s => true).ToString() should return "X+Y"
        /// new Formula("x + Y").ToString() should return "x+Y"
        /// </summary>
        public override string ToString()
        {
            return finalFormula;
        }

        /// <summary>
        /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
        /// whether or not this Formula and obj are equal.
        /// 
        /// Two Formulae are considered equal if they consist of the same tokens in the
        /// same order.  To determine token equality, all tokens are compared as strings 
        /// except for numeric tokens and variable tokens.
        /// Numeric tokens are considered equal if they are equal after being "normalized" 
        /// by C#'s standard conversion from string to double, then back to string. This 
        /// eliminates any inconsistencies due to limited floating point precision.
        /// Variable tokens are considered equal if their normalized forms are equal, as 
        /// defined by the provided normalizer.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        ///  
        /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
        /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
        /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
        /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
        /// </summary>
        public override bool Equals(object obj)
        {
            // Returns false if obj is null or not a Formula.
            if (obj is null || !(obj is Formula))
                return false;
            Formula f = (Formula)obj;
            
            return finalFormula.Equals(f.ToString());
        }

        /// <summary>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return true.  If one is
        /// null and one is not, this method should return false.
        /// </summary>
        public static bool operator ==(Formula f1, Formula f2)
        {
            if (f1 is null && f2 is null)
                return true;
            else if (f1 is null)
                return false;
            else if (f2 is null)
                return false;

            return f1.Equals(f2);
        }

        /// <summary>
        /// Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return false.  If one is
        /// null and one is not, this method should return true.
        /// </summary>
        public static bool operator !=(Formula f1, Formula f2)
        {
            return !f1.Equals(f2);
        }

        /// <summary>
        /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
        /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
        /// randomly-generated unequal Formulae have the same hash code should be extremely small.
        /// </summary>
        public override int GetHashCode()
        {
            List<string> tokens = GetTokens(finalFormula).ToList();
            int hash = 0;

            foreach (string t in tokens)
            {
                hash++;
                if (IsVariable(t))
                    hash += t.Length;
                else if (int.TryParse(t, out int result))
                    hash += result;
                else if (t.Equals("("))
                    hash++;
                else if (t.Equals(")"))
                    hash += 2;
                else if (t.Equals("+"))
                    hash += 3;
                else if (t.Equals("-"))
                    hash += 4;
                else if (t.Equals("*"))
                    hash += 5;
                else if (t.Equals("/"))
                    hash += 6;
            }

            return hash;
        }

        /// <summary>
        /// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
        /// right paren; one of the four operator symbols; a string consisting of a letter or underscore
        /// followed by zero or more letters, digits, or underscores; a double literal; and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        private static IEnumerable<string> GetTokens(String formula)
        {
            // Patterns for individual tokens
            String lpPattern = @"\(";
            String rpPattern = @"\)";
            String opPattern = @"[\+\-*/]";
            String varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
            String doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
            String spacePattern = @"\s+";

            // Overall pattern
            String pattern = String.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                            lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

            // Enumerate matching tokens that don't consist solely of white space.
            foreach (String s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace))
            {
                if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline))
                {
                    yield return s;
                }
            }

        }
    }

    /// <summary>
    /// Used to report syntactic errors in the argument to the Formula constructor.
    /// </summary>
    public class FormulaFormatException : Exception
    {
        /// <summary>
        /// Constructs a FormulaFormatException containing the explanatory message.
        /// </summary>
        public FormulaFormatException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Used as a possible return value of the Formula.Evaluate method.
    /// </summary>
    public struct FormulaError
    {
        /// <summary>
        /// Constructs a FormulaError containing the explanatory reason.
        /// </summary>
        /// <param name="reason"></param>
        public FormulaError(String reason)
            : this()
        {
            Reason = reason;
        }

        /// <summary>
        ///  The reason why this FormulaError was created.
        /// </summary>
        public string Reason { get; private set; }
    }
}