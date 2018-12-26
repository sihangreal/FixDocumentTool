using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixDocumentTool
{
    public class Component
    {
        public string Name { get; set; }

        public List<Field> Fields { get; set; }

        public List<Group> Groups { get; set; }
    }
}
