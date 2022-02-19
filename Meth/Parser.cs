// https://github.com/yafyz
using Lexer;

namespace Parser {
    public class Context {
        public Dictionary<string, BaseValue> Variables = new();
        public Dictionary<string, Func<Context, BaseValue, double>> Functions = new();
    }

    public interface BaseValue {
        public double Value { get => GetResult(); }
        public double GetResult(Context ctx = null);
        public int Sign { get; set; }
    }

    public abstract class BaseChunk : BaseValue {
        public double Value { get => GetResult(); }
        public int Sign { get; set; } = 1;
        public BaseValue[] Values;

        public BaseChunk(BaseValue[] values) {
            this.Values = values;
        }

        public BaseChunk() {
            this.Values = new BaseValue[0];
        }

        abstract public double GetResult(Context ctx = null);
    }

    public class Number : BaseValue {
        public double Value { get => GetResult(); }
        public int Sign { get; set; } = 1;
        double _value;

        public Number(double v) {
            this._value = v;
        }

        public Number(Lexem v) {
            this._value = v.value.v;
            if (v.sign == LexemSign.Negative)
                this.Sign = -1;
        }

        public double GetResult(Context ctx = null) {
            return _value * Sign;
        }
    }

    public class Variable : BaseValue {
        public int Sign { get; set; } = 1;
        public string Name;

        public Variable(Lexem lx) {
            this.Name = lx.name.name;
            if (lx.sign == LexemSign.Negative)
                this.Sign = -1;
        }

        public double GetResult(Context ctx = null) {
            if (ctx == null)
                throw new Exception("Context required");
            return ctx.Variables[Name].GetResult(ctx) * Sign;
        }
    }

    public class Function : BaseValue {
        public int Sign { get; set; } = 1;
        public BaseValue Arg;
        public string Name;

        public Function(Lexem lx) {
            this.Name = lx.name.name;
            if (lx.sign == LexemSign.Negative)
                this.Sign = -1;
        }

        public double GetResult(Context ctx = null) {
            if (ctx == null)
                throw new Exception("Context required");
            return ctx.Functions[Name](ctx, Arg) * Sign;
        }
    }

    namespace Operations {
        public class Add : BaseChunk {
            public Add(BaseValue[] values) : base(values) { }
            public Add() : base() { }


            public override double GetResult(Context ctx = null) {
                double ret = 0;
                foreach (BaseValue v in this.Values)
                    ret += v.GetResult(ctx);
                return ret * Sign;
            }
        }

        public class Mult : BaseChunk {
            public Mult(BaseValue[] values) : base(values) { }
            public Mult() : base() { }

            public override double GetResult(Context ctx = null) {
                double ret = 1;
                foreach (BaseValue v in this.Values)
                    ret *= v.GetResult(ctx);
                return ret * Sign;
            }
        }

        public class Pow : BaseChunk {
            public Pow(BaseValue[] values) : base(values) {
                if (values.Length < 2)
                    throw new ArgumentException($"Invalid ammount of values, got {values.Length}, expected atleast 2");
            }
            public Pow() : base() { }

            public override double GetResult(Context ctx = null) {
                double right = Values[^1].GetResult(ctx);
                for (int i = Values.Length - 2; i > -1; i--)
                    right = Math.Pow(Values[i].GetResult(ctx), right);
                return right * Sign;
            }
        }
    }

    class Parser {
        public static BaseValue LXC2BV(LexemChunk lx, LexemChunk[] chunks, ref int i) {
            if (lx.type == LexemChunkType.Lexem) {
                switch (lx.Value.type) {
                    case LexemType.Number:
                        return new Number(lx.Value);
                    case LexemType.Variable:
                        return new Variable(lx.Value);
                    case LexemType.Function:
                        Function f = new Function(lx.Value);
                        f.Arg = LXC2BV(chunks[++i], chunks, ref i);
                        return f;
                    default:
                        throw new Exception("Invalid LexemType given to LXC2BV");
                }
            } else {
                return Parse(lx);
            }
        }

        public static Tuple<BaseChunk, int> ParsePows(LexemChunk[] chunks, int start) {
            List<BaseValue> powVals = new List<BaseValue>();
            int i = start;

            do {
                powVals.Add(LXC2BV(chunks[i], chunks, ref i));
            } while ((i += 2) < chunks.Length && chunks[i - 1].type == LexemChunkType.Lexem && chunks[i - 1].Value.type == LexemType.OPpow);

            return new Tuple<BaseChunk, int>(new Operations.Pow(powVals.ToArray()), i - start - 2);
        }

        public static Tuple<BaseChunk, int> SubParse(LexemChunk chunk, int start) {
            List<BaseValue> vals = new List<BaseValue>();
            int i = start;
            LexemChunk[] chunks = chunk.Chunks;

            do {
                LexemChunk lxc = chunks[i];
                if (i < chunks.Length - 1 && chunks[i + 1].Value?.type == LexemType.OPpow) {
                    var r = ParsePows(chunks, i);
                    vals.Add(r.Item1);
                    i += r.Item2;
                } else {
                    if (i > 0 && chunks[i - 1].Value?.type == LexemType.OPdiv) {
                        vals.Add(new Operations.Pow(new BaseValue[] { LXC2BV(lxc, chunks, ref i), new Number(-1) }));
                    } else {
                        vals.Add(LXC2BV(lxc, chunks, ref i));
                    }
                }
            } while ((i += 2) < chunks.Length && chunks[i - 1].type == LexemChunkType.Lexem && (chunks[i - 1].Value?.type & LexemMask.MultPow) != 0);

            return new Tuple<BaseChunk, int>(new Operations.Mult(vals.ToArray()), i - start - 2);
        }

        public static Operations.Add Parse(LexemChunk chunk) {
            List<BaseValue> vals = new List<BaseValue>();
            int len = chunk.Chunks.Length;

            for (int i = 0; i < len; i++) {
                LexemChunk lx = chunk.Chunks[i];
                LexemChunk next;

                if (i == len - 1 || (next = chunk.Chunks[i + 1]).type == LexemChunkType.Chunk
                                 || i < len - 1 && next.type == LexemChunkType.Lexem && (next.Value.type & LexemMask.Value) != 0) {

                    vals.Add(LXC2BV(lx, chunk.Chunks, ref i));
                } else {
                    var t = SubParse(chunk, i);
                    vals.Add(t.Item1);
                    i += t.Item2;
                }
            }

            return new Operations.Add(vals.ToArray()) { Sign = chunk.Value?.sign == LexemSign.Negative ? -1 : 1 };
        }
    }
}
