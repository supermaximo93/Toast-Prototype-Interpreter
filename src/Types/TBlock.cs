using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toast.Types.Singletons;

namespace Toast.Types
{
    /// <summary>
    /// A TType containing strings of Toast code, which can be executed at any time.
    /// </summary>
    class TBlock : TType
    {
        List<string> statements;
        int elseLocation, currentLine;
        bool elseAllowed;

        /// <summary>
        /// The constructor for TBlock.
        /// </summary>
        /// <param name="interpreter">The interpreter that the constructor is being called from.</param>
        /// <param name="exception">An exception that will be written to if there was an error.</param>
        /// <param name="elseAllowed">
        /// Whether the 'else' keyword is allowed to be used in the top level of the block. Usually only used in when
        /// creating a block in IF statements.
        /// </param>
        public TBlock(Interpreter interpreter, out TException exception, bool elseAllowed)
        {
            statements = new List<string>();
            elseLocation = -1;
            this.elseAllowed = elseAllowed;
            exception = null;
            currentLine = -1;

            int blockLevel = 0;
            while (true) // Keep adding statements until the appropriate terminator (i.e. 'end') is found
            {
                string statement = interpreter.GetInput().Trim();

                // Split the statement into symbols so we can analyse it to find out where the block begin/ends are
                Interpreter.Group symbolGroup = interpreter.SplitIntoSymbols(statement, out exception);
                if (exception != null)
                {
                    statements.Clear();
                    break;
                }

                // A bit of Linq to easily count the number of a particular keyword in a group
                string keywordName = "";
                var keywordQuery =
                    from object item in symbolGroup
                    where (item is string) && ((string)item == keywordName)
                    select item;

                // If there's a begin, increment the block level. If there are too many then return an error
                keywordName = "begin";
                int beginCount = keywordQuery.Count();
                if (beginCount > 1)
                {
                    exception = new TException(interpreter, "Having more than one begin on a line is forbidden");
                    break;
                }
                else if (beginCount == 1) ++blockLevel;

                // If there's an end, decrement the block level
                keywordName = "end";
                int endCount = keywordQuery.Count();
                if (endCount > 1)
                {
                    exception = new TException(interpreter, "Having more than one end on a line is forbidden");
                    break;
                }
                else if (endCount == 1)
                {
                    if (blockLevel == 0) break;
                    else --blockLevel;
                }

                // Increment the block level if there is an IF statement or WHILE loop where a block of code is to
                // follow, i.e. there is no statement after the comma after the condition
                if (symbolGroup.IndexOf("if") >= 0)
                {
                    string commaStr = symbolGroup[symbolGroup.Count - 1] as string;
                    if ((commaStr != null) && (commaStr == ",")) ++blockLevel;
                }

                if (symbolGroup.IndexOf("while") >= 0)
                {
                    string commaStr = symbolGroup[symbolGroup.Count - 1] as string;
                    if ((commaStr != null) && (commaStr == ",")) ++blockLevel;
                }

                bool breakNow = false; // For a double break
                int elseIndex = -1;
                while ((elseIndex = symbolGroup.IndexOf("else", elseIndex + 1)) >= 0)
                {
                    if (elseIndex == 0) // If the else is at the beginning of the line
                    {
                        if (blockLevel == 0)
                        {
                            // If a top level 'else' has not already been used and the use of 'else' is allowed
                            if ((elseLocation < 0) && elseAllowed)
                            {
                                elseLocation = statements.Count;
                                if (elseIndex < symbolGroup.Count - 1) // If there is a statement after the 'else'
                                {
                                    /* Convert
                                     *      <block>
                                     *      else <statement>
                                     *      
                                     * into
                                     *      <block>
                                     *      else
                                     *      <block>
                                     * 
                                     * and stop populating the block with statments
                                     */
                                    statements.Add("else");
                                    statements.Add(statement.Substring(4, statement.Length - 4));
                                    breakNow = true;
                                }
                            }
                            else
                            { // 'else' already used or it is not allowed, error
                                exception =
                                    new TException(interpreter, "Unexpected keyword 'else'",
                                        elseAllowed ? "else already used" : "else not allowed in this construct");
                                statements.Clear();
                                breakNow = true;
                                break;
                            }
                        }
                        else if (elseIndex < symbolGroup.Count - 1) --blockLevel;
                            // if a statement follows the 'else' on the same line then decrement the block level
                    }
                    else if (symbolGroup.LastIndexOf("if", elseIndex) >= 0)
                    {
                        /* If there is an IF statement of the form
                         * 
                         *      if <condition>, <statement> else
                         *      <block>
                         *      end
                         *      
                         *      I.e. if the last occurrence of 'if' is before the current 'else'
                         *      and the current 'else' is the last symbol of the line
                         *      
                         * increment the block level.
                         */
                        if (elseIndex == symbolGroup.Count - 1) ++blockLevel;
                    }
                }
                if (breakNow) break;
                
                statements.Add(statement);
            }
        }

        /// <summary>
        /// Determines whether the block contains an 'else' in the top level of the block.
        /// </summary>
        /// <returns>Whether the block contains an 'else' in the top level.</returns>
        public bool HasElse()
        {
            return (elseLocation >= 0);
        }

        /// <summary>
        /// Gets the next line of Toast code in the block.
        /// </summary>
        /// <returns>A string containing the next line of Toast code in the block.</returns>
        public string GetInput()
        {
            if (++currentLine >= statements.Count) return "";
            return statements[currentLine];
        }

        /// <summary>
        /// Executes the code contained inside the TBlock.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="exitFromFunction">
        /// A value that will be set to whether 'exit()' was called during the block execution.
        /// </param>
        /// <param name="beforeElse">Whether to execute the code before the 'else', if any.</param>
        /// <returns>A TType value of the result of the last executed statement in the block.</returns>
        public TType Execute(Interpreter interpreter, out bool exitFromFunction, bool beforeElse = true)
        {
            exitFromFunction = false;
            if (!beforeElse && (elseLocation < 0)) return new TException(interpreter, "No else statement found");

            int start, end;
            // Decide which section of code should be executed
            if (elseAllowed)
            {
                if (beforeElse)
                {
                    start = 0;
                    end = (elseLocation > 0 ? elseLocation : statements.Count) - 1;
                }
                else
                {
                    start = elseLocation + 1;
                    end = statements.Count - 1;
                }
            }
            else
            {
                start = 0;
                end = statements.Count - 1;
            }

            int stackLevel = interpreter.Stack.Level;
            // Keep track of the previous block (if any). Used when there are blocks within blocks
            TBlock previousCurrentBlock = interpreter.CurrentBlock;
            interpreter.CurrentBlock = this;

            TType returnValue = null;
            for (currentLine = start; currentLine <= end; ++currentLine)
            {
                returnValue = interpreter.Interpret(statements[currentLine], true);

                if (interpreter.Stack.Level < stackLevel) // if 'exit()' was called
                {
                    exitFromFunction = true;
                    break;
                }

                // Need to check for TBreak so that it can be used to break from a loop (if any)
                if ((returnValue is TException) || (returnValue is TBreak)) break;
            }

            // Now that the block has finished executing its code, restore control to the outer block
            interpreter.CurrentBlock = previousCurrentBlock;

            return returnValue ?? TNil.Instance;
        }

        public override string TypeName { get { return T_BLOCK_TYPENAME; } }

        public override string ToCSString()
        {
            return "block";
        }
    }
}
