using System;
using System.Collections.Generic;
using System.Text;
using CatSAT;
using CatSAT.NonBoolean.SMT.MenuVariables;

public class Predicate
{
    public static Predicate Exists;
    public static Predicate Male;
    public static Predicate Female;

    public static Func<string, MenuVariable<string>> NameOf;
    
    public static void Initialize()
    {
        Problem.Current = World.Problem; // Just to be paranoid

        Exists = AddPredicate("exists", Sort.Person, new[] {null, "exists"});
        Exists.Quiet = true;
        Male = AddPredicate("male", Sort.Person,
            new [] { null, "be", "male" },
            new [] {"male", null });
        Male.Quiet = true;
        Female = AddPredicate("female", Sort.Person,
            new [] { null, "be", "female" },
            new [] { "female", null} );
        Female.Quiet = true;

        NameOf = Sort.Function("name", Sort.Person, (e,vName) => new MenuVariable<string>(vName, null, Problem.Current, Exists.Call(e)));
        Problem.Current.Assert(
            Male.Call("father"),
            Female.Call("mother"),
            Male.Call("brother"),
            Female.Call("sister"));
        var maleNames = World.MenuFromResource("male names", "MaleFirstNames");
        var femaleNames = World.MenuFromResource("female names", "FemaleFirstNames");

        foreach (var p in Sort.Person.Members())
        {
            Problem.Current.Unique(Male.Call(p), Female.Call(p));
            Problem.Current.Assert(NameOf(p).In(maleNames)
                           <= Male.Call(p));
            Problem.Current.Assert(NameOf(p).In(femaleNames)
                           <= Female.Call(p));
        }

        var likes = AddPredicate("likes", Sort.Entity,
            new[] { null, "like", null },
            new[] { "likes", null });
        likes.ImplicitSubject = "self";

        var dislikes = AddPredicate("dislikes", Sort.Entity,
            new[] { null, "dislike", null },
            new [] {"dislikes", null });
        dislikes.ImplicitSubject = "self";

        MutuallyExclusive(likes, dislikes);

        var loves = AddPredicate("loves", Sort.Person,
            new[] { null, "love", null },
            new [] { "loves", null }).AddGeneralization(likes).AddGeneralization(Exists);
        loves.ImplicitSubject = "self";

        var hates = AddPredicate("hates", Sort.Person,
            new[] { null, "hate", null },
            new[] { "hates", null }).AddGeneralization(dislikes).AddGeneralization(Exists);
        hates.ImplicitSubject = "self";

        AddPredicate("alcoholic", Sort.Person,
            new[] { null, "be", "an alcoholic" },
            new [] { "alcoholic", null }).AddGeneralization(Exists);

        MutuallyExclusive(loves, hates);
        //MutuallyExclusive(loves, dislikes);

        AddPredicate("abusive", Sort.Person,
            new[] {null, "be","abusive"},
            new[] { "abusive", null },
            new[] { null, "abusive" }).AddGeneralization(hates);

        var tastes = AddPredicate("tastes", Sort.Entity,
            new[] { null, "have", null, "tastes"},
            new [] { null, "tastes" });
        tastes.ImplicitSubject = "self";

        var living = AddPredicate("living", Sort.Person,
            new[] { null, "be", "alive" },
            new[] { "living", null },
            new [] { null, "living" }).AddStrongGeneralization(Exists);
        living.Quiet = true;

        var dead = AddPredicate("dead", Sort.Person,
            new[] { null, "be", "dead" },
            new[] { "dead", null },
            new[] { null, "dead" }).AddStrongGeneralization(Exists);

        MutuallyExclusive(living, dead);
    }

    /// <summary>
    /// Name of the predicate, for debugging purposes
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// True if we shouldn't print text about this predicate
    /// </summary>
    public bool Quiet;

    /// <summary>
    /// For single-argument predicates: the subject to realize when generating NL
    /// </summary>
    public string ImplicitSubject;

