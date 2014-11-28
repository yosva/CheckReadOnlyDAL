using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckReadOnlyDAL
{
    public class CheckReadOnlyDALResultMessage
    {
        private List<string> _errorMessages;
        private List<string> _sourceFileNames;
        private List<int> _sourceLineNumbers;
        private List<string> _typeOfDALobjects;
        private List<string> _storedProcedureNames;

        public string rootPath;
        public string projectName;

        public List<string> errorMessages
        {
            get
            { 
                if(_errorMessages == null)
                    _errorMessages = new List<string>();
                return _errorMessages;
            }
        }

        public List<string> sourceFileNames
        {
            get
            {
                if (_sourceFileNames == null)
                    _sourceFileNames = new List<string>();
                return _sourceFileNames;
            }
        }

        public List<int> sourceLineNumbers
        {
            get
            {
                if (_sourceLineNumbers == null)
                    _sourceLineNumbers = new List<int>();
                return _sourceLineNumbers;
            }
        }

        public List<string> typeOfDALobjects
        {
            get
            {
                if (_typeOfDALobjects == null)
                    _typeOfDALobjects = new List<string>();
                return _typeOfDALobjects;
            }
        }

        public List<string> storedProcedureNames
        {
            get
            {
                if (_storedProcedureNames == null)
                    _storedProcedureNames = new List<string>();
                return _storedProcedureNames;
            }
        }

        public void print()
        {
            for (int i = 0, n = _errorMessages.Count; i < n; i++)
            {
                Console.WriteLine("PROJECT: {0}; ERROR: {1}; SRCFILE: {2}; SRCLINE: {3}; DALTYPE: {4}; STOREDPROC: {5}",
                                    new object[] { projectName, _errorMessages[i], _sourceFileNames[i], _sourceLineNumbers[i], _typeOfDALobjects[i], _storedProcedureNames[i] });
            }
        }
    }
}
