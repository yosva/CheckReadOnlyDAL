using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace CheckReadOnlyDAL
{
    internal class SQLVisitor : TSqlFragmentVisitor
    {
        private int SELECTcount = 0;
        private int INSERTcount = 0;
        private int UPDATEcount = 0;
        private int DELETEcount = 0;
        string _storedProcedureSourceCode;

        public SQLVisitor(string storedProcedureSourceCode)
        {
            _storedProcedureSourceCode = storedProcedureSourceCode;
        }

        private string GetNodeTokenText(TSqlFragment fragment)
        {
            StringBuilder tokenText = new StringBuilder();
            for (int counter = fragment.FirstTokenIndex; counter <= fragment.LastTokenIndex; counter++)
            {
                tokenText.Append(fragment.ScriptTokenStream[counter].Text);
            }

            return tokenText.ToString();
        }

        // SELECTs
        public override void ExplicitVisit(SelectStatement node)
        {
            //Console.WriteLine("found SELECT statement with text: " + GetNodeTokenText(node));
            SELECTcount++;
        }

        // INSERTs
        public override void ExplicitVisit(InsertStatement node)
        {
            INSERTcount++;
        }

        // UPDATEs
        public override void ExplicitVisit(UpdateStatement node)
        {
            UPDATEcount++;
        }

        // DELETEs
        public override void ExplicitVisit(DeleteStatement node)
        {
            DELETEcount++;
        }

        public Tuple<int,int,int,int> DumpStatistics()
        {
            TSql110Parser parser = new TSql110Parser(true);

            IList<ParseError> errors;

            using(TextReader sr = new StringReader(_storedProcedureSourceCode))
            {
                TSqlFragment sqlFragment = parser.Parse(sr, out errors);

                if (errors.Count > 0)
                    throw new Exception(errors.ToString());

                sqlFragment.Accept(this);
            }

            return new Tuple<int,int,int,int>(this.SELECTcount,
                                              this.INSERTcount,
                                              this.UPDATEcount,
                                              this.DELETEcount);
        }
    }
}
