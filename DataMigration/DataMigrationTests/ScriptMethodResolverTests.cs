using System;
using System.Collections.Generic;
using System.IO;
using DataMigration;
using NUnit.Framework;

namespace DataMigrationTests
{
    /// <summary>
    /// This test class tests that the ReflectionResolverHelper works to grovel out the 
    /// TransformMethods and LitmusTests from an assembly, and the results can be executed at
    /// runtime, and produce the expected results.
    /// </summary>
    [TestFixture]
    public class ScriptMethodResolverTests
    {
        string pathToScriptFile;
        ScriptMethodResolver scriptMethodResolver;

        IDictionary<string, ILookup> lookups = new Dictionary<string, ILookup>();
        IDictionary<string, IExistence> existences = new Dictionary<string, IExistence>();
        IDictionary<string, string> row = new Dictionary<string, string>();

        /// <summary>
        /// Compiles our test script file, and loads up our transform methods and litmusTests.
        /// </summary>
        [TestFixtureSetUp]
        public void SetupForAllTests()
        {
            var util = new TestUtils();
            var cSharpScript = util.GetResourceTextFile("TestScriptMethodsAndLitmusTests.cs");

            pathToScriptFile = Path.GetTempFileName();

            using (var textWriter = File.CreateText(pathToScriptFile)) {
                textWriter.Write(cSharpScript);
            }

            scriptMethodResolver = new ScriptMethodResolver();
            scriptMethodResolver.BuildResolversFromScriptFile(pathToScriptFile);
        }

        /// <summary>
        /// Cleans up the script file in the test directory.
        /// </summary>
        [TestFixtureTearDown]
        public void TestCleanup()
        {
            if (!String.IsNullOrEmpty(pathToScriptFile) && File.Exists(pathToScriptFile)) {
                try {
                    File.Delete(pathToScriptFile);
                } catch (Exception) {
                    
                }
            }
        }

        /// <summary>
        /// Calls a simple TransformMethod in our test script file that adds two numbers.
        /// </summary>
        [Test]
        public void CallAddTwoNumberScriptTest()
        {
            var args = new List<string> {"3", "4"};
            var result = scriptMethodResolver.ResolveTransformationMethod("AddTwoNumbers")
                                (0, lookups, existences, row, args);

            Assert.AreEqual("7", result);
        }

        /// <summary>
        /// Calls AddTwoNumbers with no arguments, and verifies it throws a BadRowException.
        /// </summary>
        [Test]
        [ExpectedException(typeof(BadRowException))]
        public void CallAddTwoNumberScriptWithNoArgumentsTest()
        {
            var args = new List<string>();
            scriptMethodResolver.ResolveTransformationMethod("AddTwoNumbers")
                                            (0, lookups, existences, row, args);

            Assert.Fail("Should have failed and thrown BadRowException");
        }

        /// <summary>
        /// Calls AddToNumbers with bad arguments and verifies it throws a BadRowException.
        /// </summary>
        [Test]
        [ExpectedException(typeof(BadRowException))]
        public void CallAddTwoNumberScriptWithBadArgumentsTest()
        {
            var args = new List<string> {"three", "4"};

            scriptMethodResolver.ResolveTransformationMethod("AddTwoNumbers")
                                            (0, lookups, existences, row, args);

            Assert.Fail("Should have failed and thrown BadRowException");
        }

        /// <summary>
        /// Calls ValidateTwoNumbers LitmusTest with valid inputs, and verifies it returns 
        /// true.
        /// </summary>
        [Test]
        public void CallValidateTwoNumbersLitmusTestTest()
        {
            var args = new List<string> {"3", "4"};
            var result = scriptMethodResolver.ResolveLitmusTest("ValidateTwoNumbers")
                                (lookups, existences, args);

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Calls ValidateTwoNumbers LitmusTest with no inputs, and verifies it returns false.
        /// </summary>
        [Test]
        public void CallValidateTwoNumbersLitmusTestWithNoArgumentsTest()
        {
            var args = new List<string>();
            var result = scriptMethodResolver.ResolveLitmusTest("ValidateTwoNumbers")
                                (lookups, existences, args);
           
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Calls ValidateTwoNumbers LitmusTest with bad inputs, and verifies it returns false.
        /// </summary>
        [Test]
        public void CallValidateTwoNumbersLitmusTestWithBadArgumentsTest()
        {
            var args = new List<string> { "three", "4" };
            var result = scriptMethodResolver.ResolveLitmusTest("ValidateTwoNumbers")
                                (lookups, existences, args);

            Assert.IsFalse(result);
        }
    }
}