    /// <summary>
    /// Sort of the predicate's first argument
    /// </summary>
    private readonly Sort arg1Sort;

    /// <summary>
    /// Sort of the predicate's second argument, or null if this is a unary predicate
    /// </summary>
    private readonly Sort arg2Sort;

    private string[] generationPattern;

    /// <summary>
    /// The predicate object actually used by CatSAT
    /// </summary>
    private readonly Delegate lowLevelPredicate;

    /// <summary>
    /// Predicates that generalize this predicate
    /// </summary>
    private readonly List<Predicate> generalizations = new List<Predicate>();

    private readonly List<Predicate> strongGeneralizations = new List<Predicate>();
    private readonly List<Predicate> strongSpecializations = new List<Predicate>();


    /// <summary>
    /// Predicates that are automatically false if this is true
    /// </summary>
    private readonly List<Predicate> negativeGeneralizations = new List<Predicate>();

    /// <summary>
    /// When this predicate is unary, list of arguments this predicate has been called on
    /// </summary>
    private readonly List<string> oneArgDomain;

    /// <summary>
    /// When this predicate is binary, list of arguments this predicate has been called on
    /// </summary>
    private readonly List<Tuple<string, string>> twoArgDomain;

    public bool IsUnary => oneArgDomain != null;

    public bool IsBinary => twoArgDomain != null;

    /// <summary>
    /// The extension of this predicate within the current model, as represented by the set
    /// of its true instantiations.
    /// </summary>
    public IEnumerable<Proposition> Extension(Sort sort)
    {
        foreach (var arg in oneArgDomain)
        {
            if (Sort.IsA(arg, sort))
            {
                var prop = LowLevelCall(arg);
                if (World.Solution[prop])
                    yield return prop;
            }
        }
    }

    /// <summary>
    /// The extension of this predicate within the current model, as represented by the set
    /// of its true instantiations.
    /// </summary>
    public IEnumerable<Proposition> Extension(Sort sort1, Sort sort2)
    {
        foreach (var args in twoArgDomain)
        {
            if (Sort.IsA(args.Item1, sort1) && Sort.IsA(args.Item2, sort2))
            {
                var prop = LowLevelCall(args.Item1, args.Item2);
                if (World.Solution[prop])
                    yield return prop;
            }
        }
    }

    /// <summary>
    /// Returns the set { rightArg | this(leftArg, rightArg) true in this model }
    /// </summary>
    /// <param name="leftArg">First argument to this proposition</param>
    public IEnumerable<string> RightRelata(string leftArg)
    {
        foreach (var args in twoArgDomain)
        {
            if (args.Item1 == leftArg && World.Solution[LowLevelCall(args.Item1, args.Item2)])
                yield return args.Item2;
        }
    }

    /// <summary>
    /// Returns the set { leftArg | this(leftArg, rightArg) true in this model }
    /// </summary>
    /// <param name="rightArg">First argument to this proposition</param>
    public IEnumerable<string> LeftRelata(string rightArg)
    {
        foreach (var args in twoArgDomain)
        {
            if (args.Item2 == rightArg && World.Solution[LowLevelCall(args.Item1, args.Item2)])
                yield return args.Item1;
        }
    }

    /// <summary>
    /// Returns the set of this(leftArg, rightArg) instances true in this model
    /// </summary>
    /// <param name="leftArg">First argument to this proposition</param>
    public IEnumerable<Proposition> RightRelataPropositions(string leftArg)
    {
        foreach (var args in twoArgDomain)
        {
            var prop = LowLevelCall(args.Item1, args.Item2);
            if (args.Item1 == leftArg && World.Solution[prop])
                yield return prop;
        }
    }

    /// <summary>
    /// Returns the set { leftArg | this(leftArg, rightArg) true in this model }
    /// </summary>
    /// <param name="rightArg">First argument to this proposition</param>
    public IEnumerable<Proposition> LeftRelataPropositions(string rightArg)
    {
        foreach (var args in twoArgDomain)
        {
            var prop = LowLevelCall(args.Item1, args.Item2);
            if (args.Item2 == rightArg && World.Solution[prop])
                yield return prop;
        }
    }

