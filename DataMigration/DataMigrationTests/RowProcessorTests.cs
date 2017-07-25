using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml.Serialization;
using DataMigration;
using NUnit.Framework;

namespace DataMigrationTests
{
    /// <summary>
    /// The intent of tests here is to test the ability of the RowProcessor class to build a 
    /// proper transformed row, and its ability to add it to a DataTable propertly.
    /// </summary>
    [TestFixture]
    public class RowProcessorTests
    {
        FeedFilePlan feedFilePlan;
        IDictionary<string, IMethodResolver> methodResolver;

        /// <summary>
        /// Loads a FeedFilePlan from embedded resource, then instantiates a test method 
        /// resolver, which provides a couple of stub methods.
        /// </summary>
        [TestFixtureSetUp]
        public void SetupForAllTests ()
        {
            var util = new TestUtils ();
            var xmlStr = util.GetResourceTextFile ("TestFeedFilePlan.xml");

            using (var reader = new StringReader (xmlStr)) {
                feedFilePlan = (FeedFilePlan)
                      new XmlSerializer (typeof(FeedFilePlan)).Deserialize (reader);
            }

            methodResolver = new Dictionary<string, IMethodResolver> {
                { "zz", new TestMethodResolver () },
            };
        }

        /// <summary>
        /// Tests the Row Processor's ability to transform the row properly, given some data 
        /// and a set of TransformMaps.
        /// </summary>
        [Test]
        public void RowTransformationTest ()
        {
            var rowProcessor = new RowProcessor {
                MethodResolvers = methodResolver
            };

            var result = rowProcessor.TransformRow (1, "ARC", 0, feedFilePlan, new[] {
                "Sterling",
                "Archer",
                "40",
                "85",
                "2",
                "anotherIgnoredValue"
            });

            Assert.IsTrue (result.Keys.Count == 4);
            Assert.IsTrue (result.ContainsKey ("FirstName"));
            Assert.IsTrue (result.ContainsKey ("LastName"));
            Assert.IsTrue (result.ContainsKey ("ComputedValue"));
            Assert.IsTrue (result.ContainsKey ("FullName"));

            Assert.AreEqual ("Sterling", result ["FirstName"]);
            Assert.AreEqual ("Archer", result ["LastName"]);
            Assert.AreEqual ("42", result ["ComputedValue"]);
            Assert.AreEqual ("Mr. Sterling Archer, the world's most dangerous spy.", 
                result ["FullName"]);
        }

        [Test]
        public void AddToDataTableTest ()
        {
            var feedFilePlan = new FeedFilePlan {
                TransformMaps = new [] {
                    new TransformMapping {
                        DestCol = "intColumn",  
                        SrcColumns = "0",
                        Type = "int",
                    },
                    new TransformMapping {
                        DestCol = "strColumn",
                        SrcColumns = "1",
                    },
                    new TransformMapping {
                        DestCol = "boolColumn",
                        SrcColumns = "2",
                        Type = "bool",
                    },
                    new TransformMapping {
                        DestCol = "floatColumn",
                        SrcColumns = "3",
                        Type = "float",
                    },
                },
            };
            var dataTable = new DataTable ();
            dataTable.Columns.Add (FeedProcessor.LoadNumberString, typeof(Int32));
            dataTable.Columns.Add (FeedProcessor.DataSourceCodeString, typeof(String));
            dataTable.Columns.Add ("intColumn", typeof(int));
            dataTable.Columns.Add ("strColumn", typeof(string));
            dataTable.Columns.Add ("boolColumn", typeof(bool));
            dataTable.Columns.Add ("floatColumn", typeof(float));

            var row = new Dictionary<string, string> {
                { "intColumn", "42" },
                { "strColumn", "foo" },
                { "boolColumn", "true" },
                { "floatColumn", "10.0" },
            };

            var rowProcessor = new RowProcessor ();
            rowProcessor.AddRowToDataTable (feedFilePlan, 1, "TST", row, dataTable);

            Assert.AreEqual (1, dataTable.Rows.Count);
            var dataRow = dataTable.Rows [0];
            Assert.IsNotNull (dataRow);

            var intVal = (int)dataRow ["intColumn"];
            var stringVal = (string)dataRow ["strColumn"];
            var boolVal = (bool)dataRow ["boolColumn"];
            var floatVal = (float)dataRow ["floatColumn"];

            Assert.AreEqual (42, intVal);
            Assert.AreEqual ("foo", stringVal);
            Assert.AreEqual (true, boolVal);
            Assert.AreEqual (10.0f, floatVal);
        }

        /// <summary>
        /// Provides some minimal transformation methods, so as to exercise the RowProcessor's 
        /// ability to process rows using said transformation methods.
        /// </summary>
        sealed class TestMethodResolver : IMethodResolver
        {

            #region IMethodResolver implementation

            public TransformMethod ResolveTransformationMethod (string xformName)
            {
                if (xformName.Equals ("ComputeMe")) {
                    return (loadNumber, lookups, existenceObjects, rowInProgress, args) => {
                        var firstNumber = int.Parse (args [0]);
                        var secondNumber = int.Parse (args [1]);
                        return (firstNumber + secondNumber).ToString ();
                    };
                } 

                if (xformName.Equals ("MakeFullName")) {
                    return ((loadNumber, lookups, existenceObjects, rowInProgress, args) => 
                        "Mr. " + args [0] + " " + args [1] +
                    ", the world's most dangerous spy.");
                }
                throw new Exception (String.Format ("Unexpected method {0}", xformName));
            }

            #endregion

        }
    }
}
