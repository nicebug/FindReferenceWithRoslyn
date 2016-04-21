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

            string language = "";
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            
            Solution solution = workspace.OpenSolutionAsync(pathToSoluton).Result;
            if (solution == null)
            {
                return ;
            }
            foreach (Project project in solution.Projects)
            {
                language = project.Language;
                Parallel.ForEach(project.Documents, document =>
                {
                    if (language == "C#")
                    {
                        //var infolist = GetMethodInfoListFromDocument(document);
                        var infolist = GetMethodInfoFromDocument(document);
                        if (infolist != null && infolist != ""/*infolist.Count > 0*/)
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

        public List<MethodInfoPack> GetMethodInfoPackFromSolution(string pathToSoluton)
        {
            List<MethodInfoPack> methodPack = new List<MethodInfoPack>();
            var tmpPack = new List<MethodInfoPack>();
            string language = "";
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            Solution solution = workspace.OpenSolutionAsync(pathToSoluton).Result;
            if (solution == null)
            {
                return null;
            }
            foreach (Project project in solution.Projects)
            {
                language = project.Language;
                Parallel.ForEach(project.Documents, document =>
                {
                    if (language == "C#")
                    {
                        tmpPack = GetMethodInfoPackFromDocument(document);
                        if (tmpPack != null && tmpPack.Count > 0)
                        {
                            methodPack.AddRange(tmpPack);
                        }
                    }
                });
            }
            return methodPack;
        }

        private string GetMethodInfoFromDocument(Document document)
        {
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine(document.FilePath.ToString().Trim());
            //sb.AppendLine(document.FilePath);
            List<MethodInfo> info = new List<MethodInfo>();
            var root = (CompilationUnitSyntax)document.GetSyntaxRootAsync().Result;
            var syntaxNodes = from methodDeclaration in root.DescendantNodes()
                              .Where(x => x is MethodDeclarationSyntax)
                              select methodDeclaration;
            if (syntaxNodes != null && syntaxNodes.Count() > 0)
            {
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
            }
            //string result = info.ToJSON();

            return Regex.Replace(sb.ToString(), @" ", "");
            //return result;
        }

        //document中函数所有信息（函数名，开始行，结束行，完整文件路径）存入list中
        private List<MethodInfoPack> GetMethodInfoPackFromDocument(Document document)
        {
            List<MethodInfoPack> methods = new List<MethodInfoPack>();
            int startline;
            int endline;
            var tmpPack = new MethodInfoPack();

            var root = (CompilationUnitSyntax)document.GetSyntaxRootAsync().Result;
            var syntaxNodes = from methodDeclaration in root.DescendantNodes()
                              .Where(x => x is MethodDeclarationSyntax)
                              select methodDeclaration;
            if (syntaxNodes != null && syntaxNodes.Count() > 0)
            {
                foreach (MethodDeclarationSyntax method in syntaxNodes)
                {
                    startline = method.GetLocation().GetMappedLineSpan().StartLinePosition.Line + 1;
                    endline = method.GetLocation().GetMappedLineSpan().EndLinePosition.Line + 1;
                    //tmpPack = new MethodInfoPack
                    //{
                    //    MethodName = method.Identifier.ToString(),
                    //    StartLine = startline,
                    //    EndLine = endline,
                    //    FilePath = document.FilePath
                    //};
                    tmpPack = new MethodInfoPack(method.Identifier.ToString(), startline, endline, document.FilePath);
                    if (tmpPack != null)
                    {
                        methods.Add(tmpPack);
                    }
                }
            }
            return methods;
        }

        private List<MethodInfo> GetMethodInfoListFromDocument(Document document)
        {
            List<MethodInfo> info = new List<MethodInfo>();
            var root = (CompilationUnitSyntax)document.GetSyntaxRootAsync().Result;
            var syntaxNodes = from methodDeclaration in root.DescendantNodes()
                              .Where(x => x is MethodDeclarationSyntax)
                              select methodDeclaration;
            if (syntaxNodes != null && syntaxNodes.Count() > 0)
            {
                foreach (MethodDeclarationSyntax method in syntaxNodes)
                {
                    info.Add(new MethodInfo
                    {
                        DocName = document.Name,
                        FilePath = document.FilePath,

                        //MethodNameAndList = new MethodS(method.Identifier.ToString(), method.GetLocation().GetLineSpan().Span.ToString())
                        MethodName = method.Identifier.ToString(),
                        LineNum = method.GetLocation().GetLineSpan().Span.ToString()
                    });
                }
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