    /// <summary>
    /// For a given Sort s, represents the proposition: exists x in s.this(x)
    /// </summary>
    private readonly Dictionary<Sort, Proposition> existentialQuantifications = new Dictionary<Sort, Proposition>();

    public Proposition ExistentialQuantification(Sort s)
    {
        Proposition prop;
        if (existentialQuantifications.TryGetValue(s, out prop))
            return prop;
        var problem = Problem.Current;
        prop = problem.GetProposition(CatSAT.Call.FromArgs(Problem.Current, Name, s.Name));
        existentialQuantifications[s] = prop;

        foreach (var sub in s.Subsorts)
            problem.Assert(prop <= ExistentialQuantification(sub));
        foreach (var instance in s.Instances)
            if (oneArgDomain.Contains(instance))
                problem.Assert(prop <= Call(instance));

        return prop;
    }

    public static readonly Dictionary<string, Predicate> Predicates = new Dictionary<string, Predicate>();

    public static Predicate AddPredicate(string name, Sort arg1, params string[][] syntaxPattern)
    {
        return Predicates[name] = new Predicate(name, arg1, syntaxPattern);
    }

    // ReSharper disable once UnusedMember.Global
    public static void AddPredicate(string name, Sort arg1, Sort arg2, params string[][] syntaxPattern)
    {
        Predicates[name] = new Predicate(name, arg1, arg2, syntaxPattern);
    }

    public Predicate AddGeneralization(Predicate p)
    {
        generalizations.Add(p);
        return this;
    }

    public Predicate AddStrongGeneralization(Predicate p)
    {
        strongGeneralizations.Add(p);
        p.strongSpecializations.Add(this);
        return this;
    }

    public Predicate AddNegativeGeneralization(Predicate p)
    {
        negativeGeneralizations.Add(p);
        return this;
    }

    public static void MutuallyExclusive(Predicate a, Predicate b)
    {
        a.AddNegativeGeneralization(b);
        b.AddNegativeGeneralization(a);
    }

    private Predicate(string name, Sort arg1, params string[][] syntaxPatterns)
    {
        Name = name;
        arg1Sort = arg1;
        oneArgDomain = new List<string>();
        lowLevelPredicate = Language.Predicate<string>(name);
        generationPattern = syntaxPatterns[0];
        foreach (var p in syntaxPatterns)
            ParserRule.AddSyntaxRule((Func<string, Literal>)OneArgParser, p);
    }

    private Predicate(string name, Sort arg1, Sort arg2, params string[][] syntaxPatterns)
    {
        Name = name;
        arg1Sort = arg1;
        arg2Sort = arg2;
        twoArgDomain = new List<Tuple<string, string>>();
        lowLevelPredicate = Language.Predicate<string>(name);
        generationPattern = syntaxPatterns[0];
        foreach (var p in syntaxPatterns)
            ParserRule.AddSyntaxRule((Func<string, string, Literal>)TwoArgParser, p);
    }

    private Literal OneArgParser(string arg)
    {
        if (Sort.Sorts.ContainsKey(arg))
            return ExistentialQuantification(Sort.Sorts[arg]);
        return Call(arg);
    }

    private Literal TwoArgParser(string arg1, string arg2)
    {
        return Call(arg1, arg2);
    }

