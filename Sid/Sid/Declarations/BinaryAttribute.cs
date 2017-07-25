using System;
using System.IO;

namespace Sid.Declarations
{
    public class BinaryRequest
    {
        private long length;
        private BinaryReader reader;
        public long Length { get { return length; } }
        public BinaryReader Reader { get { return reader; } }

        public BinaryRequest(long length, BinaryReader reader)
        {
            this.length = length;
            this.reader = reader;
        }
    }

    public class BinaryAttribute : BaseParamAttribute
    {
        public BinaryAttribute()
        {
        }

        public override object GetParamValue(Sid.WebServices.IRequest request, Sid.WebServices.SidModule moduleInstance)
        {
            return new BinaryRequest(request.ContentLength,
                new BinaryReader(request.InputStream));
        }
    }
}

