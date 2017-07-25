using System;
using System.Text.RegularExpressions;

namespace DataMigration
{
    /// <summary>
    /// S3 parts - this class knows how to read S3 Urls, and break them into constituent Host, 
    /// Bucket, and File parts, which are exposed as properties.
    /// </summary>
    internal class S3Parts
    {
        // Try to match new-style URL, where bucket is part of the host name.
        private static readonly Regex s3NewStyleRgx = new Regex(
       "s3://(?<bucket>[^/]*)?\\.(?<host>s3.*\\.amazonaws\\.com)/(?<file>.*)", 
            RegexOptions.IgnoreCase);
            
        public string Bucket { get; set; }

        public string Host { get; set; }

        public string File { get; set; }

        /// <summary>
        /// Given an S3 Url, this static method will return an S3Parts instance.
        /// </summary>
        /// <returns>And S3Parts instance.</returns>
        /// <param name="s3Url">S3 URL.</param>
        static public S3Parts FromUrl(string s3Url)
        {
            // Try new-style regex only.
            var parts = PartsFromRegex(s3Url, s3NewStyleRgx);
            if (parts == null ||
                String.IsNullOrEmpty(parts.Bucket) ||
                String.IsNullOrEmpty(parts.Host)) {
                throw new ArgumentException (
                    String.Format ("S3Parts.FromUrl - cannot parse s3 url {0}", 
                        s3Url));
            }
    
            return parts;
        }

        /// <summary>
        /// Given a regex, will parse out the bucket, host, and file.
        /// </summary>
        /// <returns>An S3Parts object, with the decomposed S3 Uri</returns>
        /// <param name="s3Url">S3 URL.</param>
        /// <param name="rgx">Regular Expression to use in parsing the uri.</param>
        static S3Parts PartsFromRegex(string s3Url, Regex rgx)
        {
            var m = rgx.Match(s3Url);
            if (m.Success) {
                string bucket = String.Empty;
                string host = String.Empty;

                var bucketMatch = m.Groups["bucket"].Success;
                if (bucketMatch) {
                    bucket = m.Groups["bucket"].Value;
                }
                var hostMatch = m.Groups["host"].Success;
                if (hostMatch) {
                    host = m.Groups["host"].Value;
                }

                var file = m.Groups["file"].Value;

                return new S3Parts{ Bucket = bucket, Host = host, File = file };
            }
            return null;
        }
    }
}
