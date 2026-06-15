using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace StorageHelper.Behaviors
{
    internal class NumericBoxBehavior : Behavior<TextBox>
    {
        public string Fallback { get; set; } = "0";

        protected override void OnAttached()
        {
            AssociatedObject.LostFocus += OnLostFocus;
            AssociatedObject.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(AssociatedObject, OnPaste);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.LostFocus -= OnLostFocus;
            AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
            DataObject.RemovePastingHandler(AssociatedObject, OnPaste);
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var text = e.DataObject.GetData(DataFormats.Text) as string;
            if (string.IsNullOrEmpty(text) || !int.TryParse(text, out var v) || v < 0)
                e.CancelCommand();
        }

        private void OnPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out var value) || value < 0)
                e.Handled = true;
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AssociatedObject.Text)) 
                AssociatedObject.SetCurrentValue(TextBox.TextProperty, Fallback);
        }
    }
}
