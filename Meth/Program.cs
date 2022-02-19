// https://github.com/yafyz
using Lexer;
using Parser;

namespace Program {
    class Program {
        static void Main(string[] argv) {
            //string expr = "1-22+4*9^2^2/2+(1+2+(3+4)*-(5+6+(4*2)))+1";
            //string expr = "(+3+4)*(+5+6)";//"1-22+6*(1*2+2*2+(1*2))";
            string expr = "Sin(-a)";
            Lexem[] lx = Lexer.Lexer.LexString(expr);
            LexemChunk lxc = Lexer.Lexer.LexChunks(lx);
            rec(lxc);
            Console.WriteLine("------------------");

            Parser.Operations.Add res = Parser.Parser.Parse(lxc);

            Context ctx = new Context();
            ctx.Variables.Add("a", new Number(10));
            ctx.Functions.Add("Sin", (ctx, bv) => Math.Sin(bv.GetResult(ctx)));

            outParse(res);
            Console.WriteLine("------------------");
            Console.WriteLine(res.GetResult(ctx));
        }

        // pretty printing them structures

        static void OutputLexem(Lexer.Lexem l, int indent = 0) {
            if (l.type == LexemType.Number) {
                Console.WriteLine($"{new String(' ', indent * 4)}{(l.sign == LexemSign.Positive ? "+" : "-")}{l.value.v}");
            } else if ((l.type & LexemMask.Value) != 0) {
                Console.WriteLine($"{new String(' ', indent * 4)}{(l.sign == LexemSign.Positive ? "+" : "-")}{l.name.name}{(l.type == LexemType.Function ? "()" : "")}");
            } else if (l.type == LexemType.BraceOpen) {
                Console.WriteLine($"{new String(' ', indent * 4)}{(l.sign == LexemSign.Positive ? "+" : "-")}{l.type}");
            } else {
                Console.WriteLine($"{new String(' ', indent * 4)}{l.type}");
            }
        }

        static string ind(int n) {
            return new string(' ', n * 4);
        }

        static void rec(Lexer.LexemChunk lx, int indent = 0) {
            foreach (var v in lx.Chunks) {
                if (v.type == Lexer.LexemChunkType.Lexem) {
                    OutputLexem(v.Value, indent);
                } else {
                    Console.WriteLine($"{new String(' ', indent * 4)}{(v.Value.sign == Lexer.LexemSign.Positive ? "+" : "-")}(");
                    rec(v, indent + 1);
                    Console.WriteLine($"{new String(' ', indent * 4)} )");
                }
            }
        }

        static void outParsePrint(BaseValue b, BaseChunk c, int indent) {
            if (b is Number) {
                Console.WriteLine($"{ind(indent + 1)}{b.Value}");
            } else if (b is Variable) {
                Console.WriteLine($"{ind(indent + 1)}{(b.Sign == 1 ? "+" : "-")}{((Variable)b).Name}");
            } else if (b is Function) {
                Console.WriteLine($"{ind(indent + 1)}{(b.Sign == 1 ? "+" : "-")}{((Function)b).Name}(");
                outParsePrint(((Function)b).Arg, c, indent + 1);
                Console.WriteLine($"{ind(indent + 1)})");
            } else {
                outParse((BaseChunk)b, indent + 1);
            }
        }

        static void outParse(BaseChunk c, int indent = 0) {
            Console.WriteLine($"{ind(indent)}{(c.Sign == 1 ? "+" : "-")}{c.GetType().Name}(");
            foreach (BaseValue b in c.Values) {
                outParsePrint(b, c, indent);
            }
            Console.WriteLine($"{ind(indent)})");
        }
    }
}