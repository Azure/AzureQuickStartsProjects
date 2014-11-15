using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace DataDocumentDB.Models
{
    class Family : Resource
    {
        public string FamilyName { get; set; }
        public Parent[] Parents { get; set; }
        public Child[] Children { get; set; }
        public Pet[] Pets { get; set; }
    }
}
