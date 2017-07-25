using System;

namespace Sid.Declarations
{
    public class DocumentedAttribute : Attribute
    {
        public string Description { get; set; }

        public bool Hidden { get; set; }
    }
}

