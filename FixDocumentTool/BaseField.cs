using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixDocumentTool
{
    public class BaseField
    {
        public string Number { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public List<Enumeration> Enumerations { get; set; }
    }
}
