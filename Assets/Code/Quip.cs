using System.Diagnostics;
using PicoSAT;

/// <summary>
/// A text string packaged with some logical implications
/// </summary>
[DebuggerDisplay("Quip {" + nameof(Text) + "}")]
public class Quip
{
    /// <summary>
    /// Text to display
    /// </summary>
    public readonly string Text;
    /// <summary>
    /// Literals that must be true in order for this quip to be valid
    /// </summary>
    public readonly Literal[] Implications;

    public Quip(string text, Literal[] implications)
    {
        Text = text;
        Implications = implications;
    }
}
