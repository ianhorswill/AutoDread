using System.Collections;
using System.Collections.Generic;
using System.Text;
using CatSAT;
using UnityEngine;
using UnityEngine.UI;

public class QuestionDriver : MonoBehaviour
{
    public GameObject Display, AnswerPrefab;

    private readonly List<Literal> _implications = new List<Literal>();
    private readonly List<Quip> _potentialAnswers = new List<Quip>();

    private Questionnaire _questionnaire;
    private World _world;
    private Question _current;

    private IEnumerator Start () {
        _questionnaire = GetComponent<Questionnaire>();
        _world = GetComponent<World>();
        yield return null;
        NextQuestion();
        SetImplicationsText();
        SetResultsText();
    }

    private void OnGUI () {
        if (Event.current.type == EventType.KeyDown) {
            if (_current == null) { return; }

            AnswerQuestion(-1 + Event.current.keyCode - KeyCode.Alpha0);
        }

//        else if (_current == null) {
//            GUILayout.Label("");
//            GUILayout.Label("");
//            GUILayout.Label($"A possible you:\n{_world.Summary}", GUILayout.Width(Screen.width));
//        }
    }

    private void AnswerQuestion (int i) {
        if (i < 0 || i >= _potentialAnswers.Count) { return; }

        foreach (var l in _current.Implications) { AddImplication(l); }

        foreach (var l in _potentialAnswers[i].Implications) { AddImplication(l); }

        NextQuestion();
    }


    private readonly StringBuilder _sb = new StringBuilder();

    private void AddImplication (Literal implication) {
        if (_implications.Contains(implication)) { return; }

        _implications.Add(implication);
        _sb.Append(_sb.Length == 0 ? "Implications: " : ", ");
        _sb.Append(implication);

        SetImplicationsText();
    }


    private int _questionNumber = -1;

    // TODO: randomize the order of the quetsions
    // TODO: end after we have "enough" implications
    /// <summary>
    /// Move on to the next question that's consistent with Implications
    /// </summary>
    private void NextQuestion () {
        _potentialAnswers.Clear();
        while (_potentialAnswers.Count < 2 && _questionNumber < _questionnaire.Questions.Count - 1) {
            _potentialAnswers.Clear();
            _questionNumber++;
            if (_questionNumber >= _questionnaire.Questions.Count) { continue; }

            _current = _questionnaire.Questions[_questionNumber];
            foreach (var a in _questionnaire.Questions[_questionNumber].Answers) {
                if (_world.IsConsistent(_implications, _current.Implications, a.Implications)) {
                    _potentialAnswers.Add(a);
                }
            }
        }

        if (_potentialAnswers.Count < 2 || _questionNumber == _questionnaire.Questions.Count) {
            _current = null;
            _world.SetWorld(_implications);
            SetResultsText();
        }

        SetQuestionText();
        SetAnswersText();
    }

    //
    // Refresh the UI.

    private void SetQuestionText () {
        Display.transform.Find("Question/Question").GetComponent<Text>().text = _current?.Text;
    }

    private void SetAnswersText () {
        var answers = Display.transform.Find("Question/Answers");
        for (int i = 0, c = answers.childCount; i < c; i++) {
            Destroy(answers.GetChild(i).gameObject); // Kill 'em all.
        }

        for (int i = 0, c = _potentialAnswers.Count; i < c; i++) {
            var answer = _potentialAnswers[i];
            var text = Instantiate(AnswerPrefab, answers).GetComponent<Text>();
            text.text = $"{i + 1}: {answer.Text}";
        }
    }

    private void SetImplicationsText () {
        var text = Display.transform.Find("Summary/Implications/Text").GetComponent<Text>();
        text.text = _sb.ToString();
    }

    private void SetResultsText () {
        var text = Display.transform.Find("Summary/Possible You/Text").GetComponent<Text>();
        text.text = _current == null ? _world.Summary : null;
    }
}