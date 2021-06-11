using System;

namespace RESTy.Declarations
{
    /// <summary>
    /// Sets the root path for a given BaseModule.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RootPathAttribute : DocumentedAttribute
    {
        public readonly string Path;

        public RootPathAttribute(string path)
        {
            this.Path = path;
        }
    }
}

