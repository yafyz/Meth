// https://github.com/yafyz
namespace Lexer {
    public enum LexemType {
        Number     = 0  | LexemMask.Value,
        BraceOpen  = 1,
        BraceClose = 2,
        Operation  = 3,
        OPplus     = 4,
        OPminus    = 5,
        OPmult     = 6  | LexemMask.MultPow,
        OPdiv      = 7  | LexemMask.MultPow,
        OPpow      = 8  | LexemMask.MultPow,
        Variable   = 9  | LexemMask.Value,
        Function   = 10 | LexemMask.Value,
    }

    public static class LexemMask {
        public const LexemType MultPow = (LexemType)0b1000_0000;
        public const LexemType Value = (LexemType)0b0100_0000;
    }

    public enum LexemSign {
        Positive,
        Negative
    }

    public class Lexem {
        public LexemType type;
        public LexemSign sign;
        public LNum value;
        public LName name;
    }

    public class LNum {
        public double v;
        public int slen;
    }

    public class LName {
        public string name;

        public int slen;
    }

    public enum LexemChunkType {
        Lexem,
        Chunk,
    }

    public class LexemChunk {
        public LexemChunkType type;
        public Lexem Value;
        public LexemChunk[] Chunks;
        public int Length;
    }

    class Lexer {
        static string ops = "+-*/^";
        static string open_brace = "({[";
        static string close_brace = "]})";
        static string numbers = "0123456789.";
        static string chars = "qwertzuiopasdfghjklyxcvbnmQWERTZUIOPASDFGHJKLYXCVBNM";
        static System.Globalization.NumberFormatInfo nfi = new System.Globalization.NumberFormatInfo() { NumberDecimalSeparator = "." };

        public static Lexem[] LexString(string expr) {
            List<Lexem> ret = new List<Lexem>();
            #region lex_expr
            for (int i = 0; i < expr.Length; i++) {
                char c = expr[i];
                if (ops.IndexOf(c) > -1) {
                    Lexem l = new Lexem();
                    switch (c) {
                        case '+':
                            l.type = LexemType.OPplus;
                            break;
                        case '-':
                            l.type = LexemType.OPminus;
                            break;
                        case '*':
                            l.type = LexemType.OPmult;
                            break;
                        case '/':
                            l.type = LexemType.OPdiv;
                            break;
                        case '^':
                            l.type = LexemType.OPpow;
                            break;
                    }
                    ret.Add(l);
                } else if (open_brace.IndexOf(c) > -1) {
                    ret.Add(new Lexem() { type = LexemType.BraceOpen });
                } else if (close_brace.IndexOf(c) > -1) {
                    ret.Add(new Lexem() { type = LexemType.BraceClose });
                } else if (numbers.IndexOf(c) > -1) {
                    LNum n = lex_num(expr, i);
                    i += n.slen - 1;
                    ret.Add(new Lexem() { type = LexemType.Number, value = n });
                } else if (chars.IndexOf(c) > -1) {
                    LName name = lex_name(expr, i);
                    Lexem l = new Lexem() { name = name };

                    if (0x5B > name.name[0]) {
                        l.type = LexemType.Function;
                    } else {
                        l.type = LexemType.Variable;
                    }

                    i += name.slen - 1;
                    ret.Add(l);
                } else {
                    throw new Exception("you fuck");
                }
            }
            #endregion lex_expr
            #region apply_signs
            List<Lexem> toDel = new List<Lexem>();
            for (int i = 0; i < ret.Count(); i++) {
                switch (ret[i].type) {
                    case LexemType.OPminus:
                        ret[i + 1].sign = LexemSign.Negative;
                        goto case LexemType.OPplus;
                    case LexemType.OPplus:
                        toDel.Add(ret[i]);
                        break;
                }
            }
            toDel.ForEach(x => ret.Remove(x));
            #endregion apply_signs
            return ret.ToArray();
        }

        public static LexemChunk LexChunks(Lexem[] lexems, int start = 0) {
            LexemChunk ret = new LexemChunk() { Value = new Lexem() };
            List<LexemChunk> toRet = new List<LexemChunk>();
            ret.type = LexemChunkType.Chunk;

            int i = start;
            for (; i < lexems.Length; i++) {
                Lexem v = lexems[i];
                if (v.type == LexemType.BraceOpen) {
                    LexemChunk l = LexChunks(lexems, i + 1);
                    l.Value = v;
                    toRet.Add(l);
                    i += l.Length + 1;
                } else if (v.type == LexemType.BraceClose) {
                    break;
                } else {
                    toRet.Add(new LexemChunk() { type = LexemChunkType.Lexem, Value = v });
                }
            }

            ret.Length = i - start;
            ret.Chunks = toRet.ToArray();

            fix_funcs(ret);

            return ret;
        }

        public static void fix_funcs(LexemChunk lxc) {
            List<LexemChunk> vals = new List<LexemChunk>();
            LexemChunk[] chunks = lxc.Chunks;
            for (int i = 0; i < chunks.Length; i++) {
                if (chunks[i].type == LexemChunkType.Lexem && chunks[i].Value.type == LexemType.Function) {
                    if (chunks[i + 1].type == LexemChunkType.Chunk)
                        fix_funcs(chunks[i + 1]);

                    vals.Add(new LexemChunk() {
                        type = LexemChunkType.Chunk,
                        Value = new Lexem() {
                            type = LexemType.BraceOpen,
                            sign = LexemSign.Positive
                        },
                        Chunks = new LexemChunk[] { chunks[i], chunks[i + 1] }
                    });
                    i++;
                } else if (chunks[i].type == LexemChunkType.Chunk) {
                    fix_funcs(chunks[i]);
                    vals.Add(chunks[i]);
                } else {
                    vals.Add(chunks[i]);
                }
            }
            lxc.Chunks = vals.ToArray();
        }

        static LNum lex_num(string expr, int s) {
            int i = s;
            for (; i < expr.Length && numbers.IndexOf(expr[i]) > -1; i++) ;

            return new LNum {
                v = double.Parse(expr[s..i], nfi),
                slen = i - s,
            };
        }

        static LName lex_name(string expr, int s) {
            int i = s;
            for (; i < expr.Length && chars.IndexOf(expr[i]) > -1; i++) ;
            return new LName {
                name = expr[s..i],
                slen = i - s,
            };
        }
    }
}