using DataMigration;
using NUnit.Framework;

namespace DataMigrationTests
{
    /// <summary>
    /// These are tests for the shared DataMigration utilities.
    /// </summary>
    [TestFixture]
    public class UtilitiesTests
    {
        /// <summary>
        /// PostProcessors have a collection of properties declaratively expressed in the 
        /// DataSourcePlan.  Typically the pattern for secrets is for the Plan to authored with 
        /// a key into the App.Config, which then returns the secret, or thing that has secrets
        /// in it (like database connection strings).  This keeps all secrets out of the Plan,
        /// and means the plan is portable from one system to the next; only the App.Config
        /// changes.  
        /// TestGetConnectionString verfies this method works; there's an App.Config for the
        /// Test project that has a TestConnectionString in it.
        /// </summary>
        [Test]
        public void TestGetConnectionString()
        {
            var propertyKey = "foxyConnectionString";
            var properties = new [] {
                new DescriptorProperty {
                    Name= propertyKey,
                    Value="TestConnectionString",
                }
            };

            var connectionString = 
                Utilities.GetConfigString(propertyKey, properties);
            Assert.AreEqual("The quick brown fox", connectionString);
        }

        /// <summary>
        /// Tests that passing a property key to something that does not exist in App.Config
        /// results in a null return. 
        /// </summary>
        [Test]
        public void TestNegativeCaseGetConnectionString()
        {
            var propertyKey = "KeyToNonExistentAppSetting";

            var properties = new [] {
                new DescriptorProperty {
                    Name = propertyKey,
                    Value = "NonExistentAppSetting",
                },
            };

            var value = Utilities.GetConfigString(propertyKey, properties);
            Assert.IsNullOrEmpty(value);
        }
    }
}
