using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class NL
{
    public static string Speaker;
    public static string Addressee;
    public static string Topic;

    public static string Realize(string o, Case c)
    {
        if (o == Speaker)
            return Caseify(c, "I", "me");
        if (o == Addressee)
            return "you";
        if (o == Topic)
            return World.Solution[Predicate.Male.Call(o)] ? Caseify(c, "he", "him") : Caseify(c, "she", "her");
        return o;
    }
    
    public static string Caseify(Case c, string subject, string obj)
    {
        return c == Case.Subject ? subject : obj;
    }

    public static string Copula(string subject)
    {
        if (subject == Speaker)
            return Contract(subject, "am");
        if (subject == Addressee)
            return Contract(subject, "are");
        return Contract(subject, "is");
    }

    public static string Contract(string subject, string verb)
    {
        string realization = Realize(subject, Case.Subject);
        var lastChar = realization[realization.Length - 1];
        if ("aeiou".Contains(lastChar))
            // Contract
            return "'" + verb.Substring(1);
        return verb;
    }

    public static string RealizeReflexive(string unrealized)
    {
        if (unrealized == Speaker)
            return "myself";
        if (unrealized == Addressee)
            return "yourself";
        if (Sort.IsA(unrealized, Sort.Person))
            return World.Solution[Predicate.Male.Call(unrealized)] ? "himself" : "herself";
        return "itself";
    }
}

public enum Case
{
    Subject,
    Object
};
