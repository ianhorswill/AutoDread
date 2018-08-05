using System;
using System.Collections.Generic;
using PicoSAT;

public class Predicate
{
    /// <summary>
    /// Name of the predicate, for debugging purposes
    /// </summary>
    public readonly string Name;
    /// <summary>
    /// Sort of the predicate's first argument
    /// </summary>
    private readonly Sort arg1Sort;
    /// <summary>
    /// Sort of the predicate's second argument, or null if this is a unary predicate
    /// </summary>
    private readonly Sort arg2Sort;
    /// <summary>
    /// The predicate object actually used by PicoSAT
    /// </summary>
    private readonly Delegate lowLevelPredicate;

    /// <summary>
    /// Predicates that generalize this predicate
    /// </summary>
    private readonly List<Predicate> generalizations = new List<Predicate>();
    private readonly List<Predicate> strongGeneralizations = new List<Predicate>();


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
        prop = problem.GetProposition(new Call(Name, s.Name));
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
                Problem.Current.Assert((Expression)LowLevelCall(arg) >= g.LowLevelCall(arg));
            foreach (var g in strongGeneralizations)
            {
                Problem.Current.Assert(g.LowLevelCall(arg) <= LowLevelCall(arg));
                UnityEngine.Debug.Log($"{g.Name}({arg}) <= {Name}({arg})");
            }
            foreach (var g in negativeGeneralizations)
                Problem.Current.Inconsistent(g.Call(arg), LowLevelCall(arg));
            foreach (var pair in existentialQuantifications)
            {
                if (pair.Key.Instances.Contains(arg))
                    Problem.Current.Assert(pair.Value <= LowLevelCall(arg));
            }
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
    /// Return the Proposiiton representing the value of the predicate on the specified argument
    /// </summary>
    private Proposition LowLevelCall(string arg)
    {
        return (Proposition)lowLevelPredicate.DynamicInvoke(arg);
    }

    /// <summary>
    /// Return the Proposiiton representing the value of the predicate on the specified arguments
    /// </summary>
    private Proposition LowLevelCall(string arg1, string arg2)
    {
        return (Proposition)lowLevelPredicate.DynamicInvoke(arg1, arg2);
    }

    public static void Initialize(World world)
    {
        Problem.Current = world.Problem; // Just to be paranoid

        var exists = AddPredicate("exists", Sort.Person, new[] {null, "exists"});

        var likes = AddPredicate("likes", Sort.Entity,
            new[] {"likes", null});

        var dislikes = AddPredicate("dislikes", Sort.Entity,
            new[] {"dislikes", null});

        MutuallyExclusive(likes, dislikes);

        var loves = AddPredicate("loves", Sort.Person,
            new[] {"loves", null}).AddGeneralization(likes).AddGeneralization(exists);

        var hates = AddPredicate("hates", Sort.Person,
            new[] {"hates", null}).AddGeneralization(dislikes).AddGeneralization(exists);

        AddPredicate("alcoholic", Sort.Person,
            new[] {"alcoholic", null}).AddGeneralization(exists);

        MutuallyExclusive(loves, hates);

        AddPredicate("abusive", Sort.Person,
            new[] {"abusive", null}).AddGeneralization(hates);
        
        AddPredicate("tastes", Sort.Entity,
            new[] {null, "tastes"});

        var living = AddPredicate("living", Sort.Person,
            new[] {null, "is", "alive"},
            new[] {"living", null}).AddStrongGeneralization(exists);

        var dead = AddPredicate("dead", Sort.Person,
            new[] {null, "is", "dead"},
            new[] {null, "dead"},
            new[] {"dead", null}).AddStrongGeneralization(exists);

        MutuallyExclusive(living, dead);
    }

}