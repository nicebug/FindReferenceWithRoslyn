using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;


namespace FindReference
{
    class Program
    {
        static void Main(string[] args)
        {
            //if (args.Length == 0)
            //{
            //    PrintUsage();
            //    return;
            //}

            //var pathtosolution = args[0];
            //var projectname = args[1];
            //var fullClassName = args[2];
            //var methodName = args[3];

            //var pathtosolution = @"E:\DevWork\Project\CSharpParser\CSharpParserWithRoslyn\CSharpParserWithRoslyn.sln";
            var pathtosolution = @"..\..\..\FindReferenceWithRoslyn.sln";
            var projectname = @"FindRef";
            var fullClassName = @"CSharpParse.FindRef.Program";
            //var methodName = @"FindAllReferencesWithSolution";
            var methodName = @"FindAllReferences";

            // 测试获取函数行数
            MethodAndLine line = new MethodAndLine();
            line.GetMethodInfoFromSolution(pathtosolution);
            //Console.WriteLine(sb);

            //FindAllReferences(pathtosolution, projectname, fullClassName, methodName);

            // 测试引用的函数
            /**
            string sb = SearchMethodsForTextParallel(pathtosolution, methodName);
            Console.WriteLine(sb);
            */
        }

        private static void FindAllReferences(string pathtosolution, string projectname, string fullClassName, string methodName)
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Solution solution = workspace.OpenSolutionAsync(pathtosolution).Result;
            if (solution == null)
            {
                Console.WriteLine("no solution: " + pathtosolution);
                return;
            }

            Project project = solution.Projects.Where(proj => proj.Name == projectname).FirstOrDefault();
            if (project == null)
            {
                Console.WriteLine("no project: " + projectname);
                return;
            }

            // 以下都是为了IMethodSymbol，便于使用SymbolFinder.FindReferencesAsync
            Compilation compilation = project.GetCompilationAsync().Result;
            INamedTypeSymbol classToAnalyze = compilation.GetTypeByMetadataName(fullClassName);
            if ( classToAnalyze == null)
            {
                Console.WriteLine("no fullclass name : " + fullClassName);
                return;
            }
            IMethodSymbol methodSymbol = classToAnalyze.GetMembers(methodName).FirstOrDefault() as IMethodSymbol;
            if (methodSymbol == null)
            {
                return;
            }
            var results = SymbolFinder.FindReferencesAsync(methodSymbol, solution).Result.ToList();
            
            foreach (var result in results)
            {
                Console.WriteLine(result.Definition);
                foreach (var num in result.Locations)
                {
                    Console.WriteLine(/*num.Location.SourceTree.GetLineSpan(num.Location.SourceSpan) + */ num.Location.GetLineSpan().ToString());
                }
                
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"Usage: FindReference " +  @"pathtosolution " + @"projectname " + @"FullClassName " + @"MethodName");
        }

        public static string SearchMethodsForTextParallel(string path, string textToSearch)
        {
            StringBuilder result = new StringBuilder();
            string language = "";
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Solution solution = workspace.OpenSolutionAsync(path).Result;
            foreach (Project project in solution.Projects)
            {
                language = project.Language;
                Parallel.ForEach(project.Documents, document =>
                {
                    if (language == "C#")
                    {
                        result.Append(SearchMethodsForTextCSharp(document, textToSearch));
                    }
                });
            }
            return result.ToString();
        }

        private static string SearchMethodsForTextCSharp(Document document, string textToSearch)
        {
            StringBuilder sb = new StringBuilder();
            SyntaxTree syntax = document.GetSyntaxTreeAsync().Result;
            var root = (CompilationUnitSyntax)syntax.GetRoot();
            var syntaxNodes = from methodDeclaration in root.DescendantNodes()
                              .Where(x => x is MethodDeclarationSyntax || x is PropertyDeclarationSyntax)
                              select methodDeclaration;
            if (syntaxNodes != null && syntaxNodes.Count() > 0)
            {
                foreach (MemberDeclarationSyntax method in syntaxNodes)
                {
                    if (method != null)
                    {
                        string methodText = method.GetText().ToString();
                        if (methodText.ToUpper().Contains(textToSearch.ToUpper()))
                        {
                            sb.Append(GetMehotdOrPropertyTextCSharp(method, document));
                        }
                    }
                }
            }
            return sb.ToString();
        }

        private static string GetMehotdOrPropertyTextCSharp(SyntaxNode node, Document document)
        {
            StringBuilder sb = new StringBuilder();
            string methodText = node.GetText().ToString();
            string num = node.GetLocation().GetLineSpan().Span.ToString();
            bool isMethod = node is MethodDeclarationSyntax;
            string methodOrPropertyDefinition = isMethod ? "Method: " : "Property: ";
            object methodName = isMethod ? ((MethodDeclarationSyntax)node).Identifier.Value :
                ((PropertyDeclarationSyntax)node).Identifier.Value;
            sb.AppendLine("//=================================" + num);
            sb.AppendLine(document.FilePath);
            sb.AppendLine(methodOrPropertyDefinition + (string)methodName);
            sb.AppendLine(methodText);

            return sb.ToString();
        }
    }
}
