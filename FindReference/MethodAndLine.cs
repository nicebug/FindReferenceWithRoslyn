using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

using FindReference.Common;

namespace FindReference
{
    public class MethodAndLine
    {
        //public MethodAndLine(string pathToSolution)
        //{
        //    string path = pathToSolution;
        //}

        public void  GetMethodInfoFromSolution(string pathToSoluton)
        {
            string folder = "./tmp.txt";
            //StringBuilder result = new StringBuilder();
            List<string> result = new List<string>();
            List<object> info = new List<object>();

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            
            Solution solution = workspace.OpenSolutionAsync(pathToSoluton).Result;
            if (solution == null)
            {
                return ;
            }
            foreach (var project in solution.Projects)
            {
                var language = project.Language;
                Parallel.ForEach(project.Documents, document =>
                {
                    if (language == "C#")
                    {
                        //var infolist = GetMethodInfoListFromDocument(document);
                        var infolist = GetMethodInfoFromDocument(document);
                        if (!string.IsNullOrEmpty(infolist)/*infolist.Count > 0*/)
                        {
                            //info.Add(infolist);
                            result.Add(infolist);
                        }
                    }
                });
            }
            //return result.ToString();
            if (File.Exists(folder))
            {
                File.Delete(folder);
            }
            //result.Add(info.ToJSON());
            File.AppendAllLines(folder, result, Encoding.UTF8);
        }

        public List<MethodInfoPack> GetMethodInfoPackFromSolution(Solution solution, List<Document> documents )
        {
            List<MethodInfoPack> methodPack = new List<MethodInfoPack>();
            var tmpPack = new List<MethodInfoPack>();
            //MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            //Solution solution = workspace.OpenSolutionAsync(pathToSoluton).Result;
            if (solution == null)
            {
                return null;
            }
            //foreach (var project in solution.Projects)
            //{
            //    var language = project.Language;
            //    Parallel.ForEach(project.Documents, document =>
            //    {
            //        if (language != "C#") return;
            //        tmpPack = GetMethodInfoPackFromDocument(document);
            //        if (tmpPack != null && tmpPack.Count > 0)
            //        {
            //            methodPack.AddRange(tmpPack);
            //        }
            //    });
            //}
            foreach (var document in documents)
            {
                tmpPack = GetMethodInfoPackFromDocument(document);
                if (tmpPack.Any())
                {
                    methodPack.AddRange(tmpPack);
                }
            }
            return methodPack;
        }

        private static string GetMethodInfoFromDocument(Document document)
        {
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine(document.FilePath.ToString().Trim());
            //sb.AppendLine(document.FilePath);
            List<MethodInfo> info = new List<MethodInfo>();
            var root = (CompilationUnitSyntax)document.GetSyntaxRootAsync().Result;
            var syntaxNodes = from methodDeclaration in root.DescendantNodes()
                              .Where(x => x is MethodDeclarationSyntax)
                              select methodDeclaration;
            if (!syntaxNodes.Any()) return Regex.Replace(sb.ToString(), @" ", "");
            foreach (MethodDeclarationSyntax method in syntaxNodes)
            {
                //info.Add(new MethodInfo
                //{
                //    DocName = document.Name,
                //    FilePath = document.FilePath,
                //    //MethodNameAndList = new Method(method.Identifier.ToString(), method.GetLocation().GetLineSpan().Span.ToString())
                //    MethodName = method.Identifier.ToString(),
                //    LineNum = method.GetLocation().GetLineSpan().Span.ToString()
                //});
                //sb.AppendLine("//==========================");
                sb.AppendLine(method.Identifier.Value.ToString().Trim() + "," + method.GetLocation().GetLineSpan().Span.ToString().Trim() + "," + document.FilePath);
                //sb.AppendLine(method.GetLocation().GetLineSpan().Span.ToString());
            }
            //string result = info.ToJSON();

            return Regex.Replace(sb.ToString(), @" ", "");
            //return result;
        }

        //document中函数所有信息（函数名，开始行，结束行，完整文件路径）存入list中
        private static List<MethodInfoPack> GetMethodInfoPackFromDocument(Document document)
        {
            List<MethodInfoPack> methods = new List<MethodInfoPack>();

            var root = (CompilationUnitSyntax)document.GetSyntaxRootAsync().Result;
            var syntaxNodes = from methodDeclaration in root.DescendantNodes()
                              .Where(x => x is MethodDeclarationSyntax)
                              select methodDeclaration;
            if (!syntaxNodes.Any()) return methods;
            methods.AddRange(
                from MethodDeclarationSyntax method in syntaxNodes
                let startline = method.GetLocation().GetMappedLineSpan().StartLinePosition.Line + 1
                let endline = method.GetLocation().GetMappedLineSpan().EndLinePosition.Line + 1
                select new MethodInfoPack(method.Identifier.ToString(), startline, endline, document.FilePath));
            return methods;
        }

        private List<MethodInfo> GetMethodInfoListFromDocument(Document document)
        {
            List<MethodInfo> info = new List<MethodInfo>();
            var root = (CompilationUnitSyntax)document.GetSyntaxRootAsync().Result;
            var syntaxNodes = from methodDeclaration in root.DescendantNodes()
                              .Where(x => x is MethodDeclarationSyntax)
                              select methodDeclaration;
            var enumerable = syntaxNodes as IList<SyntaxNode> ?? syntaxNodes.ToList();
            if (/*syntaxNodes != null && */enumerable.Any())
            {
                info.AddRange(from MethodDeclarationSyntax method in enumerable
                    select new MethodInfo
                    {
                        DocName = document.Name, FilePath = document.FilePath,

                        //MethodNameAndList = new MethodS(method.Identifier.ToString(), method.GetLocation().GetLineSpan().Span.ToString())
                        MethodName = method.Identifier.ToString(), LineNum = method.GetLocation().GetLineSpan().Span.ToString()
                    });
            }
            return info;
        }

        #region 解析文件生成函数开始行，结束行信息
        // Awake,(23,1)-(43,2),E:\DailyWork\WeSpeed\Code\PreDistribution\Client\UnityProj\Assets\Plugins\BuglyInit.cs
        // ||
        // \/
        // Awake,23,43,E:\DailyWork\WeSpeed\Code\PreDistribution\Client\UnityProj\Assets\Plugins\BuglyInit.cs
        // 如上的处理，只需要函数， 开始行，结束行，文件路径
        public string  HandleMethodInfoFromFile(string filepath)
        {
            if (!File.Exists(filepath))
            {
                return @"tmp.txt not found";
            }

            
            StringBuilder sb = new StringBuilder();
            var lines = File.ReadAllLines(filepath);
            foreach (var line in lines)
            {
                var split = new char[] { '(', ')', '-', ',' };
                var result = line.Split(split);

                if (result.Length >= 10)
                {
                    //var split = new char[]{ '(', ')', '-', ','};
                    //var numinfo = result[1].Split(split);
                    sb.AppendLine(result[0] + "," + result[2] + "," + result[6] + "," + result[9]);
                }
                else
                {
                    sb.AppendLine(result[0]);
                }
            }
            return sb.ToString();
        }
        #endregion
    }
}
