using System;
using System.Collections.Generic;
using System.Text;

namespace TagSearcher.VK.Account
{
    public class VKAccount
    {
        public string NameSurname { get; set; }
        public string LastActivity { get; set; }
        public string Status { get; set; }
        public string Info { get; set; }
        public List<InfoItem> InfoItems { get; set; }
    }
}
