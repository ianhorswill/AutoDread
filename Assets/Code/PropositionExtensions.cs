using CatSAT;

using PredicateClass = Predicate;

public static class PropositionExtensions
{
    public static Call Call(this Proposition prop)
    {
        return (Call) prop.Name;
    }

    public static bool IsCall(this Proposition p)
    {
        return p.Name is Call;
    }

    public static bool IsNonQuietPredicate(this Proposition p)
    {
        var c = p.Name as Call;
        if (c == null)
            return false;
        PredicateClass pred;
        if (PredicateClass.Predicates.TryGetValue(c.Name, out pred))
            return !pred.Quiet;
        return false;
    }

    public static bool ArgIsSort(this Proposition p)
    {
        var c = p.Name as Call;
        if (c == null)
            return false;
        return c.Args[0] is Sort;
    }

    public static bool IsQuietPredicate(this Proposition p)
    {
        var c = p.Name as Call;
        if (c == null)
            return false;
        PredicateClass pred;
        if (PredicateClass.Predicates.TryGetValue(c.Name, out pred))
            return pred.Quiet;
        return false;
    }

    public static bool IsCall(this Proposition p, string name)
    {
        var c = p.Name as Call;
        return c != null && c.Name == name;
    }

    public static PredicateClass Predicate(this Proposition prop)
    {
        return PredicateClass.Predicates[prop.Call().Name];
    }

    public static string LeftArg(this Proposition prop)
    {
        return (string) prop.Call().Args[0];
    }

    public static string RightArg(this Proposition prop)
    {
        return (string) prop.Call().Args[1];
    }
}
