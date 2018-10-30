using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CatSAT;
using UnityEngine;
using static CatSAT.Language;

/// <summary>
/// Parses and stores the questions from a questionnaire
/// </summary>
// ReSharper disable once UnusedMember.Global
// ReSharper disable once ClassNeverInstantiated.Global
public class Questionnaire : MonoBehaviour
{
    /// <summary>
    /// Name of the .txt file in Resources containing the questionnaire
    /// </summary>
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public string ResourceName = "Questionnaire.txt";

    /// <summary>
    /// The questions themselves
    /// </summary>
    public readonly List<Question> Questions = new List<Question>();


    /// <summary>
    /// Load in the questionnaire
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public void Start ()
    {
        Predicate.Initialize();
        FindObjectOfType<World>().Initialize();
        Problem.Current = World.Problem;          // Just to be paranoid

        ParseQuestionnaire(Resources.Load<TextAsset>(ResourceName).text);
	}

    #region Parsing
    /// <summary>
    /// Text of the current question, if any
    /// </summary>
    string questionText;
    /// <summary>
    /// Implications of the current question, if any
    /// </summary>
    readonly List<Literal> questionImplications = new List<Literal>();
    /// <summary>
    /// Text of the current answer, if any
    /// </summary>
    string answerText;
    /// <summary>
    /// Implications of the current answer, if any
    /// </summary>
    readonly List<Literal> answerImplications = new List<Literal>();
    /// <summary>
    /// Answers to the current question, if any
    /// </summary>
    readonly List<Quip> answers = new List<Quip>();
    /// <summary>
    /// True if we most recently saw a Q: rather than an A:
    /// </summary>
    private bool parsingQuestion = true;

    private void ParseQuestionnaire(string text)
    {
        foreach (var line in text.Split('\n'))
            ParseLine(line);
        FinishQuestion();
    }

    private void ParseLine(string line)
    {
// Comments and whitespace
        if (line.StartsWith("//") || line.Trim().Length == 0)
            return;

        ParseCommand(line);
    }

    private void ParseCommand(string line)
    {
        string lhs, rhs, args;

        // Questions and answers
        if (IsCommand("Q:", line, out args))
            ParseQuestionHeader(args);
        else if (IsCommand("A:", line, out args))
            ParseAnswerHeader(args);
        else if (char.IsWhiteSpace(line[0]))
            ParseImplications(line);
        else if (DivideAt("<=", line, out lhs, out rhs))
            World.Problem.Assert(new Rule((Proposition) ParseLiteral(lhs), ParseLiteralsAsExpression(rhs)));
        else if (DivideAt("<-", line, out lhs, out rhs))
            World.Problem.Assert(new Implication((Proposition) ParseLiteral(lhs), ParseLiteralsAsExpression(rhs)));
        else if (IsCommand("contradiction:", line, out args))
            World.Problem.Inconsistent(ParseLiterals(args));
        else if (IsCommand("unique:", line, out args))
            World.Problem.Unique(ParseLiterals(args));
        else if (IsCommand("mutually exclusive:", line, out args))
            World.Problem.AtMost(1, ParseLiterals(args));
    }

    private void ParseAnswerHeader(string args)
    {
        FinishAnswer();
        parsingQuestion = false;
        answerText = args;
    }

    private void ParseQuestionHeader(string args)
    {
        FinishQuestion();
        questionText = args;
    }

    private void ParseImplications(string line)
    {
        var props = ParseLiterals(line);
        if (parsingQuestion)
            questionImplications.AddRange(props);
        else
            answerImplications.AddRange(props);
    }

    /// <summary>
    /// Package up the current question and its answers in the the Questions list
    /// </summary>
    void FinishQuestion()
    {
        if (questionText == null)
            return;
        FinishAnswer();
        Questions.Add(new Question(questionText, questionImplications.ToArray(), answers.ToArray()));
        parsingQuestion = true;
        questionText = null;
        questionImplications.Clear();
        answers.Clear();
    }

    /// <summary>
    /// Package up the current answer and its implications in to the answers list.
    /// </summary>
    void FinishAnswer()
    {
        if (answerText == null)
            return;
        answers.Add(new Quip(answerText, answerImplications.ToArray()));
        answerText = null;
        answerImplications.Clear();
    }

