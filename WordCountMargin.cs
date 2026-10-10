using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace WordsAndCharsCount
{
    // 1. The Margin Provider (Registers the margin with Visual Studio)
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(WordCountMargin.MarginName)]
    [Order(After = PredefinedMarginNames.BottomControl)] // Places it at the very bottom of the editor
    [MarginContainer(PredefinedMarginNames.Bottom)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class WordCountMarginProvider : IWpfTextViewMarginProvider
    {
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            return new WordCountMargin(wpfTextViewHost.TextView);
        }
    }

    // 2. The Margin Control (The actual persistent UI)
    internal class WordCountMargin : Border, IWpfTextViewMargin
    {
        public const string MarginName = "WordCountMargin";
        private bool _isDisposed = false;
        private readonly IWpfTextView _textView;
        private readonly TextBlock _textBlock;

        public WordCountMargin(IWpfTextView textView)
        {
            _textView = textView;

            // Setup the UI (A small text block inside this border)
            _textBlock = new TextBlock
            {
                Foreground = Brushes.LightGray,
                Margin = new Thickness(5, 2, 5, 2),
                FontSize = 11,
                FontFamily = new FontFamily("Consolas")
            };

            // Setup the border (acting as the background)
            this.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)); // Standard VS dark theme gray
            this.BorderBrush = Brushes.Transparent;
            this.Child = _textBlock;

            // Subscribe to text changes
            _textView.TextBuffer.Changed += OnTextBufferChanged;

            // Run initial update
            UpdateWordCount();
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            UpdateWordCount();
        }

        private void UpdateWordCount()
        {
            if (_isDisposed) return;

            string text = _textView.TextBuffer.CurrentSnapshot.GetText();
            int chars = text.Length;
            int words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            // Because this is a WPF control inside the editor, it's already on the UI thread!
            _textBlock.Text = $"{words} Words | {chars} Chars";
        }

        // --- IWpfTextViewMargin Implementation ---
        public FrameworkElement VisualElement => this;
        public double MarginSize => this.ActualHeight;
        public bool Enabled => true;

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Equals(marginName, MarginName, StringComparison.OrdinalIgnoreCase) ? this : null;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _textView.TextBuffer.Changed -= OnTextBufferChanged;
            }
        }
    }
}
