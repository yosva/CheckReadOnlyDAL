using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CheckReadOnlyDAL;
using System.Collections;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CheckReadOnlyDALTest
{
    [TestClass]
    public class UnitTest1
    {
        private TargetFilesFetcher targetFilesFetcher = new TargetFilesFetcher();

        [TestMethod]
        public void MatchSrcTest()
        {
            Assert.IsTrue( targetFilesFetcher.MatchSrc(@"return StockOrderDal.GetReadOnlyInstance().GetData(GetReferenceId(refId));") );
        }

        [TestMethod]
        public void getFileNamesTest()
        {
            IEnumerable<string> list = targetFilesFetcher.getFileNames("D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock");
            
            List<string> list2 = new List<string>(list);

            List<string> refList = new List<string>();
            refList.Add("D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock/Stock.cs");
            refList.Add("D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock/StockMiddle.cs");

            Assert.AreEqual(list2.ToString(), refList.ToString());
        }

        [TestMethod]
        public void getProjectForSrcFileTest()
        {
            Assert.AreEqual(targetFilesFetcher.getProjectForSrcFile("D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock/Stock.cs"),
                            "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\Cdiscount.Business.Stock.csproj");

            Assert.AreEqual(targetFilesFetcher.getProjectForSrcFile("D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock/StockMiddle.cs"),
                "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\Cdiscount.Business.Stock.csproj");
        }

        [TestMethod]
        public void getProjToSrcFilesDictTest()
        {
            Dictionary<string, List<string>> projToFilesDict = targetFilesFetcher.getProjToSrcFilesDict("D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock");
            
            Dictionary<string, List<string>> expectedProjToFilesDict = new Dictionary<string,List<string>>();

            List<string> fnList = new List<string> { "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\Stock.cs", "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\StockMiddle.cs" };
            expectedProjToFilesDict.Add("D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\Cdiscount.Business.Stock.csproj", fnList);

            Assert.AreEqual(projToFilesDict.ToString(), expectedProjToFilesDict.ToString());
        }

        [TestMethod]
        public void CodeAnaliserTest()
        {
            Dictionary<string, List<string>> projToFilesDict = targetFilesFetcher.getProjToSrcFilesDict("D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock");

            var myEnum = projToFilesDict.Keys.GetEnumerator();

            Assert.IsTrue(myEnum.MoveNext());

            string key = myEnum.Current;

            var codeAnaliser = new CodeAnalyser(key, projToFilesDict[key]);

            Assert.IsNotNull(codeAnaliser);
        }

        [TestMethod]
        public void getNextMatchPositionTest()
        {
            int pos = targetFilesFetcher.getNextMatchPosition("D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\StockMiddle.cs");
            Assert.AreEqual(6974, pos);

            pos = targetFilesFetcher.getNextMatchPosition();
            Assert.AreEqual(8315, pos);

            pos = targetFilesFetcher.getNextMatchPosition();
            Assert.AreEqual(8721, pos);

            pos = targetFilesFetcher.getNextMatchPosition();
            Assert.AreEqual(9203, pos);

            pos = targetFilesFetcher.getNextMatchPosition();
            Assert.AreEqual(9674, pos);

            pos = targetFilesFetcher.getNextMatchPosition();
            Assert.AreEqual(10108, pos);

            pos = targetFilesFetcher.getNextMatchPosition();
            Assert.AreEqual(10543, pos);
        }

        [TestMethod]
        public void getNextReadOnlyDALtypeAndCalledMethodTest()
        {
            var codeAnaliser = new CodeAnalyser(@"D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock/Cdiscount.Business.Stock.csproj",
                new List<string> { "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\Stock.cs", "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\StockMiddle.cs" });

            Assert.IsNotNull(codeAnaliser);

            string resultType, methodName;
            ISymbol iSymbol;
            PrivateObject accessor = new PrivateObject(codeAnaliser);

            iSymbol = (ISymbol)accessor.Invoke("getNextReadOnlyDALCalledMethod");
            resultType = iSymbol.ContainingType.ToString();
            methodName = iSymbol.MetadataName;
            Assert.AreEqual("Cdiscount.Dal.Stock.StockMiddleDataSetTableAdapters.StockOrderDal", resultType);
            Assert.AreEqual("GetData", methodName);

            iSymbol = (ISymbol)accessor.Invoke("getNextReadOnlyDALCalledMethod");
            resultType = iSymbol.ContainingType.ToString();
            methodName = iSymbol.MetadataName;
            Assert.AreEqual("Cdiscount.Dal.Stock.StockMiddleDataSetTableAdapters.WarehouseDal", resultType);
            Assert.AreEqual("GetData", methodName);

            iSymbol = (ISymbol)accessor.Invoke("getNextReadOnlyDALCalledMethod");
            resultType = iSymbol.ContainingType.ToString();
            methodName = iSymbol.MetadataName;
            Assert.AreEqual("Cdiscount.Dal.Stock.StockMiddleDataSetTableAdapters.WarehouseDal", resultType);
            Assert.AreEqual("GetDataById", methodName);

            iSymbol = (ISymbol)accessor.Invoke("getNextReadOnlyDALCalledMethod");
            resultType = iSymbol.ContainingType.ToString();
            methodName = iSymbol.MetadataName;
            Assert.AreEqual("Cdiscount.Bll.Stock.Dal.WarehouseDataSetTableAdapters.StockWarehouseDal", resultType);
            Assert.AreEqual("GetData", methodName);
        }

        [TestMethod]
        public void getStoredProcNameTest()
        {
            var codeAnaliser = new CodeAnalyser(@"D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock/Cdiscount.Business.Stock.csproj",
                new List<string> { "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\Stock.cs", "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\StockMiddle.cs" });
            
            PrivateObject accessor = new PrivateObject(codeAnaliser);

            ISymbol iSymbol = (ISymbol)accessor.Invoke("getNextReadOnlyDALCalledMethod");

            string spName = (string)accessor.Invoke("getStoredProcName", iSymbol);

            Assert.AreEqual("dbo.ps_cds_stock_getStockOrderLines", spName);
        }

        [TestMethod]
        public void getInitCommandCollectionMethodFromCalledMethodTest()
        {
            var codeAnaliser = new CodeAnalyser(@"D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock/Cdiscount.Business.Stock.csproj",
                new List<string> { "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\Stock.cs", "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\StockMiddle.cs" });

            PrivateObject accessor = new PrivateObject(codeAnaliser);
            ISymbol iSymbol = (ISymbol)accessor.Invoke("getNextReadOnlyDALCalledMethod");

            SyntaxNode syntaxNode = iSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync().Result;

            MethodDeclarationSyntax initCommandCollectionMethodDeclarationSyntax = (MethodDeclarationSyntax)accessor.Invoke("getInitCommandCollectionMethodFromCalledMethod", syntaxNode);

            Assert.IsNotNull(initCommandCollectionMethodDeclarationSyntax);
            Assert.AreEqual("InitCommandCollection", initCommandCollectionMethodDeclarationSyntax.Identifier.ValueText);
        }

        [TestMethod]
        public void getStoredProcedureSourceCodeTest()
        {
            var sqlAnalyser = new SqlAnalyser();

            string spSrc = sqlAnalyser.getStoredProcedureSourceCode("dbo.ps_fwk_stock_s_warehouse_by_id");

            Assert.AreEqual(@"
-- =============================================           
-- Projet		: ?? (MAJ du 25/01/2012 : DSI 606 - Date expédition commande)
-- Author			:  Sébastien Crocquesel  
-- Create date  	: ??
-- Description		: Gets a warehouse row  
-- History         	: 1.2 (??)
-- Version         	: 1.2 (??) N.Bellino 25/01/2012 : ajout d'une colonne 
-- =============================================    

CREATE PROCEDURE [dbo].[ps_fwk_stock_s_warehouse_by_id]  
(  
	@WarehouseId varchar(12)  
)  
AS  
BEGIN  

	SET NOCOUNT ON  
	SELECT WarehouseId,Title, MaximumSmoothing 
	FROM fwk_stock_warehouse (nolock)  
	WHERE WarehouseId = @WarehouseId COLLATE database_default  

END ", spSrc);
        }

        [TestMethod]
        public void spIsReadOnlyTest()
        {
            var sqlAnalyser = new SqlAnalyser();

            bool r = sqlAnalyser.spIsReadOnly("dbo.ps_fwk_stock_s_warehouse_by_id");
            Assert.IsTrue(r);

            r = sqlAnalyser.spIsReadOnly("dbo.ps_fwk_stock_i_vat");
            Assert.IsFalse(r);
        }

        [TestMethod]
        public void logErrorTest()
        {
            var codeAnaliser = new CodeAnalyser(@"D:/Main/Service/Services/ServicesR1/Cdiscount.Business.Stock/Cdiscount.Business.Stock.csproj",
                new List<string> { "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\Stock.cs", "D:\\Main\\Service\\Services\\ServicesR1\\Cdiscount.Business.Stock\\StockMiddle.cs" });

            PrivateObject accessor = new PrivateObject(codeAnaliser);
            
            CheckReadOnlyDALResultMessage message = new CheckReadOnlyDALResultMessage();

            accessor.Invoke("logError", new object[]{message, "msg", "file1.cs", 5, "DALType", "spTest"});

            Assert.AreEqual(1, message.errorMessages.Count);
            Assert.AreEqual("msg", message.errorMessages[0]);

            Assert.AreEqual(1, message.sourceFileNames.Count);
            Assert.AreEqual("file1.cs", message.sourceFileNames[0]);

            Assert.AreEqual(1, message.sourceLineNumbers.Count);
            Assert.AreEqual(5, message.sourceLineNumbers[0]);

            Assert.AreEqual(1, message.typeOfDALobjects.Count);
            Assert.AreEqual("DALType", message.typeOfDALobjects[0]);

            Assert.AreEqual(1, message.storedProcedureNames.Count);
            Assert.AreEqual("spTest", message.storedProcedureNames[0]);
        }
    }
}
