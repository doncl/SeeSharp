using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTy.Declarations;

namespace RESTy.WebServices
{
    /// <summary>
    /// End point group.  These are all the EndPoints for a particular HTTP Method.
    /// </summary>
    public class EndPointGroup
    {
        public List<EndPointDescriptor> EndPoints { get; set; }

        // The Trie is used to resolve requests to their appropriate method.
        public Trie Trie { get; set; }

        public EndPointGroup(List<EndPointDescriptor> endPoints)
        {
            Trie = new Trie();
            EndPoints = endPoints;
            foreach (var endPoint in EndPoints) {
                var oneEndPointTrie = new Trie();
                oneEndPointTrie.Insert(endPoint.Path, endPoint);
                ValidatePathParameters(oneEndPointTrie, endPoint);
                Trie.Merge(oneEndPointTrie, endPoint.Path);
            }
        }

        // Forwards the resolution request to the Trie.   We can change this here if we elect
        // to use a different method to resolve requests.
        public virtual IEndPointDescriptor ResolveRequest(Uri uri, out Regex pathRegex) {
            return Trie.ResolveRequest(uri, out pathRegex);
        }

        /// <summary>
        /// Validates that Path Parameters match the replaceable tokens in the 
        /// BaseRouteAttribute on the method.
        /// </summary>
        /// <param name="trie"></param>
        /// <param name="endPoint"></param>
        void ValidatePathParameters(Trie trie, EndPointDescriptor endPoint)
        {
            // If we don't have any parameters, we're done.
            if (endPoint.Parameters == null || !endPoint.Parameters.Any()) {
                return;
            }

            // If none of the parameters are Path Parameters, then we're done.
            var pathParameters = endPoint.Parameters.Where(p => p is PathAttribute);
            if (!pathParameters.Any()) {
                return;
            }

            var pathTokens = trie.GetReplaceableTokens(endPoint.Path);

            // Now, check that there are no replaceable tokens from the BaseRouteAttribute
            // that lack counterparts in the parameter list of the method.
            foreach (var pathParameter in pathParameters) {
                var matchingToken = pathTokens.SingleOrDefault(t => 
                    t.Equals(pathParameter.ParameterInfo.Name));

                if (matchingToken == null) {
                    throw new Exception(String.Format("FATAL ERROR - Method {0} has a path " +
                        "parameter {1} whose name is not found in the replaceable tokens in " +
                        "its BaseRoutePath {2}", endPoint.Name, 
                        pathParameter.ParameterInfo.Name, endPoint.Path));        
                }
            }

            // Just compare the counts now.  
            var pathParametersCount = pathParameters.Count();
            var pathTokensCount = pathTokens.Count();

            if (pathParametersCount != pathTokensCount) {
                var parmNames = String.Join(",",  
                        pathParameters.Select(pp => pp.ParameterInfo.Name));
                var tokenNames = String.Join(",", pathTokens);

                throw new Exception(String.Format("FATAL ERROR - Method {0} has {1} " 
                  + "parameterized tokens in BaseRouteAttribute {2}, tokens = \"{3}\", and it " 
                  + "has {4} method signature [Path] parameters \"{5}\".",
                    endPoint.Name, pathTokensCount, endPoint.Path, tokenNames, 
                    pathParametersCount, parmNames));
            }
        }
    }
}
