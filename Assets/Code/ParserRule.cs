using System;
using System.Collections.Generic;
using PicoSAT;

public class ParserRule
{
    public static void AddSyntaxRule(Delegate predicate, params string[] words)
    {
        Rules.Add(new ParserRule(predicate, words));
    }

    public static readonly List<ParserRule> Rules = new List<ParserRule>();

    private string[] pattern;
    Delegate predicate;

    public ParserRule(Delegate pred, params string[] words)
    {
        predicate = pred;
        pattern = words;
    }

    public bool Match(string[] words)
    {
        if (words.Length != pattern.Length)
            return false;
        for (int i = 0; i < pattern.Length; i++)
            if (pattern[i] != null && pattern[i] != words[i])
                return false;

        return true;
    }

    public Literal Parse(string[] words)
    {
        string arg1 = null, arg2 = null;

        for (int i = 0; i < pattern.Length; i++)
            if (pattern[i] == null)
            {
                if (arg1 == null)
                    arg1 = words[i];
                else
                    arg2 = words[i];
            }

        if (arg2 == null)
            return (Literal) predicate.DynamicInvoke(arg1);
        return (Literal) predicate.DynamicInvoke(arg1, arg2);
    }
}