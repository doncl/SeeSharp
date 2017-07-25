using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Sid.Declarations;

namespace Sid.WebServices.Test
{
    public class TrieTests
    {
        [Test]
        public void TestTrieMerge()
        {
            var endPointDescriptor = new TestEndPointDescriptor {
                Name = "foo",
            };

            var firstTrie = new Trie();
            firstTrie.Insert("firstSegment/secondSegment", null);
            var secondTrie = new Trie();

            var path = "firstSegment/anotherSegment";

            secondTrie.Insert(path,  endPointDescriptor);

            firstTrie.Merge(secondTrie, path);

            var uri = new Uri("http://localhost:8081/firstSegment/anotherSegment");

            Regex rgx;

            var endPoint = firstTrie.ResolveRequest(uri, out rgx);
            Assert.AreEqual("foo", endPoint.Name);
        }

        [Test]
        public void TestTrieMergeReplaceableTokenOnFirst()
        {
            var endPointDescriptor = new TestEndPointDescriptor {
                Name = "foo",
            };

            var firstTrie = new Trie();
            firstTrie.Insert("firstSegment/secondSegment/{parm1}", null);
            var secondTrie = new Trie();

            var path = "firstSegment/anotherSegment";

            secondTrie.Insert(path,  endPointDescriptor);

            firstTrie.Merge(secondTrie, path);

            var uri = new Uri("http://localhost:8081/firstSegment/anotherSegment");

            Regex rgx;

            var endPoint = firstTrie.ResolveRequest(uri, out rgx);
            Assert.AreEqual("foo", endPoint.Name);
        }


        /// <summary>
        /// SP-325 - this test regresses the issue when putting a static endpoint right after
        /// (i.e. in the same XXXModule.cs file) one with exact same path but replaceable token
        /// on the end (i.e. PathParameter).   The Trie merge code was hiding the static one.
        /// </summary>
        [Test]
        public void TestStaticEndPointAfterTokenizedEndPoint()
        {
            var endPointDescriptor = new TestEndPointDescriptor {
                Name = "foo",
            };

            var firstTrie = new Trie();
            firstTrie.Insert("content/media/{id}", endPointDescriptor);
            var secondTrie = new Trie();

            var path = "content/media/search";

            secondTrie.Insert(path,  null);

            firstTrie.Merge(secondTrie, path);

            var uri = new Uri("http://localhost:8081/content/media/12345");

            Regex rgx;

            var endPoint = firstTrie.ResolveRequest(uri, out rgx);
            Assert.AreEqual("foo", endPoint.Name);
        }

        /// <summary>
        /// Tests the trie greedy star functionality.
        /// </summary>
        [Test]
        public void TestTrieGreedyStarFunctionality()
        {
            var endPointDescriptor = new TestEndPointDescriptor {
                Name = "foo",
            };

            var firstTrie = new Trie();
            firstTrie.Insert("api-docs/{path*}", endPointDescriptor);

            var uri = new Uri("http://localhost:8081/api-docs/content/stories");

            Regex rgx;

            var endPoint = firstTrie.ResolveRequest(uri, out rgx);
            Assert.AreEqual("foo", endPoint.Name);            
        }

        [Test]
        public void TestTrieMergeReplaceableTokenOnSecond()
        {
            var endPointDescriptor = new TestEndPointDescriptor {
                Name = "foo",
            };

            var firstTrie = new Trie();
            firstTrie.Insert("firstSegment/secondSegment", null);
            var secondTrie = new Trie();

            var path = "firstSegment/anotherSegment/{parm1}";

            secondTrie.Insert(path,  endPointDescriptor);

            firstTrie.Merge(secondTrie, path);

            var uri = new Uri("http://localhost:8081/firstSegment/anotherSegment/42");

            Regex rgx;

            var endPoint = firstTrie.ResolveRequest(uri, out rgx);
            Assert.AreEqual("foo", endPoint.Name);
        }

        [Test]
        public void TestTrieMergeReplaceableTokenOnBoth()
        {
            var endPointDescriptor = new TestEndPointDescriptor {
                Name = "foo",
            };

            var firstTrie = new Trie();
            firstTrie.Insert("firstSegment/secondSegment/{parm1}", null);
            var secondTrie = new Trie();

            var path = "firstSegment/{parm1}/anotherSegment";

            secondTrie.Insert(path,  endPointDescriptor);

            firstTrie.Merge(secondTrie, path);

            var uri = new Uri("http://localhost:8081/firstSegment/42/anotherSegment");

            Regex rgx;

            var endPoint = firstTrie.ResolveRequest(uri, out rgx);
            Assert.AreEqual("foo", endPoint.Name);
        }

        [Test]
        public void TestTrieMergeSecondWithNoToken()
        {
            var endPointDescriptor = new TestEndPointDescriptor {
                Name = "foo",
            };

            Regex rgx;
            var path = "content/sites";
            var uri = new Uri("http://localhost:8081/content/sites");

            var firstTrie = new Trie();
            firstTrie.Insert("content/sites/{siteId}", null);
            var secondTrie = new Trie();
            secondTrie.Insert(path,  endPointDescriptor);
            firstTrie.Merge(secondTrie, path);

            var endPoint = firstTrie.ResolveRequest(uri, out rgx);
            Assert.AreEqual("foo", endPoint.Name);
        }

        [Test]
        public void TestTrieMergeSegAfterToken()
        {
            var endPointDescriptor1 = new TestEndPointDescriptor {
                Name = "foo",
            };
            var endPointDescriptor2 = new TestEndPointDescriptor {
                Name = "bar",
            };

            Regex rgx;
            var path = "content/sites/{siteId}/settings";
            var uri1 = new Uri("http://localhost:8081/content/sites/2343");
            var uri2 = new Uri("http://localhost:8081/content/sites/2343/settings");

            var insertTrie = new Trie();
            insertTrie.Insert("content/sites/{id}", endPointDescriptor1);
            insertTrie.Insert(path, endPointDescriptor2);

            var endPointPre = insertTrie.ResolveRequest(uri1, out rgx);
            Assert.AreEqual("foo", endPointPre.Name);

            endPointPre = insertTrie.ResolveRequest(uri2, out rgx);
            Assert.AreEqual("bar", endPointPre.Name);

            var mergeTrie = new Trie();
            mergeTrie.Insert("content/sites/{id}", endPointDescriptor1);

            var singleEndPointTrie = new Trie();
            singleEndPointTrie.Insert(path,  endPointDescriptor2);

            mergeTrie.Merge(singleEndPointTrie, path);

            var endPoint = mergeTrie.ResolveRequest(uri1, out rgx);
            Assert.AreEqual("foo", endPoint.Name);

            endPoint = mergeTrie.ResolveRequest(uri2, out rgx);
            Assert.AreEqual("bar", endPoint.Name);
        }
    }

    internal sealed class TestEndPointDescriptor : IEndPointDescriptor
    {
        public Type ModuleType { get; set; }  
        public string Path { get; set; }      
        public HttpMethod WebMethod { get; set; } 
        public MethodInfo MethodInfo { get; set; }
        public BaseRouteAttribute BaseRouteAttribute {get; set;}
        public List<BaseParamAttribute> Parameters { get; set; }
        public string Name {get; set; }
        public bool Public {get; set;}
    }
}

