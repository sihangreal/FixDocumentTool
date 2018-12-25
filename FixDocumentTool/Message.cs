using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixDocumentTool
{
    public class Message
    {
        public string Name { get; set; }

        public string MsgType { get; set; }

        public string MsgCat { get; set; }

        public List<Field> Fields { get; set; }

        public List<Group> Groups { get; set; }
    }
}
