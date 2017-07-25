using System;
using System.Collections.Generic;
using System.Net;
using Sid.WebServices;
using Sid.Declarations;

namespace Sid.WebServices.Test
{
    /// <summary>
    /// Test Module to use in Sid unit tests.
    /// </summary>
    [RootPath("testcontent/testobject", Description = "Interface module for Sid Unit Testing")]
    public class SidTestModule : SidModule
    {
        Dictionary<string, SidTestObject> objectStore = new Dictionary<string, SidTestObject> {
            {"15", new SidTestObject {Id="15", StringProp="string property value"}},
        };

        /// <summary>
        /// Endpoint that just tests GET functionality.
        /// </summary>
        /// <returns>A test object.</returns>
        /// <param name="id">Id of object to return.</param>
        [Get("{id}", Description="Simple endpoint that returns a test object")]
        SidTestObject GetTestObject([Path] string id)
        {
            if (!objectStore.ContainsKey(id)) {
                throw new ApiException(HttpStatusCode.BadRequest, 
                    string.Format("Unknown object {0}", id));
            }
            return objectStore[id];
        }

        /// <summary>
        /// Simple endpoint that accepts PUTing an object to it. 
        /// </summary>
        /// <returns>The default object.</returns>
        /// <param name="defaultTestObject">Default test object.</param>
        [Put("default", Description="Simple endoint that tests default object materialization"+
            " from Bind attribute")]
        SidTestObject PutDefaultObject([Bind] SidTestObject defaultTestObject)
        {
            if (defaultTestObject == null) {
                throw new ApiException(HttpStatusCode.BadRequest, "Null test object");
            }
            defaultTestObject.Id = "sentinalValue";
            return defaultTestObject;
        }

        /// <summary>
        /// Endpoint for testing the greedy star functionality.
        /// </summary>
        /// <returns>The path paseed in.</returns>
        /// <param name="path">Path.</param>
        [Get("path/{path*}", Description="Endpoint that tests greedy star functionality")]
        string GreedyStarEndpoint([Path] string path)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ApiException(HttpStatusCode.BadRequest, "Null path");
            }
            return path;
        }
    }

    /// <summary>
    /// Test object class to use for Sid unit tests.
    /// </summary>
    public class SidTestObject
    {
        public string Id { get; set; }
        public string StringProp {get; set;}
    }
}

