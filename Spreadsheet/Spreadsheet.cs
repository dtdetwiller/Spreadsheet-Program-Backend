using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using SpreadsheetUtilities;

// Author: Daniel Detwiller
namespace SS
{
    /// <summary>
    /// This class represents a spreadsheet of infinite cells.
    /// </summary>
    public class Spreadsheet : AbstractSpreadsheet
    {
        /// <summary>
        /// Represents the spreadsheet.
        /// </summary>
        private Dictionary<string, Cell> sheet;
        /// <summary>
        /// List of the non-empty cells.
        /// </summary>
        private List<string> nonEmptyCells;
        /// <summary>
        /// Dependency graph to keep track of cell dependencies.
        /// </summary>
        private DependencyGraph dg;
        
        /// <summary>
        /// Boolean that is true when the spreadsheet has been changed, false otherwise.
        /// </summary>
        public override bool Changed { get; protected set; }

        /// <summary>
        /// Creates an empty spreadsheet.
        /// </summary>
        public Spreadsheet() : this(s => true, s => s, "default")
        {
        }

        /// <summary>
        /// Creates an empty spreadsheet with a validator and normalizer.
        /// </summary>
        /// <param name="isValid"></param>
        /// <param name="normalize"></param>
        /// <param name="version"></param>
        public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version) : base(isValid, normalize, version)
        {
            sheet = new Dictionary<string, Cell>();
            dg = new DependencyGraph();
            nonEmptyCells = new List<string>();
            Changed = false;
        }