    public Proposition Call(string arg)
    {
        if (!Sort.IsA(arg, arg1Sort, true))
            throw new ArgumentException($"Argument to {Name}, {arg}, must be of sort {arg1Sort.Name}");
        if (!oneArgDomain.Contains(arg))
        {
            // We're being called on an arg we've never been called on before
            oneArgDomain.Add(arg);
            foreach (var g in generalizations)
                Problem.Current.Assert(LowLevelCall(arg) > g.LowLevelCall(arg));
            foreach (var g in strongGeneralizations)
            {
                Problem.Current.Assert(g.LowLevelCall(arg) <= LowLevelCall(arg));
                //UnityEngine.Debug.Log($"{g.Name}({arg}) <= {Name}({arg})");
            }
            foreach (var g in negativeGeneralizations)
                Problem.Current.Inconsistent(g.Call(arg), LowLevelCall(arg));
            foreach (var pair in existentialQuantifications)
            {
                if (pair.Key.Instances.Contains(arg))
                    Problem.Current.Assert(pair.Value <= LowLevelCall(arg));
            }

            foreach (var s in strongSpecializations)
                s.Call(arg);
        }

        return LowLevelCall(arg);
    }

    public Proposition Call(string arg1, string arg2)
    {
        if (!Sort.IsA(arg1, arg1Sort, true))
            throw new ArgumentException($"Argument to {Name}, {arg1}, must be of sort {arg1Sort.Name}");
        if (!Sort.IsA(arg2, arg1Sort, true))
            throw new ArgumentException($"Argument to {Name}, {arg2}, must be of sort {arg2Sort.Name}");

        var arg = new Tuple<string, string>(arg1, arg2);
        if (!twoArgDomain.Contains(arg))
        {
            twoArgDomain.Add(arg);
            foreach (var g in generalizations)
                Problem.Current.Assert(g.LowLevelCall(arg1, arg2) <= LowLevelCall(arg1, arg2));
        }

        return LowLevelCall(arg1, arg2);
    }

    /// <summary>
    /// Return the Proposition representing the value of the predicate on the specified argument
    /// </summary>
    private Proposition LowLevelCall(string arg)
    {
        return (Proposition)lowLevelPredicate.DynamicInvoke(arg);
    }

    /// <summary>
    /// Return the Proposition representing the value of the predicate on the specified arguments
    /// </summary>
    private Proposition LowLevelCall(string arg1, string arg2)
    {
        return (Proposition)lowLevelPredicate.DynamicInvoke(arg1, arg2);
    }

    private string UnparseMyProposition(Proposition p)
    {
        var c = (Call)p.Name;
        var arg = 0;
        var sb = new StringBuilder();
        var firstOne = true;
        var argCase = Case.Subject;
        string subject = null;
        foreach (var element in generationPattern)
        {
            string increment;
            if (element == "be")
                increment = NL.Copula(c.Args[0].ToString());
            else if (element != null)
                increment = element;
            else
            {
                string unrealized;
                if (firstOne && ImplicitSubject != null)
                    unrealized = ImplicitSubject;
                else
                    unrealized = c.Args[arg++].ToString();
                if (firstOne)
                    subject = unrealized;
                if (argCase == Case.Object && unrealized == subject)
                    increment = NL.RealizeReflexive(unrealized);
                else increment = NL.Realize(unrealized, argCase);
            }

            argCase = Case.Object;

            if (increment.EndsWith("(s)"))
            {
                var thirdPerson = subject != NL.Speaker && subject != NL.Addressee;
                increment = increment.Replace("(s)", thirdPerson ? "s" : "");
            }

            if (firstOne)
            {
                firstOne = false;
                // Capitalize it.
                increment = char.ToUpper(increment[0]) + increment.Substring(1);
            }
            else if (!increment.StartsWith("'"))
                sb.Append(' ');

            sb.Append(increment);
        }

        sb.Append(".");

        return sb.ToString();
    }

    public static bool IsPredicateInstance(Proposition p)
    {
        var c = p.Name as Call;
        return c != null && Predicates.ContainsKey(c.Name);
    }

    public static string Unparse(Proposition p)
    {
        var c = p.Name as Call;
        if (c == null)
            return p.Name.ToString();
        var predicate = Predicates[c.Name];
        if (predicate == null)
            return p.Name.ToString();
        return predicate.UnparseMyProposition(p);
    }
}