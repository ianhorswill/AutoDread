using System.Collections.Generic;
using PicoSAT;
using UnityEngine;

/// <summary>
/// Holds the story world state, as presented by a PicoSAT Problem.
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
        Solution = Problem.Solve();
    }
}
