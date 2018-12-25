using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixDocumentTool
{
    public class FieldType
    {
        private string extendType;
        public string ExtendType
        {
            get { return extendType; }
            set { extendType = value; }
        }
        private string baseType;
        public string BaseType
        {
            get { return baseType; }
            set { baseType = value; }
        }
        public FieldType(string extendType, string baseType)
        {
            this.extendType = extendType;
            this.baseType = baseType;
        }
    }
}
