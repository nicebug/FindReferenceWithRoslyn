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
        private static void Main(string[] args)
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

            var st = new Stopwatch();
            st.Start();
            //var pathtosolution = @"xxx\UnityVS.UnityProj.sln";
            //var pathtosolution = @"..\..\..\FindReferenceWithRoslyn.sln";
            var projectname = @"FindRef";
            var fullClassName = @"CSharpParse.FindRef.Program";
            var methodName = @"FindAllReferencesWithSolution";
            //var methodName = @"FindAllReferences";


            // step1 通过VS工程获取函数行数

            Solution solution = GetSolution(pathtosolution);
            var documents = GetDocuments(solution);
            MethodAndLine line = new MethodAndLine();
            //line.GetMethodInfoFromSolution(pathtosolution);
            var methodPack = line.GetMethodInfoPackFromSolution(solution, documents);
            // 存在列表中有空的情况，清除null
            methodPack.RemoveAll(item => item == null);
            //if (File.Exists(Constant.MethodInfoTxt))
            //{
            //    File.Delete(Constant.MethodInfoTxt);
            //}
            //// 将函数的起始行写入methodinfo.txt
            //// eg: Awake,23,43,E:\xxxx\Client\UnityProj\Assets\Plugins\XXXXX.cs
            //File.AppendAllText(Constant.MethodInfoTxt, line.HandleMethodInfoFromFile("./tmp.txt"), Encoding.UTF8);

            // step2 变更行查函数名
            // eg: {"Awake": "Assets\Scripts\ClassicalPVP\cpClassicalPvpInfo.cs"...}
            //var functionlist = GetChangedFunctionName(Constant.SvnDiffTxt, Constant.MethodInfoTxt);

            var functionlist = GetChangedFunctionName(Constant.SvnDiffTxt, methodPack);

            // step3: 变更函数查相关引用
            foreach (var function in functionlist/*.Keys*/)
            {
                Console.WriteLine("变更的函数名：" + function.Key + " " + function.Value);
                Console.WriteLine(SearchMethodsForTextParallel(solution, documents, function, methodPack));
            }


            ///////////////////////////////////////////
            //FindAllReferences(pathtosolution, projectname, fullClassName, methodName);

            // 测试引用的函数
            /**
            string sb = SearchMethodsForTextParallel(pathtosolution, methodName);
            Console.WriteLine(sb);
            */

            st.Stop();
            Console.WriteLine("Total time:" + st.ElapsedMilliseconds);
        }

        private static Solution GetSolution(string pathtosolution)
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            Solution solution = workspace.OpenSolutionAsync(pathtosolution).Result;
            return solution;
        }

        private static List<Document> GetDocuments(Solution solution)
        {
            var documents = new List<Document>();
            if (solution == null)
            {
                return null;
            }

            foreach (var project in solution.Projects)
            {
                var language = project.Language;
                Parallel.ForEach(project.Documents, document =>
                {
                    if (language != "C#")
                    {
                        return;
                    }
                    documents.Add(document);
                });
            }
            return documents;;
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
                    if (line.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    // 找到对应的变更脚本
                    foreach (var num in change.Value)
                    {
                        var info = line.Split(',');
                        var start = int.Parse(info[1]) + 1;
                        var end = int.Parse(info[2]) + 1;
                        var filepath = info[3];
                        //var num1 = Int32.Parse(num);
                        var num1 = num;
                        if (start > num1 || num1 > end) continue;
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
            return functionlist;
        }

        private static Dictionary<string, string> GetChangedFunctionName(string svndifftxt, List<MethodInfoPack> methodsPack)
        {
            var referenceDict = new Dictionary<string, string>();
            var svnlist = HandleSvnDiffFile(svndifftxt);

            foreach (var svn in svnlist)
            {
                foreach (var method in methodsPack)
                {
                    if (method.FilePath.IndexOf(svn.Key, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    foreach (var num in svn.Value)
                    {
                        if (method.StartLine > num || num > method.EndLine) continue;
                        if (referenceDict.ContainsValue(method.MethodName) && referenceDict.ContainsValue(method.FilePath))
                        {
                            continue;
                        }
                        else
                        {
                            referenceDict[method.MethodName] = method.FilePath;
                        }
                    }
                }
            }

            return referenceDict;
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

            Project project = solution.Projects.FirstOrDefault(proj => proj.Name == projectname);
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


        private static void FindAllReferences(string pathtosolution, string fullClassName, string methodName)
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Solution solution = workspace.OpenSolutionAsync(pathtosolution).Result;
            if (solution == null)
            {
                Console.WriteLine("no solution: " + pathtosolution);
                return;
            }

            Project project = solution.Projects.Single();
            if (project == null)
            {
                Console.WriteLine("no project: " );
                return;
            }

            // 以下都是为了IMethodSymbol，便于使用SymbolFinder.FindReferencesAsync
            Compilation compilation = project.GetCompilationAsync().Result;
            INamedTypeSymbol classToAnalyze = compilation.GetTypeByMetadataName(fullClassName);
            if (classToAnalyze == null)
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

        public static string SearchMethodsForTextParallel(Solution solution, List<Document> documents, KeyValuePair<string, string> function, List<MethodInfoPack> methodPack)
        {
            StringBuilder result = new StringBuilder();
            //string language = "";
            //MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            //Solution solution = workspace.OpenSolutionAsync(path).Result;
            var symbollist = new List<ISymbol>();

            foreach (var document in documents)
            {
                symbollist.AddRange(SearchMethodSymbolFromDocument(document, function));
            }

            //foreach (var project in solution.Projects)
            //{
            //    language = project.Language;
            //    Parallel.ForEach(project.Documents, document =>
            //    {
            //        if (language == "C#")
            //        {
            //            //result.Append(SearchMethodsForTextCSharp(document, textToSearch));
            //            //symbollist.AddRange(SearchMethodSymbolForTextCSharp(document, textToSearch));
            //            symbollist.AddRange(SearchMethodSymbolFromDocument(document, function));
            //        }
            //    });
            //}

            foreach (var symbol in symbollist)
            {
                var results = SymbolFinder.FindReferencesAsync(symbol, solution).Result;
                foreach (var _result in results)
                {
                    Console.WriteLine(_result.Definition);
                    Console.WriteLine("------------相关引用--------------");
                    //Console.WriteLine( ((IMethodSymbol)_result).Name);
                    foreach (var num in _result.Locations)
                    {
                        var start = num.Location.GetMappedLineSpan().StartLinePosition.Line + 1;
                        var end = num.Location.GetMappedLineSpan().EndLinePosition.Line + 1;
                        string _path = num.Document.FilePath;
                        foreach (var method in methodPack)
                        {
                            if (!_path.Equals(method.FilePath)) continue;
                            if (start >= method.StartLine && end <= method.EndLine)
                            {
                                Console.WriteLine(method.MethodName);
                            }
                        }
                        Console.WriteLine(/*num.Location.SourceTree.GetLineSpan(num.Location.SourceSpan) + */ num.Location.GetLineSpan().ToString());
                    }
                }
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

            if (syntaxNodes == null || !syntaxNodes.Any()) return sb.ToString();
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
            return sb.ToString();
        }

        private static List<ISymbol> SearchMethodSymbolForTextCSharp(Document document, string textToSearch)
        {
            List<ISymbol> _symbollist = new List<ISymbol>();
            SyntaxTree syntax = document.GetSyntaxTreeAsync().Result;

          

            var root = (CompilationUnitSyntax)syntax.GetRoot();
            var syntaxNodes = from methodDeclaration in root.DescendantNodes()
                              .Where(x => x is MethodDeclarationSyntax /*|| x is PropertyDeclarationSyntax*/)
                              select methodDeclaration;

            if (syntaxNodes == null || !syntaxNodes.Any()) return _symbollist;
            foreach (MemberDeclarationSyntax method in syntaxNodes)
            {
                if (!(method is MethodDeclarationSyntax)) continue;
                var _methodname = ((MethodDeclarationSyntax)method).Identifier.ValueText;
                if (_methodname.ToUpper().Contains(textToSearch.ToUpper()))
                {
                    var methodSymbol = document.GetSemanticModelAsync().Result.GetDeclaredSymbol(method);
                    _symbollist.Add(methodSymbol);
                }
            }
            return _symbollist;
        }

        private static List<IMethodSymbol> SearchMethodSymbolFromDocument(Document document, KeyValuePair<string, string> function)
        {
            List<IMethodSymbol> _symbollist = new List<IMethodSymbol>();
            // 函数名与文件名均相同才添加
            var methodDeclarationList = document.GetSyntaxRootAsync().Result.DescendantNodes().OfType<MethodDeclarationSyntax>()
              .Where(x => (x.Identifier.ValueText == function.Key && x.GetLocation().SourceTree.FilePath.Contains(function.Value)));
            if (/*methodDeclarationList != null*/ methodDeclarationList.Any())
            {
                //var method = methodDeclarationList.GetEnumerator();
                //while (method.MoveNext())
                //{
                //    var _name = method.Current;
                //    var _methodSymbol = document.GetSemanticModelAsync().Result.GetDeclaredSymbol(_name);
                //    _symbollist.Add(_methodSymbol);
                //}
                _symbollist.AddRange(methodDeclarationList.Select(method => document.GetSemanticModelAsync().Result.GetDeclaredSymbol(method)));
            }

            return _symbollist;
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
                        key = line.Split()[1].Trim().Replace(@"/", @"\");
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


    static class SyntaxNodeHelper
    {
        public static bool TryGetParentSyntax<T>(SyntaxNode syntaxNode, out T result) where T :SyntaxNode
        {
            result = null;
            if (syntaxNode == null)
            {
                return false;
            }
            try
            {
                syntaxNode = syntaxNode.Parent;
                if (syntaxNode == null)
                {
                    return false;
                }

                if (syntaxNode.GetType() == typeof(T))
                {
                    result = syntaxNode as T;
                    return true;
                }
                return TryGetParentSyntax<T>(syntaxNode, out result);
            }
            catch
            {
                return false;
            }
        }
    }
}
