using System.Collections.Generic;
using System.Reflection;
using DataMigration;
using NUnit.Framework;


namespace DataMigrationTests
{
    [TestFixture]
    public class ReflectionResolverHelperTests
    {
        IDictionary<string, IDictionary<string, TransformMethod>> transformMethods;
        IDictionary<string, IDictionary<string, LitmusTest>> litmusTests;

        IDictionary<string, ILookup> lookups = new Dictionary<string, ILookup>();
        IDictionary<string, IExistence> existences = new Dictionary<string, IExistence>();
        IDictionary<string, string> row = new Dictionary<string, string>();

        const string validEmail = "joe@beet.com";
        const string invalidEmail = "invalidEmailNoAtSign";


        /// <summary>
        /// Loads up our transform methods and litmusTests.
        /// </summary>
        [TestFixtureSetUp]
        public void SetupForAllTests()
        {
            transformMethods = new Dictionary<string, IDictionary<string, TransformMethod>>();
            litmusTests = new Dictionary<string, IDictionary<string, LitmusTest>>();

            var assembly = Assembly.LoadFrom("DataMigration.exe");
            var resolverHelper = new ReflectionResolverHelper();

            resolverHelper.Initialize(assembly);

            // 'cx' is our convention for CompiledXForms.  Since we use this for the actual
            // product, we'll also use it for the tests.   
            transformMethods["cx"] = resolverHelper.TransformMethods;
            litmusTests["cx"] = resolverHelper.LitmusTests;
        }

        /// <summary>
        /// This invokes the ValidateEmail TransformMethod, and verifies it returns the valid
        /// email string passed in.
        /// </summary>
        [Test]
        public void InvokeTransformMethodTest()
        {
            var arguments = new List<string> { validEmail };

            var returnedEmail =
                transformMethods["cx"]["ValidateEmail"](0, lookups, existences, row, arguments);

            Assert.AreEqual(validEmail, returnedEmail);
        }

        /// <summary>
        /// This invokes the ValidateEmail TransformMethod with an invalid email string, and
        /// verifies a BadRow exception is thrown with the correct Reason.
        /// </summary>
        [Test]
        [ExpectedException(typeof(BadRowException))]
        public void InvokeTransformMethodWithBadArgumentTest()
        {
            var arguments = new List<string> { invalidEmail };

            try
            {
                transformMethods["cx"]["ValidateEmail"](
                                    0, lookups, existences, row, arguments);
            }
            catch (BadRowException bre)
            {
                Assert.AreEqual("Badly formed email address", bre.Reason);
                throw;
            }
        }

        /// <summary>
        /// Invokes the EnsureValidEmail LitmusTest with a valid email string, and that it 
        /// correctly returns true.
        /// </summary>
        [Test]
        public void InvokeLitmusTestTest()
        {
            var arguments = new List<string> { validEmail };
            var passed = litmusTests["cx"]["EnsureValidEmail"](lookups, existences, arguments);
            Assert.IsTrue(passed);
        }

        /// <summary>
        /// Invokes the EnsureValidEmail LitmusTest with an invalid email string, and that
        /// it correctly returns false.
        /// </summary>
        [Test]
        public void InvokeLitmusTestWithBadArgumentTest()
        {
            var arguments = new List<string> { invalidEmail };
            var passed = litmusTests["cx"]["EnsureValidEmail"](lookups, existences, arguments);
            Assert.IsFalse(passed);
        }

    }
}

