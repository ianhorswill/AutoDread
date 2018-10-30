using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CatSAT;
using CatSAT.NonBoolean.SMT.MenuVariables;
using UnityEngine;

/// <summary>
/// Holds the story world state, as presented by a CatSAT Problem.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class World : MonoBehaviour {
    /// <summary>
    /// SAT problem holding the various accumulated implications in the story world.
    /// </summary>
    public static readonly Problem Problem = new Problem("World");

    public static Solution Solution;

    public MenuVariable<string> FirstName;
    public MenuVariable<string> LastName;
    public FloatVariable Age;
    public Proposition Male;
    public Proposition Female;

    public void Initialize() {
        // Just to be sure
        Problem.Current = Problem;
        Age = new FloatVariable("age", 15, 60);
        Male = "male";
        Female = "female";
        Problem.Unique(Male, Female);
        LastName = new MenuVariable<string>("surname", MenuFromResource("surnames", "LastNames"), Problem);
        FirstName = Predicate.NameOf("self");
    }

    public static Menu<string> MenuFromResource(string menuName, string resourceName)
    {
        return new Menu<string>(menuName, Resources.Load<TextAsset>(resourceName).text.Split(new char[0], StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// True if all the various assumptions are mutually consistent, i.e. if they have a model.
    /// </summary>
    public bool IsConsistent(params IEnumerable<Literal>[] assumptions)
    {
        Problem.ResetPropositions();
        foreach (var assumptionset in assumptions)
        foreach (var assumption in assumptionset)
            if (Problem.IsConstant(assumption))
            {
                // That proposition has already been determined
                if (assumption is Proposition != Problem[assumption])
                    // We have contradictory assumptions
                    return false;
            }
            else Problem[assumption] = true;
        return Problem.Solve(false) != null;
    }

    public void SetWorld(IEnumerable<Literal> assumptions)
    {
        Problem.ResetPropositions();
        foreach (var a in assumptions)
            Problem[a] = true;
        Problem.Optimize();
        Solution = Problem.Solve();
        truths = Problem.Propositions.Where(p => Solution[p]).ToArray();
        summary = null;
    }

    private string summary;
    private Proposition[] truths;

    Proposition[] PredicateTruths(Predicate p)
    {
        return truths.Where(prop => prop.IsCall(p.Name)).ToArray();
    }

    Proposition[] PredicatesAbout(string entity)
    {
        return truths.Where(p =>
            {
                var c = p.Name as Call;
                return c != null && c.Args.Length == 1 && c.Args[0].Equals(entity);
            }
        ).ToArray();
    }

    public string Summary
    {
        get
        {
            NL.Addressee = "self";
            if (summary != null)
                return summary;
            var b = new StringBuilder();
            var remainingTruths = new List<Proposition>(truths);
            var existences = PredicateTruths(Predicate.Exists);
            b.Append($"<b>You are {FirstName.Value(Solution)} {LastName.Value(Solution)}</b>, age {(int) (Age.Value(Solution))}\n");
            foreach (var e in existences)
            {
                var who = e.Arg<string>(0);

                if (who != "self")
                {
                    var whoName = Predicate.NameOf(who).Value(Solution);
                    var isFirst = true;

                    NL.Topic = who;

                    var facts = PredicatesAbout(who);
                    foreach (var f in facts)
                        remainingTruths.Remove(f);

                    var relevantFacts = facts.Where(f => f.IsNonQuietPredicate()).ToArray();

                    if (relevantFacts.Length > 0)
                    {
                        b.AppendFormat("\n<b>About your {0}, {1}: </b>", who, whoName);

                        foreach (var p in relevantFacts)
                        {
                            if (isFirst)
                                isFirst = false;
                            else
                                b.Append("  ");
                            b.Append(Predicate.Unparse(p));
                        }
                    }
                    else b.Append($"\n<b>You have a {who}, {whoName}</b>");
                }
            }

            if (remainingTruths.Count > 0)
            {
                b.Append("\n<b>Other:</b> \n");

                foreach (var p in remainingTruths)
                {
                    if (!Predicate.IsPredicateInstance(p) || p.IsQuietPredicate() || p.ArgIsSort())
                        continue;
                    b.Append(Predicate.Unparse(p));
                    b.Append("  ");
                }

            }

            return summary = b.ToString();
        }
    }
}
