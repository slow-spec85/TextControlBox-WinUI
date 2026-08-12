using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.ComponentModel;

namespace TextControlBoxNS.Controls;

/// <summary>
/// Infrastructure text input control used by <c>TextControlBox</c>.
/// </summary>
/// <remarks>
/// This type is public because the Windows App SDK XAML compiler must activate custom
/// controls through public metadata. It is not intended to be used directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class InputHandlerControl : TextBox
{
    /// <summary>
    /// Represents a handler for text entered through the infrastructure input control.
    /// </summary>
    /// <param name="sender">The input control.</param>
    /// <param name="e">The text change event data.</param>
    public delegate void TextEnteredEvent(object sender, TextChangedEventArgs e);

    /// <summary>
    /// Occurs when user-entered text is available.
    /// </summary>
    public event TextEnteredEvent TextEntered;

    private bool _isProgrammaticChange;

    /// <summary>
    /// Initializes the infrastructure input control.
    /// </summary>
    public InputHandlerControl()
    {
        TextChanged += InputHandlerControl_TextChanged;
    }

    private void InputHandlerControl_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (base.IsReadOnly || base.Text.Length == 0)
            return;

        if (!_isProgrammaticChange)
            TextEntered?.Invoke(this, e);
    }

    /// <summary>
    /// Gets or sets the input buffer without raising <see cref="TextEntered"/> for
    /// programmatic changes.
    /// </summary>
    public new string Text
    {
        get => base.Text;
        set
        {
            _isProgrammaticChange = true;
            base.Text = value;
            _isProgrammaticChange = false;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        // Override the default TextBox key handling; the editor handles keys itself.
    }
}
