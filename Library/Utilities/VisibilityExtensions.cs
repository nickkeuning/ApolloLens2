using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace ApolloLensLibrary.Utilities
{
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
