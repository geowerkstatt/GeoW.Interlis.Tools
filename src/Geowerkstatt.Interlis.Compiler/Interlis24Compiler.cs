using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler;

public class Interlis24Compiler
{
    public static void Main()
    {
        var file = "";

        using (StreamReader fileStream = new StreamReader(file))
        {
            var inputStream = new AntlrInputStream(fileStream);
            var interlisLexer = new Interlis24Lexer(inputStream);
            var interlisParser = new Interlis24Parser(new CommonTokenStream(interlisLexer));

            var tree = interlisParser.interlis();
        }
    }
}
