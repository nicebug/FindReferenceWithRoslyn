using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            string folder = "./method.txt";
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
                        var infolist = GetMethodInfoListFromDocument(document);
                        if (infolist != null && infolist.Count > 0)
                        {
                            info.Add(infolist);
                        }
                        //result.Add(GetMethodInfoFromDocument(document));
                    }
                });
            }
            //return result.ToString();
            if (File.Exists(folder))
            {
                File.Delete(folder);
            }
            result.Add(info.ToJSON());
            File.AppendAllLines(folder, result, Encoding.UTF8);
        }

        private string GetMethodInfoFromDocument(Document document)
        {
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine(document.Name);
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
                    info.Add(new MethodInfo
                    {
                        DocName = document.Name,
                        FilePath = document.FilePath,
                        //MethodNameAndList = new Method(method.Identifier.ToString(), method.GetLocation().GetLineSpan().Span.ToString())
                        MethodName = method.Identifier.ToString(),
                        LineNum = method.GetLocation().GetLineSpan().Span.ToString()
                    });
                    //sb.AppendLine("//==========================");
                    //sb.AppendLine(method.Identifier.Value.ToString() + "," + method.GetLocation().GetLineSpan().Span.ToString());
                    //sb.AppendLine(method.GetLocation().GetLineSpan().Span.ToString());
                    //sb.AppendLine(method.GetLocation().GetLineSpan().Span.ToString());
                }
            }
            string result = info.ToJSON();
            //return sb.ToString();
            return result;
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
    }
}
