using System;

namespace RESTy.Declarations
{
    public class DocumentedAttribute : Attribute
    {
        public string Description { get; set; }

        public bool Hidden { get; set; }
    }
}

