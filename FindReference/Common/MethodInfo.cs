using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindReference.Common
{
    public class MethodInfoPack
    {
        //Equals,23,28,E:xxx\BinaryDeserialization.cs
        public string MethodName { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string FilePath { get; set; }
        public MethodInfoPack(string MethodName, int StartLine, int EndLine, string FilePath)
        {
            this.MethodName = MethodName;
            this.StartLine = StartLine;
            this.EndLine = EndLine;
            this.FilePath = FilePath;
        }
        public MethodInfoPack()
        {

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
