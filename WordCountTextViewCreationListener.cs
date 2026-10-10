using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace WordsAndCharsCount
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class WordCountTextViewCreationListener : IWpfTextViewCreationListener
    {
        private static TextBlock _statusBarTextBlock;
        private static StatusBar _vsStatusBar;
        
        public void TextViewCreated(IWpfTextView textView)
        {
            EnsureStatusBarItemExists();

            // When the text changes, only update if THIS specific tab is the one being focused/typed in
            textView.TextBuffer.Changed += (s, e) =>
            {
                if (textView.HasAggregateFocus)
                    UpdateCount(textView.TextBuffer.CurrentSnapshot);
            };

            // When the user switches to this tab, update the status bar
            textView.GotAggregateFocus += (s, e) =>
            {
                UpdateCount(textView.TextBuffer.CurrentSnapshot);
            };
        }

        private void UpdateCount(ITextSnapshot snapshot)
        {
            if (_statusBarTextBlock == null) return;
            
            string text = snapshot.GetText();
            int chars = text.Length;
            int words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            _statusBarTextBlock.Text = $"{words} Words | {chars} Chars";
        }

        private void EnsureStatusBarItemExists()
        {
            if (_statusBarTextBlock != null) return;

            // Run this safely on the WPF UI thread
            Application.Current.Dispatcher.InvokeAsync
            (
                () =>
                {
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow == null) return;

                    // Hack: Walk the WPF visual tree to find the literal Visual Studio status bar control
                    _vsStatusBar = FindVisualChild<StatusBar>(mainWindow);
                    if (_vsStatusBar == null) return;

                    // Create our custom status bar text block
                    _statusBarTextBlock = new TextBlock
                    {
                        Foreground = Brushes.LightGray,
                        Margin = new Thickness(10, 0, 10, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        FontFamily = new FontFamily("Consolas")
                    };

                    // Wrap it in a StatusBarItem so it aligns correctly
                    var item = new StatusBarItem
                    {
                        Content = _statusBarTextBlock,
                        Margin = new Thickness(0)
                    };

                    // Inject it directly into the Visual Studio status bar!
                    // Index 1 places it right after the transient "Ready" text, but before the Git controls
                    _vsStatusBar.Items.Insert(0, item);
                }
            );
        }

        // Helper method to recursively search the WPF visual tree
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
