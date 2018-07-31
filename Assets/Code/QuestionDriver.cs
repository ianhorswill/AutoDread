using System.Collections;
using System.Collections.Generic;
using System.Text;
using PicoSAT;
using UnityEngine;

/// <summary>
/// Asks the users Questions from the Questionnaire.
/// </summary>
// ReSharper disable once UnusedMember.Global
public class QuestionDriver : MonoBehaviour
{
    /// <summary>
    /// The game's Questionnaire component
    /// </summary>
    private Questionnaire questionnaire;
    /// <summary>
    /// The game's World component
    /// </summary>
    private World world;

    /// <summary>
    /// The implications of the questions answered so far.
    /// </summary>
    public readonly List<Literal> Implications = new List<Literal>();

    /// <summary>
    /// The question currently being asked, if any
    /// </summary>
    private Question current;
    /// <summary>
    /// The answers to the current question that are consistent with Implications
    /// </summary>
    private readonly List<Quip> potentialAnswers = new List<Quip>();

    public IEnumerator Start ()
	{
	    questionnaire = GetComponent<Questionnaire>();
	    world = GetComponent<World>();
	    yield return null;
        NextQuestion();
	}

    /// <summary>
    /// Display the current question and implications, if any.
    /// </summary>
    public void OnGUI()
    {
        switch (Event.current.type)
        {
            case EventType.KeyDown:
                if (current == null)
                    return;
                AnswerQuestion(-1 + Event.current.keyCode - KeyCode.Alpha0);
                break;

            default:
                if (current != null)
                {
                    GUILayout.Label($"<b><i>{current.Text}</i></b>", GUILayout.Width(Screen.width));
                    for (int i = 0; i < potentialAnswers.Count; i++)
                    {
                        GUILayout.Label($"{i + 1}: {potentialAnswers[i].Text}", GUILayout.Width(Screen.width));
                    }
                }

                if (implicationString != null)
                {
                    GUILayout.Label("");
                    GUILayout.Label("");
                    GUILayout.Label(implicationString, GUILayout.Width(Screen.width));
                }

                break;
        }
    }

    /// <summary>
    /// Pick answer number i.
    /// </summary>
    private void AnswerQuestion(int i)
    {
        if (i < 0 || i >= potentialAnswers.Count)
            return;

        foreach (var l in current.Implications)
            AddImplcation(l);
        foreach (var l in potentialAnswers[i].Implications)
            AddImplcation(l);

        implicationString = sb.ToString();

        NextQuestion();
    }

    /// <summary>
    /// Cached string of the comma-separated list of all the items in Implciations
    /// </summary>
    private string implicationString;
    /// <summary>
    /// StringBuilder used to build implicationString
    /// </summary>
    private readonly StringBuilder sb = new StringBuilder();

    /// <summary>
    /// Add an implication to Implications
    /// </summary>
    private void AddImplcation(Literal implication)
    {
        if (Implications.Contains(implication))
            return;

        Implications.Add(implication);
        sb.Append(sb.Length==0 ? "<b>Implications:</b> " : ", ");
        sb.Append(implication);
    }

    private int questionNumber = -1;
    /// <summary>
    /// Move on to the next question that's consistent with Implications
    /// </summary>
    private void NextQuestion()
    {
        // TODO: randomize the order of the quetsions
        // TODO: end after we have "enough" implications
        potentialAnswers.Clear();
        while (potentialAnswers.Count < 2 && questionNumber < questionnaire.Questions.Count - 1)
        {
            potentialAnswers.Clear();
            questionNumber++;
            if (questionNumber < questionnaire.Questions.Count)
            {
                current = questionnaire.Questions[questionNumber];
                foreach (var a in questionnaire.Questions[questionNumber].Answers)
                {
                    if (world.IsConsistent(Implications, current.Implications, a.Implications))
                        potentialAnswers.Add(a);
                }

            }
        }

        if (potentialAnswers.Count < 2 || questionNumber == questionnaire.Questions.Count)
            current = null;
    }
}
