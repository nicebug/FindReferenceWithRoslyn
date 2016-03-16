using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindReference.Common
{
    public struct MethodS
    {
        public string MethodName;
        public string MethodLine;
        public MethodS(string MethodName, string MethodLine)
        {
            this.MethodName = MethodName;
            this.MethodLine = MethodLine;
        }
    }
    class MethodInfo
    {
        public string MethodName { get; set; }
        public string LineNum { get; set; }
        public string DocName { get; set; }
        public string FilePath { get; set; }
        //public MethodS MethodNameAndList { get; set; }
    }
}