        /// <summary>
        /// Creates a new spreadsheet from a file (filePath).
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isValid"></param>
        /// <param name="normalize"></param>
        /// <param name="version"></param>
        public Spreadsheet(string filePath, Func<string, bool> isValid, Func<string, string> normalize, string version) : base(isValid, normalize, version)
        {
            sheet = new Dictionary<string, Cell>();
            dg = new DependencyGraph();
            nonEmptyCells = new List<string>();
            Changed = false;

            
            string name = null;
            string contents = null;

            try
            {
                // Reads the file and adds the cells to the spreadsheet.
                using (XmlReader reader = XmlReader.Create(filePath))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "cell":
                                    break;

                                case "name":
                                    reader.Read();
                                    name = reader.Value;
                                    break;

                                case "contents":
                                    reader.Read();
                                    contents = reader.Value;
                                    break;
                            }
                        }
                        else
                        {
                            if (reader.Name == "cell")
                                SetContentsOfCell(name, contents);
                        }
                    }
                }
            }
            // Handle all exceptions
            catch (DirectoryNotFoundException) {
                throw new SpreadsheetReadWriteException("There was a problem opening the file.");
            }
            catch (FileNotFoundException)
            {
                throw new SpreadsheetReadWriteException("There was a problem opening the file.");
            }
            catch (CircularException)
            {
                throw new SpreadsheetReadWriteException("There was circular dependency in the spreadsheet.");
            }
            catch (InvalidNameException)
            {
                throw new SpreadsheetReadWriteException(name + " is an invalid cell name.");
            }
            catch (FormulaFormatException)
            {
                throw new SpreadsheetReadWriteException(contents + " is an invalid formula.");
            }
            catch (XmlException)
            {
                throw new SpreadsheetReadWriteException("There was a problem reading the file.");
            }

            // If version does not match the version of the file, throws
            if (GetSavedVersion(filePath) != version)
                throw new SpreadsheetReadWriteException("Version of the saved spreadsheet: " + GetSavedVersion(filePath) + ", does not match: " + version);


            Changed = false;
            }

        /// <summary>
        /// Returns the contents of the cell, name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override object GetCellContents(string name)
        {
            object contents = null;
            string normalizedName = Normalize(name);

            // Throws exception if name is null or invalid.
            if (normalizedName is null || !IsValidCellName(normalizedName))
                throw new InvalidNameException();
            // Gets the contents of the cell.
            if (sheet.TryGetValue(normalizedName, out Cell result))
                contents = result.GetContents();
            else
                // return empty string if cell is empty.
                return "";

            return contents;
        }

        /// <summary>
        /// Returns a list of the names of all the non-empty cells.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<string> GetNamesOfAllNonemptyCells()
        {
            return nonEmptyCells;
        }

        /// <summary>
        /// Sets the contents of the specified cell, whether the contents are a double, text, or formula.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public override IList<string> SetContentsOfCell(string name, string content)
        {
            // Throws exception if content is null.
            if (content is null)
                throw new ArgumentException();
            // Throws exception if name is null or not a valid cell name.
            if (name is null || !IsValidCellName(name))
                throw new InvalidNameException();

            // Normalizes the name of the cell.
            string normalizedName = Normalize(name);

            // If content is a number.
            if (double.TryParse(content, out double result))
                return RecalculateValues(SetCellContents(normalizedName, result));
            // If content is a formula.
            else if (content.Length > 0 && Char.Equals(content[0], '='))
            {
                try
                {
                    Formula f = new Formula(content.Substring(1), Normalize, IsValid);
                    return RecalculateValues(SetCellContents(normalizedName, f));
                }
                catch (FormulaFormatException)
                {
                    throw new FormulaFormatException("Invalid formula.");
                }
            }
            // Content is text.
            else
                return RecalculateValues(SetCellContents(normalizedName, content));
        }

        /// <summary>
        /// Creates a cell when it contains a number.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        protected override IList<string> SetCellContents(string name, double number)
        {
            // Adds contents to this cell.
            AddCellToSheet(name, number);
            // Set the values of the cell.
            sheet[name].SetValue(number);
            // Spreadsheet was changed.
            Changed = true;
            // Returns the list of cells whose values depend directly or indirectly to name.
            return GetCellsToRecalculate(name).ToList();
        }

        /// <summary>
        /// Creates a cell when it contains text.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        protected override IList<string> SetCellContents(string name, string text)
        {
            // If text is not empty
            if (!(text.Trim() == ""))
                AddCellToSheet(name, text);
            else
            {
                AddCellToSheet(name, text);
                if (nonEmptyCells.Contains(name))
                    nonEmptyCells.Remove(name);
            }

            // Set the values of the cell.
            sheet[name].SetValue(text);
            // Spreadsheet was changed.
            Changed = true;
            // Returns the list of cells whose values depend directly or indirectly to name.
            return GetCellsToRecalculate(name).ToList();
        }

        /// <summary>
        /// Creates a cell when it contains a formula.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        protected override IList<string> SetCellContents(string name, Formula formula)
        {
            // Adds cell to spreadsheet.
            AddCellToSheet(name, formula);
            // Set the values of the cell.
            sheet[name].SetValue(formula.Evaluate(Lookup));
            // Spreadsheet was changed.
            Changed = true;
            // Returns the list of cells whose values depend directly or indirectly to name.
            return GetCellsToRecalculate(name).ToList();
        }

        /// <summary>
        /// Re-evaluates the cells.
        /// </summary>
        /// <param name="names"></param>
        private IList<string> RecalculateValues(IList<string> names)
        {
            foreach (string name in names)
            {
                if (sheet.TryGetValue(name, out Cell result))
                {
                    if (result.GetContents() is Formula)
                    {
                        Formula f = (Formula)result.GetContents();
                        result.SetValue(f.Evaluate(Lookup));
                    }
                }
            }
            return names;
        }

        /// <summary>
        /// Helper method to add cell contents to the cell.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        private void AddCellToSheet(string name, object obj)
        {
            Formula f;
            List<string> vars = new List<string>();
            if (obj is Formula)
            {
                f = (Formula)obj;
                vars = f.GetVariables().ToList();
            }

            // If the spreadsheet already has contents for the cell name.
            if (sheet.ContainsKey(name))
            {
                if (sheet.TryGetValue(name, out Cell result))
                {
                    // Removes dependencies if any.
                    RemoveDependencies(result, name);
                    // If variables exist
                    if (vars.Count() > 0)
                    {
                        // Go through each variable and add a dependency to the cell.
                        foreach (string v in vars)
                        {
                            // Throws CircularException if there is a circular dependency between name and v.
                            if (IsCircularDependency(name, v))
                                throw new CircularException();
                            
                            // Adds the new dependency when name depends on v.
                            dg.AddDependency(v, name);
                        }
                    }
                    result.SetContents(obj);
                }
            }
            else
            {
                // If variables exist
                if (vars.Count() > 0)
                {
                    // Go through each variable and add a dependency to the cell.
                    foreach (string v in vars)
                    {
                        // Throws CircularException if there is a circular dependency between name and v.
                        if (IsCircularDependency(name, v))
                            throw new CircularException();
                        // Adds the new dependenct when name depends on v.
                        dg.AddDependency(v, name);
                    }
                }
                // Adds contents to this cell.
                if (obj is double)
                    sheet.Add(name, new Cell((double)obj));
                else if (obj is string)
                    sheet.Add(name, new Cell((string)obj));
                else if (obj is Formula)
                    sheet.Add(name, new Cell((Formula)obj));

                // Adds cell name to the list of non-empty cells.
                if (!nonEmptyCells.Contains(name))
                    nonEmptyCells.Add(name);
            }
        }

        /// <summary>
        /// Helper method to remove dependencies.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="name"></param>
        private void RemoveDependencies(Cell result, string name)
        {
            // If it's a formula then it can check for variable.
            if (result.GetContents() is Formula)
            {
                // Make copy of the formula.
                Formula copy = (Formula)result.GetContents();
                // If there are variables.
                if (copy.GetVariables().Count() > 0)
                {
                    // Gets the variables as a list.
                    List<string> vars = copy.GetVariables().ToList();
                    // Goes through each variable.
                    foreach (string v in vars)
                    {
                        // Removes the dependency.
                        dg.RemoveDependency(v, name);
                    }
                }
            }
        }

        /// <summary>
        /// Helper method that checks if the cells name is valid.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool IsValidCellName(string name)
        {
            // This statement returns true if name is a valid cell name.
            return Regex.IsMatch(name, "^[a-zA-Z]+[0-9]+$") && IsValid(name);
        }

        /// <summary>
        /// Helper method to indicate if there is a circular dependency or not.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private bool IsCircularDependency(string name, string v)
        {
            // Checks if v is already in the list of names dependents.
            if (GetCellsToRecalculate(name).ToList().Contains(v))
                return true;

            return false;
        }

        /// <summary>
        /// Gets the dependents of the cell, name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected override IEnumerable<string> GetDirectDependents(string name)
        {
            return dg.GetDependents(name);
        }

        /// <summary>
        /// Returns the version of the spreadsheet in this xml file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public override string GetSavedVersion(string filename)
        {
            string version = "";
            try
            {
                using (XmlReader reader = XmlReader.Create(filename))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            if (reader.Name == "spreadsheet")
                            {
                                version = reader["version"];
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw new SpreadsheetReadWriteException("Something went wrong reading the file.");
            }

            return version;
        }

        /// <summary>
        /// Writes the contents of this spreadsheet to a file.
        /// </summary>
        /// <param name="filename"></param>
        public override void Save(string filename)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";

            try
            {
                // Creates a writer and writes to the file.
                using (XmlWriter w = XmlWriter.Create(filename, settings))
                {

                    w.WriteStartDocument();
                    w.WriteStartElement("spreadsheet");
                    w.WriteAttributeString("version", Version);

                    foreach (string v in nonEmptyCells)
                    {
                        w.WriteStartElement("cell");
                        w.WriteElementString("name", v);
                        if (GetCellContents(v) is double)
                            w.WriteElementString("contents", GetCellContents(v).ToString());
                        else if (GetCellContents(v) is Formula)
                        {
                            Formula f = (Formula)GetCellContents(v);
                            w.WriteElementString("contents", "=" + f.ToString());
                        }
                        else
                        {
                            w.WriteElementString("contents", (string)GetCellContents(v));
                        }

                        w.WriteEndElement();
                    }

                    w.WriteEndElement();
                    w.WriteEndDocument();
                }
            }
            catch(ArgumentNullException)
            {
                throw new SpreadsheetReadWriteException("Not a valid file.");
            }
            catch (DirectoryNotFoundException)
            {
                throw new SpreadsheetReadWriteException("There was a problem writing to the file.");
            }

        }

        

        /// <summary>
        /// Gets the value of the cell.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override object GetCellValue(string name)
        {
            object value;
            string normalizedName = Normalize(name);

            if (normalizedName is null || !IsValidCellName(normalizedName))
                throw new InvalidNameException();

            // Gets the contents of the cell.
            if (sheet.TryGetValue(normalizedName, out Cell result))
                value = result.GetValue();
            else
                // return empty string if cell is empty.
                return "";

            return value;
        }

        /// <summary>
        /// Helper method to lookup the value of a cell.
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        private double Lookup(string var)
        {
            object value = GetCellValue(var);

            if (value is double)
                return (double)value;
            else if (value is FormulaError)
                throw new ArgumentException();
            else
                throw new ArgumentException(value + " is an unknown vlaue.");
        }

        /// <summary>
        /// Class to represent a cell.
        /// </summary>
        private class Cell
        {
            /// <summary>
            /// The contents of a cell.
            /// </summary>
            private object contents;
            /// <summary>
            /// The value of a cell.
            /// </summary>
            private object value;

            /// <summary>
            /// Constructor for a cell that contains a number.
            /// </summary>
            /// <param name="number"></param>
            public Cell(double number)
            {
                contents = number;
            }

            /// <summary>
            /// Constructor for a cell that contains text.
            /// </summary>
            /// <param name="text"></param>
            public Cell(string text)
            {
                contents = text;
            }

            /// <summary>
            /// Constructor for a cell that contains a formula object.
            /// </summary>
            /// <param name="formula"></param>
            public Cell(Formula formula)
            {
                contents = formula;
            }

            /// <summary>
            /// Returns the contents of this cell.
            /// </summary>
            /// <returns></returns>
            public object GetContents()
            {
                return contents;
            }

            /// <summary>
            /// Set the value of this cell.
            /// </summary>
            public void SetValue(object val)
            {
                value = val;
            }

            /// <summary>
            /// Returns the contents of this cell.
            /// </summary>
            /// <returns></returns>
            public object GetValue()
            {
                return value;
            }

            /// <summary>
            /// For the case where the cell alreadt exists.
            /// </summary>
            public void SetContents(object cont)
            {
                contents = cont;
            }

        }
    }
}
