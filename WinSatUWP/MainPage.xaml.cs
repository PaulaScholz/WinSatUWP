//***********************************************************************
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//**********************************************************************​

using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.AppService;
using GalaSoft.MvvmLight.Messaging;
using WinSatUWP.Messages;
using WinSatModels;
using System;
using Newtonsoft.Json;
using Windows.UI.Core;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.CompilerServices;
using Windows.UI.Popups;
using Windows.ApplicationModel.Resources;

namespace WinSatUWP
{
    /// <summary>
    /// Note that we are not using a ViewModel in this simple example, rather, the MainPage
    /// itself supports property change notifications.
    /// </summary>
    public partial class MainPage : Page, INotifyPropertyChanged
    {
        //properties for the UI to bind to
        public ObservableCollection<WinSATAssessmentInfo> Assessments { get; set; }

        private DateTime _assessmentTime = DateTime.MinValue;
        /// <summary>
        /// The Date of the latest WinSAT assessment.
        /// </summary>
        public DateTime AssessmentTime
        {
            get { return _assessmentTime; }
            set { Set(ref _assessmentTime, value); }
        }

        private string _ratingState;
        /// <summary>
        /// The "Rating State", always "Windows Experience Index".  Not currently displayed.
        /// </summary>
        public string RatingState
        {
            get { return _ratingState; }
            set { Set(ref _ratingState, value); }
        }

        static bool bAssessmentAlreadyMade = false;

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(800, 400);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            // hook up the Loaded event handler
            Loaded += MainPage_Loaded;

            Assessments = new ObservableCollection<WinSATAssessmentInfo>();
        }

        /// <summary>
        /// Loaded is fired after OnNavigatedTo, launch the full trust WinSatInfo Win32 process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // get assessments from the Win32 application.  When launched, it will get the current assessment from WinSAT
            // and send it back to us through our Connection_RequestReceived event handler.
            await Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Property setter for UI-bound values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        protected void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            RaisePropertyChanged(propertyName);
        }

        /// <summary>
        /// Runs before Loaded event handler.  Registers a subscription to the ConnectionReadyMessage thorugh
        /// the GalaSoft MVVMLight Messenger object and sets up an anonymous method to connect our Connection_RequestReceived
        /// handler to the appplication's AppServiceConnection's RequestReceived endpoint.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Messenger.Default.Register<ConnectionReadyMessage>(this, message =>
            {
                if (App.Connection != null)
                {
                    App.Connection.RequestReceived += Connection_RequestReceived;
                }
            });
        }

        private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var deferral = args.GetDeferral();

            // get the verb or "command" for this request
            string verb = (string)args.Request.Message["verb"];

            switch (verb)
            {
                case "assessmentResults":
                    {
                        // This List was sent as JSON serialized string by the Win32 app
                        List<WinSATAssessmentInfo> receivedAssessments = JsonConvert.DeserializeObject<List<WinSATAssessmentInfo>>((string)args.Request.Message["assessments"]);

                        float baseScore = (float)args.Request.Message["basescore"];

                        // Update UI-bound collections and controls on the UI thread
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            // clear the UI-bound collection
                            Assessments.Clear();

                            // adjust the UI
                            WEIPanel.Visibility = Visibility.Visible;
                            winSatProgressRing.IsActive = false;
                            ProgressBox.Visibility = Visibility.Collapsed;

                            foreach (WinSATAssessmentInfo info in receivedAssessments)
                            {
                                // add the received assessments to the UI bound collection
                                Assessments.Add(info);
                            }

                            // populate public UI-bound properties
                            RatingState = (string)args.Request.Message["ratingState"];
                            AssessmentTime = DateTime.Parse((string)args.Request.Message["assessmentTime"]);

                            // select the lowest subscore in the datagrid
                            WEIGrid.SelectedIndex = FindLowestSubscoreIndex();
                        });

                        break;
                    }

                case "getimageResults":
                    {
                        byte[] imageBytes = (byte[])args.Request.Message["imagebytes"];

                        // update the rating image on the UI thread
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        async () =>
                        {
                            using (InMemoryRandomAccessStream memStream = new InMemoryRandomAccessStream())
                            {
                                // the AsBuffer extension is in System.Runtime.InteropServices.WindowsRuntime
                                await memStream.WriteAsync(imageBytes.AsBuffer());

                                memStream.Seek(0);

                                BitmapImage rating = new BitmapImage();

                                // the bytes sent to us by WinSatInfo and now inside memStream represent a PNG file
                                await rating.SetSourceAsync(memStream);

                                // set the UI Image control's source to the BitmapImage
                                RatingImage.Source = rating;
                            }
                        });

                        break;
                    }
            }

            // if you want to respond, this is how you do it
            //ValueSet valueSet = new ValueSet();
            //valueSet.Add("response", "success");
            //await args.Request.SendResponseAsync(valueSet);

            deferral.Complete();
        }

        /// <summary>
        /// Find the index of the lowest subscore in the Assessments collection.
        /// </summary>
        /// <returns></returns>
        private int FindLowestSubscoreIndex()
        {
            float currentLowestScore = float.MaxValue;

            int lowestIndex = 0;

            // Determine the lowest subscore index, to be selected on WEIGrid.
            // There could be more than one category that shares the lowest score, but we don't care in this demo.
            for (int i = 0; i < Assessments.Count; i++)
            {
                if (Assessments[i].Score < currentLowestScore)
                {
                    currentLowestScore = Assessments[i].Score;
                    lowestIndex = i;
                }
            }

            return lowestIndex;
        }

        /// <summary>
        /// Re-run the assessment.  Only once per run of the app.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            // Only one new assessment per run of the program allowed.
            if(!bAssessmentAlreadyMade)
            {
                // no need to open a connection, if we got this far we have one
                ValueSet valueSet = new ValueSet();

                // program that receives this valueset will switch on the value of the verb
                valueSet.Add("verb", "formalWinSatRequest");

                AppServiceResponse response = null;

                // send the command and wait for a response
                response = await App.Connection.SendMessageAsync(valueSet);

                // if the command is a success, get the new assessment results
                if (response?.Status == AppServiceResponseStatus.Success)
                {
                    int exitCode = (int)response.Message["exitcode"];

                    // we're done with the new assessmet, refresh the interface by telling
                    // our WinSatInfo program to get the assessment, which will refresh the UI
                    if (0 == exitCode)
                    {
                        // we've made a good assessment, set our flag so we don't 
                        // do this again this run.
                        bAssessmentAlreadyMade = true;

                        valueSet.Clear();
                        valueSet.Add("verb", "assessmentRequest");

                        // don't need a response, just send the assessmentRequest command
                        await App.Connection.SendMessageAsync(valueSet);
                    }
                }
            }
            else
            {
                // this is how you load strings from Resources for use in UI code-behind
                ResourceLoader loader = ResourceLoader.GetForCurrentView();
                var oneAssessmentMessage = loader.GetString("OneAssessmentMessage");

                var dialog = new MessageDialog(oneAssessmentMessage);
                await dialog.ShowAsync();
            }


            
        }
    }
}
