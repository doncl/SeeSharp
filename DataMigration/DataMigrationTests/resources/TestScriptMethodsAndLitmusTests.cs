using System;
using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// This is a test script method and litmustest file; the test code will compile it on the
    /// fly, invoke the TransformMethod(s) and LitmusTest(s) in the file, and verify they work
    /// as expected.
    /// </summary>
    public class TestScriptMethodsAndLitmusTests
    {
        #region TransformMethods

        /// <summary>
        /// TransformMethod that validates input and adds two numbers.
        /// </summary>
        /// Parameters and return are described at <see cref="DataMigration.TransformMethod"/>
        public static string AddTwoNumbers(int loadNum, IDictionary<String, ILookup> lookups, 
                                          IDictionary<string, IExistence> existenceObjects,
                                          IDictionary<string, string> rowInProgress, 
                                          IList<string> args)
        {
            int? firstNumber;
            int? secondNumber;

            // This will throw the BadRowException if the two numbers aren't parseable.
            ValidateAndExtractTwoArgs(args, true, out firstNumber, out secondNumber);  
 
            return (firstNumber.Value + secondNumber.Value).ToString();
        }
        #endregion

        #region LitmusTests

        /// <summary>
        /// Litmust test that validates that two args are passed in, and they're both integers.
        /// </summary>
        /// Parameters and return are described at <see cref="DataMigration.LitmusTest"/>
        public static bool ValidateTwoNumbers(IDictionary<String, ILookup> lookups, 
            IDictionary<string, IExistence> existenceObjects,IList<string> arguments)
        {
            int? firstNumber;
            int? secondNumber;

            return ValidateAndExtractTwoArgs(arguments, false, out firstNumber, 
                                                               out secondNumber);
        }

        #endregion

        /// <summary>
        /// Helper used by both a TransformMethod and LitmusTest.  Verifies that the first two
        /// arguments in the argument list exist and are integers.  It throws a BadRowException
        /// for the TransformMethod, and just returns false for the LitmusTest.
        /// </summary>
        /// <param name="args">List of arguments to parse.</param>
        /// <param name="throwException">If true, throws the BadRowException.</param>
        /// <param name="firstNumber">The first integer argument to parse out and return.
        /// </param>
        /// <param name="secondNumber">The second integer argument to parse out and return.
        /// </param>
        /// <returns>True if both numbers exist and are pareseable as ints, false otherwsie.
        /// It throws a BadRowException on failure if throwException is true.</returns>
        private static bool ValidateAndExtractTwoArgs(IList<string> args, bool throwException,
                                            out int? firstNumber, out int? secondNumber)
        {
            firstNumber = null;
            secondNumber = null;

            if (args.Count != 2) {
                if (throwException) {
                    throw new BadRowException(String.Format("AddTwoNumbers expects 2 " +
                                                  "arguments but was passed in {0} arguments",
                                                    args.Count));
                }
                else {
                    return false;
                }
            }

            var firstArgument = args[0];
            var secondArgument = args[1];

            firstNumber = NumericArgHelper(firstArgument);
            secondNumber = NumericArgHelper(secondArgument);

            if (throwException) {
                if (!firstNumber.HasValue) {
                    throw new BadRowException(String.Format("Argument {0} to AddTwoNumbers is " 
                                             + "not parseable as an int", firstArgument));
                }

                if (!secondNumber.HasValue) {
                    throw new BadRowException(String.Format("Argument {0} to AddTwoNumbers is " 
                                         + "not parseable as an int", secondArgument));
                }
            }
            return firstNumber.HasValue && secondNumber.HasValue;
        }

        /// <summary>
        /// Helper function to parse numbers from the arguments array, throwing 
        /// BadRowsException if it fails.
        /// </summary>
        /// <returns>The parsed integer.</returns>
        /// <param name="stringArgument">Numeric string argument.</param>
        static int? NumericArgHelper(string stringArgument)
        {
            int number;
            if (!Int32.TryParse(stringArgument, out number)) {
                return null;
            }
            return number;
        }    
    }
}
