using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixDocumentTool
{
    public class Group
    {
        public string Name { get; set; }

        public string ParentName { get; set; }

        public List<Field> Fields { get; set; }

        public List<Group> Groups { get; set; }

        public List<Component> Components { get; set; }
    }
}
