using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixDocumentTool
{
    public class Field
    {
        public string Name { get; set; }

        public string Required { get; set; }

        public bool IsComponent { get; set; }

        public bool IsGroup { get; set; }
    }
}
