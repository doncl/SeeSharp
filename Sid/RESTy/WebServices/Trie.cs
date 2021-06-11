using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace RESTy.WebServices
{
    /// <summary>
    /// This is a trie of uri segments, not characters (as is more frequently the case).
    /// It's responsible for storing the segments of all the endpoint paths (RootPathAttribute
    /// plus BaseRouteAttribute concatenated) and building pathRegexes for them so that 
    /// Path Parameters can be resolved properly, and the requests can go to the correct 
    /// method.
    /// </summary>
    public class Trie
    {
        // The segment that belongs to this Trie.
        public string Segment { get; set; }

        // Terminal nodes have EndPointDescriptors.
        public IEndPointDescriptor EndPoint { get; set; }

        // Child Nodes.
        public List<Trie> Children { get; set; }

        public bool IsPath {
            get {
                return Path != null;
            }
        }

        public string Path { get; set; }

        public bool IsPathRegex {
            get {
                return PathRegex != null;
            }
        }

        public Regex PathRegex { get; set; }

        public bool IsNodeReplaceableToken { get; set; }

        // Used for expressions like "{path*}".  We're not supporting anything more
        // sophisticated.
        public bool IsGreedyStar { get; set; }

        public bool Terminal { get; set; }

        /// <summary>
        /// The default constructor is used for the root of the Trie.
        /// </summary>
        public Trie() 
        {
        }

        /// <summary>
        /// This constructor is used when a child Trie node is being created.
        /// </summary>
        /// <param name="segment">Uri segment.</param>
        /// <param name="replaceableToken">Is this a replaceable token node?</param>
        /// <param name="greedyStar">Is this a 'greedy star' node, e.g.' {path*}</param>
        public Trie(string segment, bool replaceableToken, bool greedyStar) {
            Segment = segment;
            IsNodeReplaceableToken = replaceableToken;
            IsGreedyStar = greedyStar;
        }

        /// <summary>
        /// Given a request, returns the EndPointDescriptor for making the call, and a 
        /// pathRegex for use in resolving path parameters.
        /// </summary>
        /// <returns>An EndPointDescriptor.</returns>
        /// <param name="uri">The Uri to resolve.</param>
        /// <param name="pathRegex">(out) an optional regex for use when resolving path
        /// parameters.</param>
        public IEndPointDescriptor ResolveRequest(Uri uri, out Regex pathRegex) 
        {
            pathRegex = null;

            var segs = GetSegs(uri);
            var trie = this;
            var i = 0;
            while (trie != null && i < segs.Length) {
                trie = trie.Next(segs, i++);
                if (trie == null || !trie.IsPath) {
                    continue;
                }
                if (i == segs.Length || trie.IsGreedyStar) {
                    pathRegex = trie.PathRegex;
                    return trie.EndPoint;
                }
            }
            return null;
        }

        /// <summary>
        /// Given a Uri, returns the segments as a string array, with the whacks ('/') trimmed
        /// off.
        /// </summary>
        /// <returns>The segments as string array.</returns>
        /// <param name="uri">The Uri input.</param>
        static string[] GetSegs(Uri uri) 
        {
            var segs = uri.Segments.Select(s => s.Trim('/'))
                .Where(ss => !String.IsNullOrEmpty(ss)).ToArray();
            return segs;
        }

        /// <summary>
        /// Standard Trie stuff.  Walks the Trie.
        /// </summary>
        /// <param name="segments">List of segments.</param>
        /// <param name="currIndex">Current segment we're processing.</param>
        /// <param name="merging">We're in middle of merge</param>
        public Trie Next(String[] segments, int currIndex,
                            bool merging = false) 
        {
            var segment = segments[currIndex];
            if (Children == null) {
                return null;
            }

            // Look for a literal exact match first.  That always wins.
            var exactMatch = Children.SingleOrDefault(c => c
                .Segment.Equals(segment, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null) {
                return exactMatch;
            }

            // If we are merging, we do NOT consider a replaceable token as a match; when 
            // merging, if you hit a replaceable token node in the destination (merged-to)
            // data structure, that immediately means you need a new sub-route. On the other
            // hand, insertions are always done on a brand-new empty Trie, and so there's 
            // nothing to discover.  The third case, at request time, you do want to look for
            // replaceable token nodes, in order to see if this portion of the incoming 
            // request path represents a match. 
            if (!merging) {
                var matchingChild = LookForReplacableTokenPath(segments);
                if (matchingChild != null) {
                    return matchingChild;
                }
            }

            return null;
        }

        /// <summary>
        /// Looks for a child with a replaceable token path in it.  
        /// </summary>
        /// <returns>The Trie node that matches the input segment.</returns>
        /// <param name="segments">Segments.</param>
        private Trie LookForReplacableTokenPath(string[] segments)
        {
            /* ********************************************************************************
            if you have intermediate paths in the trie for routes like:

            /a/b
            /a/{id}     --  where {id} means a replaceable token i.e. PathAttribute
            /a/{siteId}\settings
            /a/c

            which sorta looks like:

                             (a)
                     ________ |  ______________________
                   |               /    \              |
                  (b)          ({id}  {(siteId}       (c)
                                             |
                                           (settings)

            You need to be able to disambiguate between these.   The path '/content/sites/2345' 
            looks very similar to the Trie (from a single-node standpoint) to 
            '/content/sites/2345/settings'.), if you only look at each Trie node in isolation.

            To disambiguate these, we leverage the fact hat every single terminal node (with
            replaceable parameters in the path) in the Trie is storing a complete Regex - it's 
            already there, was created at startup time; it's ready for use.

            So, in this example, when at point (a), and the current segment is named '2345', to 
            find a path to choose from, first look at (b) and (c), and see if a literal match 
            exists. If so, that always takes precedence.  If not, then look at {id}, and 
            {siteId}, and get the regexes from their terminal Trie nodes, and match it 
            against the entire path (e.g. a/{siteId}/settings).
    
            There can only be one at best one match, and the code will throw an exception if 
            more than one appears; this means developers have created un-disambiguatable 
            endpoint paths (which is entirely possible), and they need to change one of those 
            paths to restore uniqueness.  
            **********************************************************************************/

            // Look for all the kids with replaceable tokens.
            var tokenKids = Children.Where(k => k.IsNodeReplaceableToken).ToList();

            // If any, then we have a potential match.
            if (!tokenKids.Any()) {
                return null;
            }
            // If only one, we're good.
            if (tokenKids.Count == 1) {
                return tokenKids[0];
            }

            // If more than one, we have to disambiguate which one it is.  This is
            // where we're going to have to match on the regex.
            var path = string.Join("/", segments);
            var pathRegexes = new List<KeyValuePair<Regex, Trie>>();
            foreach (var tokenKid in tokenKids) {
                pathRegexes.AddRange(tokenKid.GetPathRegexes(tokenKid));
            }
            var matches = pathRegexes.Where(kvp => kvp.Key.IsMatch(path))
                .ToList();

            if (matches.Count == 0) {
                return null;
            }
            if (matches.Count > 1) {
                throw new Exception(
                    string.Format("Path {0} matches more than one endpoint regex, matches {1}",
                                path,
                                string.Join(" and ", matches.Select(m => m.Key.ToString()))));
            }

            // Found the trie node that matches.
            return matches[0].Value;
        }

        /// <summary>
        /// Given an intermediate node, gets the list of terminal regexes for the various 
        /// sub-paths that sprout from it.
        /// </summary>
        /// <returns>A list of regexes, paired with the trie from *this* level that is the
        ///root to the matching sub-path.  Basically, each one just uses the passed-in trie
        /// as a value in the keyvaluepair, but this is called for each child, and aggregated;
        /// there's a different trie for each 'batch' of these.</returns>
        /// <param name="trie">Trie.</param>
        IEnumerable<KeyValuePair<Regex, Trie>> GetPathRegexes(Trie trie) 
        {
            var regexes = new List<KeyValuePair<Regex, Trie>>(); 
            if (IsPathRegex) {
                regexes.Add(new KeyValuePair<Regex, Trie>(PathRegex, trie));
            }
            if (Children == null) {
                return regexes;
            }
            foreach (var child in Children) {
                regexes.AddRange(child.GetPathRegexes(trie)); // pass in parent Trie.
            }
            return regexes;
        }

        /// <summary>
        /// Merges a single path trie to an already-existing one.
        /// </summary>
        /// <param name="trieToMerge">Single path trie to merge.</param>
        /// <param name="trieToMergePath">Path for merging trie.</param>
        public void Merge(Trie trieToMerge, string trieToMergePath) {
            var thisTrie = this;
            var segs = trieToMergePath.Trim('/').Split('/');

            var i = 0;
            while (i < segs.Length) {
                trieToMerge = trieToMerge.Next(segs, i);
                if (trieToMerge == null) {
                    break;
                }
                var next = thisTrie.Next(segs, i, true);
                i++;

                // If Next returns null, or the segment it returns does not equal this
                // one, and it's not a replaceable token, then create a new one here.
                if (next == null ||
                    (!next.Segment.Equals(trieToMerge.Segment) &&
                    !next.IsNodeReplaceableToken)) {
                    if (thisTrie.Children == null) {
                        thisTrie.Children = new List<Trie>();
                    }
                    thisTrie.Children.Add(trieToMerge);
                    return;
                }
                thisTrie = next;
            }
            ShallowClone(thisTrie, trieToMerge, trieToMergePath);
        }

        /// <summary>
        /// Returns the replaceable tokens for a segment for a given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public List<string> GetReplaceableTokens(string path) {
            var trie = this;
            var tokens = new List<string>();
            var segments = path.Split('/');
            //foreach (var segment in segments) {
            for (var i = 0; i < segments.Length; i++) {
                trie = trie.Next(segments, i);
                if (trie == null) {
                    throw new Exception(String.Format("FATAL ERROR - could not find segment " +
                    "{0} in Trie when searching for replaceable tokens for path {1}",
                        segments[i], path));
                }
                if (trie.IsNodeReplaceableToken) {
                    tokens.Add(trie.Segment.TrimStart('{').TrimEnd('}', '*'));
                }
            }
            return tokens;
        }

        /// <summary>
        /// Clone the trie node, but not its children.
        /// </summary>
        /// <param name="source">source</param>
        /// <param name="trieToMergePath">Path for node.</param>
        /// <param name="destination">Destination.</param>
        static void ShallowClone(Trie destination, Trie source, string trieToMergePath) 
        {
            destination.Path = trieToMergePath;
            destination.Terminal = source.Terminal;
            destination.IsGreedyStar = source.IsGreedyStar;

            // Don't allow new shorter terminal paths to override the replaceable token nature
            // of longer paths that share a subpath.
            if (source.IsNodeReplaceableToken) {
                destination.IsNodeReplaceableToken = source.IsNodeReplaceableToken;
            }
            destination.PathRegex = source.PathRegex;
            destination.EndPoint = source.EndPoint;
        }

        /// <summary>
        /// Loads up a trie with a complete path.
        /// </summary>
        /// <param name="path">Path to break into nodes and store.</param>
        /// <param name="endPoint">EndPointDescriptor to store at the terminus.</param>
        public void Insert(string path, IEndPointDescriptor endPoint) 
        {
            var createRegex = false;
            var t = this;
            var inputSegments = path.Split('/');
            var regexPathSegments = new List<string>();

            var i = 0;
            foreach (var segment in inputSegments) {
                var greedyStar = false;
                var regexString = CheckIfReplaceableToken(segment, out greedyStar);
                regexPathSegments.Add(regexString ?? segment);
                var segmentIsRegex = !string.IsNullOrEmpty(regexString);
                createRegex = createRegex | segmentIsRegex;

                var next = t.Next(inputSegments, i++);
                if (next == null) {
                    next = new Trie(segment, segmentIsRegex, greedyStar);
                    if (t.Children == null) {
                        t.Children = new List<Trie>();
                    }
                    t.Children.Add(next);
                }
                t = next;
                if (greedyStar) {
                    break;
                }
            }
            t.Terminal = true;
            t.Path = path;
            t.EndPoint = endPoint;
            if (!createRegex) {
                return;
            }
            t.PathRegex = new Regex("^/?" + string.Join("/", regexPathSegments) + "$", 
                RegexOptions.Compiled);
        }


        /// <summary>
        /// Looks for replaceable tokens, e.g. "{termid}", notes if found with the necessary
        /// bit of regex pattern text.
        /// </summary>
        /// <returns>The if replaceable token.</returns>
        /// <param name="segment">Segment.</param>
        /// <param name="greedyStar">Greedy star.</param>
        private string CheckIfReplaceableToken(string segment, out bool greedyStar) 
        {
            greedyStar = false;
            if (!segment.Contains('{')) {
                return null;  // This is the normal, non-tokenized case.
            }
            // This is all done at startup, so it's reasonable to take the computational time
            // to check for silliness.
            if (!segment.StartsWith("{") || !segment.EndsWith("}")) {
                throw new Exception(String.Format("Path segment {0} badly formed", segment));
            }

            segment = segment.TrimStart('{').TrimEnd('}');
            greedyStar = segment.EndsWith("*");
            var regexString = String.Format(greedyStar ?
                "(?<{0}>.+)" : "(?<{0}>[^/]+)", segment.TrimEnd('*'));
            return regexString;
        }
    }
}
