using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Resources;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.FindSymbols;

namespace CheckReadOnlyDAL
{
    public class CodeAnalyser
    {
        private string _projectFileName;
        private List<string> _targetSrcFilesList;
        private MSBuildWorkspace _workspace;
        private Project _project;
        private Compilation _compilation;
        private Solution _solution;
        private List<string>.Enumerator _targetSrcFilesListEnumerator;
        private TargetFilesFetcher _targetFilesFetcher;
        private string _srcInLastCall;
        private SqlAnalyser _sqlAnalyser;

        public CodeAnalyser(string projectFileName, List<string> targetSrcFilesList)
        {
            _projectFileName = projectFileName;
            _targetSrcFilesList = targetSrcFilesList;
            _targetSrcFilesListEnumerator = _targetSrcFilesList.GetEnumerator();

            _workspace = MSBuildWorkspace.Create();

            _solution = _workspace.CurrentSolution;

            if (_solution == null)
            {
                throw new Exception("Invalid CurrentSolution property");
            }

            _project = _workspace.OpenProjectAsync(_projectFileName).Result;

            _compilation = _project.GetCompilationAsync().Result;

            _targetFilesFetcher = new TargetFilesFetcher();

            _sqlAnalyser = new SqlAnalyser();
        }

        public void Analyze()
        {
            CheckReadOnlyDALResultMessage message = new CheckReadOnlyDALResultMessage();

            //Initialize response's message
            message.rootPath = Path.GetDirectoryName(_projectFileName);
            message.projectName = Path.GetFileNameWithoutExtension(_projectFileName);

            ISymbol calledMethodSymbol;
            while((calledMethodSymbol = getNextReadOnlyDALCalledMethod()) != null)
            {
                string spName = null;
                string typeOfDALobject = calledMethodSymbol.ContainingType.ToString();

                try
                {
                    spName = getStoredProcName(calledMethodSymbol);

                    if (string.IsNullOrEmpty(spName))//cannot find SP, I should make a smartest search :(
                    {
                        logError(message, Resource1.StoredProcedureNotFoundError, _targetSrcFilesListEnumerator.Current, _targetFilesFetcher.CurrentSourceLineNumber, typeOfDALobject, spName);
                        continue;
                    }

                    if(!_sqlAnalyser.spIsReadOnly(spName))
                        logError(message, Resource1.StoredProcedureIsNotReadOnlyError, _targetSrcFilesListEnumerator.Current, _targetFilesFetcher.CurrentSourceLineNumber, typeOfDALobject, spName);
                }
                catch(Exception e)
                {
                    logError(message, e.Message, _targetSrcFilesListEnumerator.Current, _targetFilesFetcher.CurrentSourceLineNumber, typeOfDALobject, spName);
                }
            }

            message.print();
        }

        private void logError(CheckReadOnlyDALResultMessage message, string errorMessage, string sourceFileName, int sourceLineNumber, string typeOfDALobj, string spName)
        {
            message.ErrorMessages.Add(errorMessage);
            message.SourceFileNames.Add(sourceFileName);
            message.SourceLineNumbers.Add(sourceLineNumber);
            message.TypeOfDALobjects.Add(typeOfDALobj);
            message.StoredProcedureNames.Add(spName);
        }

        private ISymbol getNextReadOnlyDALCalledMethod()
        {
            string srcFn = _targetSrcFilesListEnumerator.Current;
            int pos = 0;

            do
            {
                if (string.IsNullOrEmpty(srcFn) || pos == -1)
                {
                    if (_targetSrcFilesListEnumerator.MoveNext())
                    {
                        srcFn = _targetSrcFilesListEnumerator.Current;
                    }
                    else
                        return null;
                }

                if (_srcInLastCall == srcFn)
                    pos = _targetFilesFetcher.getNextMatchPosition();
                else
                {
                    pos = _targetFilesFetcher.getNextMatchPosition(srcFn);
                    _srcInLastCall = srcFn;
                }
            }
            while (!string.IsNullOrEmpty(srcFn) && pos == -1);

            if (pos == -1)
                return null;

            SyntaxTree tree = _compilation.SyntaxTrees.Single(t => t.FilePath.CompareTo(srcFn) == 0);

            var model = _compilation.GetSemanticModel(tree);

            return SymbolFinder.FindSymbolAtPosition(model, pos, _workspace);
        }

        private string getStoredProcName(ISymbol methodSymbol)
        {
            SyntaxNode syntaxNode = methodSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync().Result;

            //---------------------- get command's index --------------------------
            Regex rx = new Regex(@"this\.CommandCollection\[(\d+)\]");

            var expression = syntaxNode.DescendantNodes()
                .Where(e => (e.CSharpKind() == SyntaxKind.SimpleAssignmentExpression && rx.Match(((AssignmentExpressionSyntax)e).Right.ToString()).Success) || (e.CSharpKind() == SyntaxKind.EqualsValueClause && rx.Match(((EqualsValueClauseSyntax)e).Value.ToString()).Success))
                .SingleOrDefault();

            if (expression == null)
                return null;
            string expStr;
            if(expression.CSharpKind() == SyntaxKind.SimpleAssignmentExpression)
                expStr = ((AssignmentExpressionSyntax)expression).Right.ToString();
            else
                expStr = ((EqualsValueClauseSyntax)expression).Value.ToString();

            Match match = rx.Match(expStr);
            if (!match.Success)
                return null;

            string targetAssigment = string.Format("this._commandCollection[{0}].CommandText", match.Groups[1].Captures[0].Value);

            //---------------------- get stored proc's name -------------------------- 
            MethodDeclarationSyntax initCommandCollectionMethod = getInitCommandCollectionMethodFromCalledMethod(syntaxNode);

            var assignmentExpression2 = initCommandCollectionMethod.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Where(e => e.CSharpKind() == SyntaxKind.SimpleAssignmentExpression && e.Left.ToString() == targetAssigment)
                .Single();

            return assignmentExpression2.Right.ToString().Trim('\"');
        }

        private MethodDeclarationSyntax getInitCommandCollectionMethodFromCalledMethod(SyntaxNode methodSyntaxNode)
        {
            MethodDeclarationSyntax result = methodSyntaxNode.Parent.DescendantNodes().OfType<MethodDeclarationSyntax>()
                                                .Where(m => m.Identifier.ValueText == "InitCommandCollection")
                                                .Single();
            return result;
        }
    }
}
