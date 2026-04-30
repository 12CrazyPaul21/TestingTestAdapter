using Microsoft.Dia;
using System.IO;

namespace Phantom.Testing.TestAdapter.Dia
{
    internal class FunctionInfo
    {
        public string Symbol { get; }
        public string SourceFile { get; }
        public uint LineNumber { get; }

        public FunctionInfo(string symbol, string sourcefile, uint line)
        {
            Symbol = symbol;
            SourceFile = sourcefile;
            LineNumber = line;
        }

        public override string ToString()
        {
            return Symbol;
        }
    }

    internal class DiaResolver
    {
        private readonly IDiaSession _diaSession;
        private readonly IDiaDataSource _diaDataSource;

        internal DiaResolver(string pdb)
        {
            if (!File.Exists(pdb))
            {
                throw new FileNotFoundException($"PDB file '{pdb}' does not exist");
            }
            _diaDataSource = DiaFactory.CreateDiaDataSource();
            _diaDataSource.loadDataFromPdb(pdb);
            _diaDataSource.openSession(out _diaSession);
        }

        internal bool HasFunction(string name)
        {
            _diaSession.globalScope.findChildren(SymTagEnum.SymTagFunction, name, 0, out IDiaEnumSymbols symbols);
            return symbols != null && symbols.count > 0;
        }

        internal FunctionInfo GetFunctionInfo(string name)
        {
            FunctionInfo result = null;
            _diaSession.globalScope.findChildren(SymTagEnum.SymTagFunction, name, 0, out IDiaEnumSymbols symbols);
            foreach (IDiaSymbol symbol in symbols)
            {
                _diaSession.findLinesByAddr(symbol.addressSection, symbol.addressOffset, (uint)symbol.length, out IDiaEnumLineNumbers lineNumbers);
                foreach (IDiaLineNumber lineNumber in lineNumbers)
                {
                    result = new FunctionInfo(name, lineNumber.sourceFile.fileName, lineNumber.lineNumber);
                    return result;
                }
            }
            return result;
        }
    }
}
