using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Sid.WebServices;
using Sid.Declarations;
using Newtonsoft.Json;

namespace Sid.WebServices.Test
{
    /// <summary>
    /// Test class for exercising Sid's basic functionality.
    /// </summary>
    public class WebServiceTests
    {
        public static IEnumerable<Assembly> SidModuleAssemblies = 
            new[] { Assembly.GetExecutingAssembly()};

        /// <summary>
        /// SN-1951 When the last parameter of the path was variable (a path parameters)
        /// the query string would get sucked up into it.
        /// </summary>
        [Test]
        public void TestMatchQueryInTerminalPathParam()
        {
            var browser = new Browser(SidModuleAssemblies);

            var response = browser.Get("/testcontent/testobject/15?x");
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Body);
            var testObj = JsonConvert.DeserializeObject<SidTestObject>(response.Body);

            Assert.AreEqual("15", testObj.Id);
        }

        /// <summary>
        /// Tests that [Bind] attributes works when there is no JSON Body sent; the 
        /// BindAttribute is asked to materialize a default SidTestObject in this case.
        /// Will fail if the BindAttribute failed to materialize a SidTestObject.
        /// </summary>

        [Test]
        public void TestBindAttributeNoPostBody()
        {
            var browser = new Browser(SidModuleAssemblies);
            var response = browser.Put("/testcontent/testobject/default");
            Assert.IsNotNull(response.Body);
            var testObj = JsonConvert.DeserializeObject<SidTestObject>(response.Body);
            Assert.AreEqual("sentinalValue", testObj.Id);
        }

        /// <summary>
        /// Tests the 'greedy star' functionality through the RestfulMethodDispatcher, 
        /// verifying that a PathAttribute is actually created, and that it has the correct 
        /// value.
        /// </summary>
        [Test]
        public void TestWebServiceGreedyStarFunctionality()
        {
            var browser = new Browser(SidModuleAssemblies);
            EndPointGroup group;

            var result = RestfulMethodDispatcher.WebMethodEndPoints.TryGetValue(HttpMethod.Get, 
                out group);

            Assert.IsTrue(result);

            Regex pathRegex;
            var path = "FrankZappa/DukeEllington";
            var fullPath = "testcontent/testobject/path/" + path;

            var uri = new Uri("http://localhost:8081/" + fullPath);
            var endPoint = group.ResolveRequest(uri, out pathRegex);
            Assert.IsNotNull(endPoint);

            var request = new RestfulTestServerRequest {
                Url = uri,
                HttpMethod = HttpMethod.Get,
                Match = pathRegex.Match(fullPath),
            };
            Assert.AreEqual(1, endPoint.Parameters.Count);
            var parm = endPoint.Parameters[0];
            Assert.IsTrue(parm is PathAttribute);

            var moduleInstance = Activator.CreateInstance(endPoint.ModuleType) as SidModule;
            var paramValue = parm.GetParamValue(request, moduleInstance) as string;
            Assert.IsNotNull(paramValue);
            Assert.AreEqual(path, paramValue);
        }
    }
}

