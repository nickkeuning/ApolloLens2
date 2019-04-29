using Windows.UI.Xaml;

namespace ApolloLensLibrary.Utilities
{
    /// <summary>
    /// Simplifies showing and hiding UI elements.
    /// </summary>
    public static class VisibilityExtensions
    {
        public static void Hide(this FrameworkElement element)
        {
            element.Visibility = Visibility.Collapsed;
        }

        public static void Show(this FrameworkElement element)
        {
            element.Visibility = Visibility.Visible;
        }
    }
}
