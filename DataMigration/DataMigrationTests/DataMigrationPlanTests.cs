using System;
using System.IO;
using System.Xml.Serialization;
using DataMigration;
using NUnit.Framework;

namespace DataMigrationTests
{
    /// <summary>
    /// DataMigrationPlanTests verifies that our mechanism for reading and deserializing
    /// DataMigrationPlan objects works correctly.
    /// </summary>
    [TestFixture]
    public class DataMigrationPlanTests
    {
        /// <summary>
        /// This test loads a DataMigrationPlan xml document stored as an embedded resource, 
        /// deserializes it, and verifies the C# object created is as expected in all respects.
        /// </summary>
        [Test]
        public void TestDataMigrationPlan ()
        {
            var util = new TestUtils ();
            var xmlStr = util.GetResourceTextFile ("TestDataMigrationPlan.xml");

            DataMigrationPlan plan;

            using (var reader = new StringReader (xmlStr)) {
                plan = (DataMigrationPlan)
                    new XmlSerializer (typeof(DataMigrationPlan)).Deserialize (reader);
            }

            Assert.IsNotNull (plan);
            Assert.AreEqual ("fileRoot", plan.LocalFilePathRoot);

            // Method Resolver
            Assert.IsTrue (plan.MethodResolverDescriptors.Length == 1);
            var mr = plan.MethodResolverDescriptors [0];
            Assert.AreEqual ("anotherCoolAssembly", mr.Assembly);
            Assert.AreEqual ("zz", mr.Tag);

            // DataSourcePlan
            Assert.IsTrue (plan.DataSourcePlans.Length == 1);
            var dataSourcePlan = plan.DataSourcePlans [0];
            Assert.AreEqual ("XZX", plan.DataSourcePlans [0].DataSourceCode);
            Assert.AreEqual ("somewhereInS3", dataSourcePlan.FilesLocation);
            Assert.IsFalse (dataSourcePlan.PostProcess);
            Assert.IsNotNull (dataSourcePlan.FeedStager);
            Assert.IsNotNull (dataSourcePlan.FeedFilePlans);
            Assert.IsTrue (dataSourcePlan.FeedFilePlans.Length == 1);

            // FeedStager
            var feedStager = dataSourcePlan.FeedStager;
            Assert.AreEqual ("anotherFakeFileName", feedStager.Assembly);
            Assert.IsNotNull (feedStager.Properties);
            Assert.IsTrue (feedStager.Properties.Length == 1);

            // FeedStager property
            var feedStagerProperties = feedStager.Properties [0];
            Assert.AreEqual ("foo", feedStagerProperties.Name);
            Assert.AreEqual ("bar", feedStagerProperties.Value);

            // FeedFilePlan
            var feedFilePlan = dataSourcePlan.FeedFilePlans [0];
            Assert.AreEqual ("fakeFileName", feedFilePlan.FileName);
            Assert.IsNull (feedFilePlan.LocalFile);
            Assert.IsNotNull (feedFilePlan.Parser);
            Assert.AreEqual (1, feedFilePlan.SkipLines);
            Assert.AreEqual ("fakeLocation", feedFilePlan.StagingLocation);
            Assert.IsNotNull (feedFilePlan.TransformMaps);
            Assert.IsTrue (feedFilePlan.TransformMaps.Length == 2);

            // parser
            var parser = feedFilePlan.Parser;
            Assert.AreEqual ("aWayCoolAssembly", parser.Assembly);
            Assert.IsNotNull (parser.Properties);
            Assert.IsTrue (parser.Properties.Length == 1);

            // parser property
            var parserProperty = parser.Properties [0];
            Assert.AreEqual ("Parsee", parserProperty.Name);
            Assert.AreEqual ("Beets", parserProperty.Value);
        
            // TransformMaps
            var maps = feedFilePlan.TransformMaps;

            // first name
            var firstNameMap = maps [0];
            Assert.IsNotNull (firstNameMap);
            Assert.AreEqual ("FirstName", firstNameMap.DestCol);
            Assert.AreEqual ("0", firstNameMap.SrcColumns);

            // last name
            var lastNameMap = maps [1];
            Assert.IsNotNull (lastNameMap);
            Assert.AreEqual ("LastName", lastNameMap.DestCol);
            Assert.AreEqual ("1", lastNameMap.SrcColumns);
        }
    }
}
