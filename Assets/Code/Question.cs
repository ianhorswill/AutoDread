using System.Diagnostics;
using PicoSAT;

/// <summary>
/// A question for the plater, its possible answers, and their implications
/// </summary>
[DebuggerDisplay("Question {" + nameof(Text) + "}")]
public class Question : Quip
{
    /// <summary>
    /// Potential answers to the question
    /// </summary>
    public readonly Quip[] Answers;

    public Question(string text, Literal[] implications, Quip[] answers) : base(text, implications)
    {
        Answers = answers;
    }
}
