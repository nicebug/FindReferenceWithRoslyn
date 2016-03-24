using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindReference.Common
{
    public class Constant
    {
        public static readonly string MethodInfoTxt = @"methodinfo.txt";
        public static readonly string SvnDiffTxt = @"svndiff.txt";
        // 正则匹配括号内的内容
        public static readonly string RegexStr = @"(?<=[(（])[^（）()]*(?=[)）])";
    }
}
