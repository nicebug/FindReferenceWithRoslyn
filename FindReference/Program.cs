using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

using FindReference.Common;

namespace FindReference
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            var pathtosolution = args[0];
            //var projectname = args[1];
            //var fullClassName = args[2];
            //var methodName = args[3];

            //Stopwatch st = new Stopwatch();
            //st.Start();
            /**
            var pathtosolution = @"xxx\UnityVS.UnityProj.sln";
            //var pathtosolution = @"..\..\..\FindReferenceWithRoslyn.sln";
            var projectname = @"FindRef";
            var fullClassName = @"CSharpParse.FindRef.Program";
            //var methodName = @"FindAllReferencesWithSolution";
            var methodName = @"FindAllReferences";
            */

            // step1 通过VS工程获取函数行数
            MethodAndLine line = new MethodAndLine();
            line.GetMethodInfoFromSolution(pathtosolution);
            if (File.Exists(Constant.MethodInfoTxt))
            {
                File.Delete(Constant.MethodInfoTxt);
            }
            File.AppendAllText(Constant.MethodInfoTxt, line.HandleMethodInfoFromFile("./tmp.txt"), Encoding.UTF8);

            // step2 变更行查函数名
            var functionlist = GetChangedFunctionName(Constant.SvnDiffTxt, Constant.MethodInfoTxt);

            // step3: 变更函数查相关引用
            foreach (var function in functionlist.Keys)
            {
                Console.WriteLine("变更的函数名：" + function/*.Key + " " + function.Value*/);
                Console.WriteLine(SearchMethodsForTextParallel(pathtosolution, function));
            }
            
            
            ///////////////////////////////////////////
            //FindAllReferences(pathtosolution, projectname, fullClassName, methodName);

            // 测试引用的函数
            /**
            string sb = SearchMethodsForTextParallel(pathtosolution, methodName);
            Console.WriteLine(sb);
            */

            //st.Stop();
            //Console.WriteLine("Total time:" + st.ElapsedMilliseconds);
        }


        private static Dictionary<string, string> GetChangedFunctionName(string svndifftxt, string methodinfotxt)
        {
            var lines = File.ReadAllLines(methodinfotxt);
            var functionlist = new Dictionary<string, string>();

            var changelist = HandleSvnDiffFile(svndifftxt);
            foreach (var change in changelist)
            {
                foreach (var line in lines)
                {
                    var key = change.Key.Replace(@"/", @"\");
                    if (line.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // 找到对应的变更脚本
                        foreach (var num in change.Value)
                        {
                            var info = line.Split(',');
                            var start = Int32.Parse(info[1]) + 1;
                            var end = Int32.Parse(info[2]) + 1;
                            var filepath = info[3];
                            //var num1 = Int32.Parse(num);
                            var num1 = num;
                            if (start <= num1 && num1 <= end)
                            {
                                //Console.WriteLine(line);
                                if (functionlist.ContainsValue(info[0]) && functionlist.ContainsValue(key))
                                {
                                    continue;
                                }
                                else
                                {
                                    functionlist[info[0]] = key;
                                }
                            }
                        }
                    }
                }
            }
            return functionlist;
        }

        #region 查找函数引用
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
        #endregion

        private static void PrintUsage()
        {
            Console.WriteLine(@"Usage: FindReference " +  @"pathtosolution");
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
            //sb.AppendLine(methodText);

            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename">svn diff file</param>
        /// <returns>{"Assets/Scripts/ssGameWorld.cs", [1, 20...]}</returns>
        private static Dictionary<string, List<int>> HandleSvnDiffFile(string filename)
        {
            Dictionary<string, List<int>> result = new Dictionary<string, List<int>>();
            var lines = File.ReadAllLines(filename);
            string key = null;
            foreach (var line in lines)
            {
                // +++ Assets/Scripts/ssGameWorld.cs (revision 190380)这种文件格式的处理
                if (!line.StartsWith(@"+++") && !line.StartsWith(@"@@"))
                {
                    continue;
                }
                else
                {
                    if (line.StartsWith(@"+++"))
                    {
                        key = line.Split()[1].Trim();
                        if (!result.ContainsKey(key))
                        {
                            result[key] = new List<int>();
                        }
                    }
                    //# @@ -1034,7 +1030,6 @@ 这种格式的处理
                    else if (line.StartsWith(@"@@"))
                    {
                        var linenum = line.Split()[2].Split(',')[0].Split('+')[1].Trim();
                        result[key].Add(int.Parse(linenum));
                    }
                }
            }
            return result;
        }
    }
}
