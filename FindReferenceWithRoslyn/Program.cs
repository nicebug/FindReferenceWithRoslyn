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


namespace FindReferenceWithRoslyn
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
            var pathtosolution = @"E:\DevWork\Project\CSharpParser\CSharpParserWithRoslyn\CSharpParserWithRoslyn.sln";
            var projectname = @"FindRef";
            var fullClassName = @"FindRef.Program";
            var methodName = @"FindAllReferencesWithSolution";

            FindAllReferences(pathtosolution, projectname, fullClassName, methodName);
        }

        private static void FindAllReferences(string pathtosolution, string projectname, string fullClassName, string methodName)
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Solution solution = workspace.OpenSolutionAsync(pathtosolution).Result;
            Project project = solution.Projects.Where(proj => proj.Name == projectname).FirstOrDefault();

            // 以下都是为了IMethodSymbol，便于使用SymbolFinder.FindReferencesAsync
            Compilation compilation = project.GetCompilationAsync().Result;
            INamedTypeSymbol classToAnalyze = compilation.GetTypeByMetadataName(fullClassName);
            IMethodSymbol methodSymbol = classToAnalyze.GetMembers(methodName).FirstOrDefault() as IMethodSymbol;
            if (methodSymbol == null)
            {
                return;
            }
            var results = SymbolFinder.FindReferencesAsync(methodSymbol, solution).Result.ToList();
            foreach (var result in results)
            {
                foreach (var num in result.Locations)
                {
                    Console.WriteLine(/*num.Location.SourceTree.GetLineSpan(num.Location.SourceSpan) + */ num.Location.GetLineSpan().ToString());
                }
                
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"Usage: FindReferenceWithRoslyn " +  @"pathtosolution " + @"projectname " + @"FullClassName " + @"MethodName");
        }
    }
}