    /// <summary>
    /// True if str starts with prefix.  If so, stores the remainder in argument. 
    /// </summary>
    bool IsCommand(string prefix, string str, out string argument)
    {
        if (!str.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
        {
            argument = null;
            return false;
        }

        argument = str.Substring(prefix.Length).Trim();
        return true;
    }

    /// <summary>
    /// True if str contains delimter.  If so, stores the text to the left of deliminter in lhs and that to the right in rhs.
    /// </summary>
    bool DivideAt(string delimiter, string str, out string lhs, out string rhs)
    {
        // ReSharper disable once StringIndexOfIsCultureSpecific.1
        var startOfDelim = str.IndexOf(delimiter);
        if (startOfDelim < 0)
        {
            lhs = rhs = null;
            return false;
        }

        lhs = str.Substring(0, startOfDelim);
        rhs = str.Substring(startOfDelim + delimiter.Length);
        return true;
    }

    /// <summary>
    /// Given a string such as "foo" or "!foo" returns the literal represented by the string.
    /// </summary>
    public Literal ParseLiteral(string lit)
    {
        lit = lit.Trim();
        if (lit.StartsWith("!"))
            return Not(ParsePropositionString(lit.Substring(1)));
        return ParsePropositionString(lit);
    }

    private Literal ParsePropositionString(string proposition)
    {
        //var words = SplitWords(proposition);
        //return ParsePropositionWords(words, proposition);

        return ParsePropositionWords(TokenizePropositionString(proposition).ToArray(), proposition);
    }

    private readonly StringBuilder tokenBuffer = new StringBuilder();
    IEnumerable<string> TokenizePropositionString(string propositionString)
    {
        tokenBuffer.Length = 0;
        foreach (var c in propositionString)
        {
            if (char.IsWhiteSpace(c))
            {
                if (tokenBuffer.Length > 0)
                {
                    yield return tokenBuffer.ToString();
                    tokenBuffer.Length = 0;
                }
            } else if ("=<>".Contains(c))
            {
                // It's a single-character operator
                if (tokenBuffer.Length > 0)
                {
                    yield return tokenBuffer.ToString();
                    tokenBuffer.Length = 0;
                }

                yield return new string(new[] {c});
            }
            else
                tokenBuffer.Append(c);
        }

        if (tokenBuffer.Length > 0)
        {
            yield return tokenBuffer.ToString();
            tokenBuffer.Length = 0;
        }
    }

    private readonly string[] operators = {"=", "<", ">", "<=", ">="};
    private Literal ParsePropositionWords(string[] words, string source)
    {
        if (words[0] == "not")
            return Not(ParsePropositionWords(words.Skip(1).ToArray(), source));

        if (words.Length == 1 && !string.IsNullOrEmpty(words[0]))
            return ParseAtomicProposition(words[0]);
        if (words.Length == 3 && operators.Contains(words[1]))
            return ParseOperatorExpression(words[1], words[0], words[2]);

        foreach (var rule in ParserRule.Rules)
            if (rule.Match(words))
                return rule.Parse(words);

        throw new ArgumentException($"Invalid proposition syntax: \"{source}\"");
    }

    class OperatorCase
    {
        public Func<object, bool> LeftTest;
        public Func<object, bool> RightTest;
        public Func<object, object, Literal> Parser;

        public OperatorCase(Func<object, bool> leftTest, Func<object, bool> rightTest, Func<object, object, Literal> parser)
        {
            LeftTest = leftTest;
            RightTest = rightTest;
            Parser = parser;
        }

        public OperatorCase(Type leftType, Type rightType, Func<object, object, Literal> parser)
            : this(TypePredicate(leftType), TypePredicate(rightType), parser)
        { }

        private static Func<object, bool> TypePredicate(Type t)
        {
            return t.IsInstanceOfType;
        }

        public Literal TryParse(object lhs, object rhs)
        {
            return LeftTest(lhs) && RightTest(rhs) ? Parser(lhs, rhs) : null;
        }

        public static Literal Parse(string op, OperatorCase[] cases, object lhs, object rhs)
        {
            foreach (var c in cases)
            {
                var p = c.TryParse(lhs, rhs);
                if (!ReferenceEquals(p, null))
                    return p;
            }

            throw new Exception($"Invalid expression: {lhs}{op}{rhs}");
        }
    }

    private readonly OperatorCase[] eqCases = new[]
    {
        new OperatorCase(typeof(FDVariable<string>), typeof(string), (a, b) => (FDVariable<string>) a == (string) b)
    };

    private readonly OperatorCase[] ltCases = new[]
    {
        new OperatorCase(typeof(FloatVariable), typeof(float), (a, b) => (FloatVariable) a < (float) b)
    };

    private readonly OperatorCase[] gtCases = new[]
    {
        new OperatorCase(typeof(FloatVariable), typeof(float), (a, b) => (FloatVariable) a > (float) b)
    };

    private Literal ParseOperatorExpression(string op, string lhs, string rhs)
    {
        object realLhs = ParseOperand(lhs);
        object realRhs = ParseOperand(rhs);
        switch (op)
        {
            case "=":
                return OperatorCase.Parse(op, eqCases, realLhs, realRhs);

            case "<":
            case "<=":
                return OperatorCase.Parse(op, ltCases, realLhs, realRhs);

            case ">":
            case ">=":
                return OperatorCase.Parse(op, gtCases, realLhs, realRhs);

            default: throw new ArgumentException($"Unknown operator {op}");
        }
    }

    private object ParseOperand(string expr)
    {
        var v = World.Problem.VariableNamed(expr);
        if (!ReferenceEquals(v, null)) return v;
        float f;
        if (float.TryParse(expr, out f))
            return f;
        return expr;
    }

    private static Literal ParseAtomicProposition(string word)
    {
        if (Sort.Sorts.ContainsKey(word) || Sort.SortOf(word) != null)
            return Predicate.Predicates["exists"].Call(word);

        return (Proposition) word;
    }

    private static string[] SplitWords(string proposition)
    {
        var words = proposition.Split(' ').Select(s => s.Trim()).Where(s => s != "").ToArray();
        if (words.Length == 0)
            throw new ArgumentException($"Invalid proposition syntax: \"{proposition}\"");
        return words;
    }

    /// <summary>
    /// Given string with a comma-separated list of literals, parse and return the listerals
    /// </summary>
    private IEnumerable<Literal> ParseLiterals(string literals)
    {
        return literals.Split(',').Select(ParseLiteral);
    }

    /// <summary>
    /// Given string with a comma-separated list of literals, parse them and return them as a Conjunction object
    /// </summary>
    private Expression ParseLiteralsAsExpression(string literals)
    {
        return ParseLiterals(literals).Aggregate<Expression>((rest, first) => new Conjunction(rest, first));
    }

    #endregion
}
