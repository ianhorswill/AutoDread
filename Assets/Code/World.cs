using System.Collections.Generic;
using System.Linq;
using System.Text;
using CatSAT;
using UnityEngine;

/// <summary>
/// Holds the story world state, as presented by a CatSAT Problem.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class World : MonoBehaviour {
    /// <summary>
    /// SAT problem holding the various accumulated implications in the story world.
    /// </summary>
    public readonly Problem Problem = new Problem("World");

    public Solution Solution;

    // ReSharper disable once UnusedMember.Local
    void Start () {
        // Just to be sure
        Problem.Current = Problem;
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
            if (summary != null)
                return summary;
            var b = new StringBuilder();
            var remainingTruths = new List<Proposition>(truths);
            var existences = PredicateTruths(Predicate.Exists);
            foreach (var e in existences)
            {
                var who = e.Arg<string>(0);

                if (who != "self")
                {
                    var facts = PredicatesAbout(who);

                    if (facts.Length > 1)
                    {
                        b.AppendFormat("\n<b>About your {0}: </b>", who);
                        remainingTruths.Remove(e);

                        foreach (var p in facts)
                        {
                            if (p.IsCall("exists"))
                                continue;
                            b.Append(Predicate.Unparse(p));
                            b.Append(", ");
                            remainingTruths.Remove(p);
                        }
                    }
                    else b.Append($"\n<b>You have a {who}</b>");
                }
            }

            if (remainingTruths.Count > 0)
            {
                b.Append("\n<b>Other:</b> \n");

                foreach (var p in remainingTruths)
                {
                    b.Append(Predicate.Unparse(p));
                    b.Append(", ");
                }

            }

            return summary = b.ToString();
        }
    }
}
