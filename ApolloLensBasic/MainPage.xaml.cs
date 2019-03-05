using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ApolloLensLibrary.Conducting;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensBasic
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Wrapper Wrapper { get; }
        //private Org.WebRtc.MediaVideoTrack MediaVideoTrack { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            this.Wrapper = Wrapper.Instance;
            //this.Wrapper.DestoyingMediaStream += this.Wrapper_DestoyingMediaStream;
        }        

        private async void InitializeButton_Click(object sender, RoutedEventArgs e)
        {
            await this.Wrapper.Initialize(this.Dispatcher);
        }

        private async void GetMediaStreamButton_Click(object sender, RoutedEventArgs e)
        {
            await this.Wrapper.PrepareUserMediaStream();
        }

        private async void SetToHighest_Click(object sender, RoutedEventArgs e)
        {
            await this.Wrapper.SetToHighestBitrate();
        }

        private async void SetToLowest_Click(object sender, RoutedEventArgs e)
        {
            await this.Wrapper.SetToLowestBitrate();
        }

        private async void DestroyMedia_Click(object sender, RoutedEventArgs e)
        {
            await this.Wrapper.DestroyUserMediaStream();
        }

        private async void ShowVideo_Click(object sender, RoutedEventArgs e)
        {
            await this.Wrapper.BindLocalVideo(this.LocalVideo);
        }

        private async void HideVideo_Click(object sender, RoutedEventArgs e)
        {
            await this.Wrapper.DetachLocalVideo();
        }
    }
}
