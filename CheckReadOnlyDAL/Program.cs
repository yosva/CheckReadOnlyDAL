using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.FindSymbols;

namespace CheckReadOnlyDAL
{
    class Program
    {
        static int Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Please, enter the solution's path");
                Console.WriteLine("Usage: CheckReadOnlyDAL <SolutionPath>");
                return 1;
            }

            Console.WriteLine("Retrieving list of projects to scan ...");

            TargetFilesFetcher getTargetFiles = new TargetFilesFetcher();

            Dictionary<string, List<string>> projToFilesDict = getTargetFiles.getProjToSrcFilesDict(args[0]);

            Console.WriteLine("{0} projects found", projToFilesDict.Count);

            var myEnum = projToFilesDict.Keys.GetEnumerator();

            while (myEnum.MoveNext())
            {
                string key = myEnum.Current;

                Console.WriteLine("Scanning project <{0}> ...", key);

                List<string> srcFnList = projToFilesDict[key];

                var codeAnaliser = new CodeAnalyser(key, srcFnList);

                CheckReadOnlyDALResultMessage message = new CheckReadOnlyDALResultMessage();

                codeAnaliser.Analyze(message);

                message.print();
            }

            return 0;

            #region old code
            /*try
            {
                //string solutionPath = @"D:\Main\Service\Services\Services.sln";
                var workspace = MSBuildWorkspace.Create();

                //var solution = workspace.OpenSolutionAsync(solutionPath).Result;
                var project = workspace.OpenProjectAsync(@"D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock/Cdiscount.Business.Stock.csproj").Result;

                //var project = solution.Projects.Where(p => p.Name == "Services").First();
                var compilation = project.GetCompilationAsync().Result;
                var programClass = compilation.GetTypeByMetadataName("RoslynTest.Program");

                var barMethod = programClass.GetMembers("Bar").First();
                var fooMethod = programClass.GetMembers("Foo").First();

                //var barResult = SymbolFinder.FindReferencesAsync(barMethod, project).Result.ToList();
                //var fooResult = SymbolFinder.FindReferencesAsync(fooMethod, solution).Result.ToList();

                //Debug.Assert(barResult.First().Locations.Count() == 1);
                //Debug.Assert(fooResult.First().Locations.Count() == 0);
            }
            catch (ReflectionTypeLoadException e)
            {
                Console.WriteLine(e.ToString());
                var le = e.LoaderExceptions;

                foreach (var item in le)
                {
                    Console.WriteLine(item.Message.ToString());
                }
            }*/
            #endregion
        }
    }
}
