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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Newtonsoft.Json;
using WinSatModels;
using WINSATLib;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

/// <summary>
/// Win32 imported functions.
/// </summary>
class API
{
    [DllImport("GDI32.dll")] public static extern int DeleteObject(IntPtr hObject);
}

namespace WinSatInfo
{
    /// <summary>
    /// This is the Win32 application used to connect to the WinSAT COM interface and report 
    /// WinSAT assessment information back to the UWP program through an AppServiceConnection
    /// using a ValueSet.
    /// 
    /// This application is started by the Windows.ApplicationModel.FullTrustProcessLauncher in 
    /// the UWP application.
    /// </summary>
    class Program
    {
     
        // the AppServiceConnection to our UWP app
        private static AppServiceConnection connection = new AppServiceConnection();

        // HRESULT 80004005 is E_FAIL
        const int E_FAIL = unchecked((int)0x80004005);

        /// <summary>
        /// Entry point of the demo WinSatInfo application.
        /// </summary>
        /// <remarks>A single-threaded apartment state is required for COM interop. Don't remove the [STAThread] attribute.</remarks>
        [STAThread]
        static void Main(string[] args)
        {
            // To debug this app, you'll need to have it started in console mode.  Uncomment 
            // the lines below and then right-click on the project file to get to project settings.
            // Select the Application tab and change the Output Type from Windows Application to 
            // Console Application.  A "Windows Application" is simply a headless console app.

            //Console.WriteLine("Detatch your debugger from the UWP app and attach it to WinSatInfo.");
            //Console.WriteLine("Set your breakpoint in WinSatInfo and then press Enter to continue.");
            //Console.ReadLine();

            // The AppServiceName must match the name declared in the WinSatPackage packaging project's Package.appxmanifest file.
            // You'll have to view it as code to see the XML.  It will look like this:
            //
            //       <Extensions>
            //           <uap:Extension Category="windows.appService">
            //              <uap:AppService Name="CommunicationService" />
            //          </uap:Extension>
            //          <desktop:Extension Category="windows.fullTrustProcess" Executable="WinSatInfo\WinSatInfo.exe" />
            //       </Extensions>
            //
            connection.AppServiceName = "CommunicationService";
            connection.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;

            // hook up the connection event handlers
            connection.ServiceClosed += Connection_ServiceClosed;
            connection.RequestReceived += Connection_RequestReceived;

            AppServiceConnectionStatus result = AppServiceConnectionStatus.Unknown;

            // static void Main cannot be async until C# 7.1, so put this on the thread pool
            Task.Run(async () =>
            {
                // open a connection to the UWP AppService
                result = await connection.OpenAsync();

            }).GetAwaiter().GetResult();

            
            if (result == AppServiceConnectionStatus.Success)
            {
                QueryWinSatAssessment();

                GetWinSATBitmap();

                // Let the app service connection handlers respond to events.  If this Win32 app had a Window,
                // this would be a message loop.  The app ends when the app service connection to 
                // the UWP app is closed and our Connection_ServiceClosed event handler is fired.
                while (true)
                {
                    // pump the underlying STA thread
                    // https://blogs.msdn.microsoft.com/cbrumme/2004/02/02/apartments-and-pumping-in-the-clr/
                    Thread.CurrentThread.Join(0);
                }

            }
        }

        /// <summary>
        /// Contact WinSAT through the COM object, get the assessments if valid, and send
        /// the response to the UWP app.
        /// </summary>
        private static void QueryWinSatAssessment()
        {
            // this holds the WinSAT assessment info objects we create
            List<WinSATAssessmentInfo> Assessments = new List<WinSATAssessmentInfo>();

            // Provides access to assessment state.
            //
            // If you get CS1752, "Interop type 'CQueryWinSATClass' cannot be embedded. Use the applicable interface instead",
            // then open References and right-click on WINSATLib to edit its properties. Change the 'Embed Interop Types' setting
            // from True to False.  No need to embed interop types for COM objects that are part of Windows, as WinSAT is.
            //
            CQueryWinSATClass q = new CQueryWinSATClass();

            // make a ValueSet object and send the results to the UWP program
            ValueSet valueSet = new ValueSet();

            // we switch on the value of the verb in the UWP app that receives this valueSet
            valueSet.Add("verb", "assessmentResults");
            valueSet.Add("winsatAssessmentState", q.Info.AssessmentState.ToString());

            // Check for valid WinSAT state.
            if (q.Info.AssessmentState == WINSAT_ASSESSMENT_STATE.WINSAT_ASSESSMENT_STATE_VALID
                || q.Info.AssessmentState == WINSAT_ASSESSMENT_STATE.WINSAT_ASSESSMENT_STATE_INCOHERENT_WITH_HARDWARE)
            {
                // iterate through the types of WinSAT assessments and get the info for each
                IEnumerator e = Enum.GetValues(typeof(WINSAT_ASSESSMENT_TYPE)).GetEnumerator();
                while (e.MoveNext())
                {
                    // get the info from WinSAT for this assessment type
                    IProvideWinSATAssessmentInfo i = q.Info.GetAssessmentInfo((WINSAT_ASSESSMENT_TYPE)e.Current);

                    // put the info received from the COM interface into a convenient List object
                    Assessments.Add(new WinSATAssessmentInfo(i.Score, i.Title, i.Description));
                }

                // grab some additional info from about the overall assessment from the COM object
                float baseScore = q.Info.SystemRating;
                string ratingState = q.Info.RatingStateDesc;
                DateTime assessmentTime = (DateTime)q.Info.AssessmentDateTime;

                valueSet.Add("assessmentState", "valid");

                // add the assessment data
                valueSet.Add("assessmentCount", Assessments.Count);
                valueSet.Add("basescore", baseScore);
                valueSet.Add("ratingState", ratingState);

                //valueSet.Add("assessmentTime", assessmentTime.ToLongDateString());

                // Results from WinSAT actually don't include the time, only the date.  We add
                // the time now so one can distinguish in the UI that a new assessment has been made, since
                // the assessment numbers won't change unless hardware is added/removed or new
                // drivers have been installed, which isn't often.  WinSAT is initially run during
                // the Machine-Out-Of-Box Experience (MOOBE), the first time the computer is turned on.
                valueSet.Add("assessmentTime", assessmentTime.ToLongDateString() + "  " + DateTime.Now.ToLongTimeString());

                // we can only send value types in ValueSet, so stringify our assessment list into JSON
                valueSet.Add("assessments", JsonConvert.SerializeObject(Assessments));               
            }
            else
            {

                // we switch on the value of the verb in the UWP app that receives this valueSet
                valueSet.Add("assessmentState", "invalid");
            }

            AppServiceResponse response = null;

            // put this on the thread pool inside this non-async function
            Task.Run(async () =>
            {
                response = await connection.SendMessageAsync(valueSet);

            }).GetAwaiter().GetResult();

            // we don't need to do anything here, but this is the pattern if you need it
            if (response?.Status == AppServiceResponseStatus.Success)
            {
                //string responseMessage = response.Message["response"].ToString();
                //if (responseMessage == "success")
                //{
                //    // do whatever your scenario requires here
                //}                    
            }
        }

        /// <summary>
        /// The UWP host has sent a request for something. Responses to the UWP app are
        /// sent by the respective case handlers, to the UWP Connection_RequestReceived handler
        /// via the AppServiceConnection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async static void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var deferral = args.GetDeferral();

            // get the verb or "command" for this request
            string verb = (string)args.Request.Message["verb"];

            switch (verb)
            {
                case "getimageRequest":
                    {
                        GetWinSATBitmap();

                        break;
                    }

                case "assessmentRequest":
                    {
                        QueryWinSatAssessment();

                        GetWinSATBitmap();

                        break;
                    }

                case "formalWinSatRequest":
                    {
                        int exitCode = InitiateFormalWinSatAssessment();

                        ValueSet valueSet = new ValueSet();
                        valueSet.Add("exitcode", exitCode);
                        await args.Request.SendResponseAsync(valueSet);

                        break;
                    }
            }

            // this is what happens in the case handlers, FYI
            //ValueSet valueSet = new ValueSet();
            //valueSet.Add("response", "success");
            //await args.Request.SendResponseAsync(valueSet);
            deferral.Complete();
        }

        /// <summary>
        /// Launch the elevated process.  The only way it can communicate back to this
        /// process is through its exit code.
        /// </summary>
        /// <returns></returns>
        private static int InitiateFormalWinSatAssessment()
        {
            // call the elevated process here to trigger assessment
            ProcessStartInfo info = new ProcessStartInfo();
            info.Verb = "runas";
            info.UseShellExecute = true;

            // we want to show the WinSAT console window while it executes its assessment
            info.WindowStyle = ProcessWindowStyle.Normal;

            // this path is a proxy for the Package
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            info.FileName = localAppDataPath + @"\microsoft\windowsapps\WinSatRunAssessment.exe";

            Debug.WriteLine("Inside WinSatInfo, about to start elevated WinSatRunAssessment.exe.");

            Process elevatedProcess = null;
            int exitCode = 0;

            try
            {
                elevatedProcess = Process.Start(info);

                // wait 3 minutes for the assessment to run, should take half that.
                elevatedProcess?.WaitForExit(180000);

                // if everything went normally, the exit code will be zero
                exitCode = elevatedProcess.ExitCode;
            }
            catch(Exception ex)
            {
                if(ex.HResult == E_FAIL)
                {
                    // the user cancelled the elevated process
                    // by clicking "No" on the Windows elevation dialog
                    exitCode = 1;
                }
            }

            return exitCode;
        }

        /// <summary>
        /// Sends image with assessment rating in WinSAT style to UWP app.  You must have called
        /// QueryWinSatAssessment beforehand.
        /// </summary>
        private static void GetWinSATBitmap()
        {
            // Provides access to assessment state.
            //
            // If you get CS1752, "Interop type 'CQueryWinSATClass' cannot be embedded. Use the applicable interface instead",
            // then open References and right-click on WINSATLib to edit its properties. Change the 'Embed Interop Types' setting
            // from True to False.  No need to embed interop types for COM objects that are part of Windows, as WinSAT is.
            //
            CQueryWinSATClass q = new CQueryWinSATClass();

            // pointer to the GDI HBITMAP returned by WinSAT
            IntPtr t;

            // this will hold the bytes from the rating bitmap
            // during its conversion to PNG format
            Byte[] imageBytes;

            // The unsafe keyword denotes an unsafe context, 
            // which is required for any operation involving pointers. 
            unsafe
            {
                /*
                 * get_Bitmap has the following unmanaged signature:
                 *   HRESULT get_Bitmap(
                 *     WINSAT_BITMAP_SIZE bitmapSize,
                 *     WINSAT_ASSESSMENT_STATE state,
                 *     float rating,
                 *     HBITMAP* pBitmap
                 *   );
                 * where HBITMAP is defined as (WinDef.h):
                 *   typedef HANDLE HBITMAP;
                 * 
                 * The last parameter gets translated into an IntPtr which is the pointer (&t) to the pointer (t) to the bitmap.
                 */
                CProvideWinSATVisualsClass visualsClass = new CProvideWinSATVisualsClass();

                // get the GDI Hbitmap and return it in IntPtr t
                visualsClass.get_Bitmap(WINSAT_BITMAP_SIZE.WINSAT_BITMAP_SIZE_NORMAL, q.Info.AssessmentState, q.Info.SystemRating, new IntPtr(&t));
            }

            // take our Hbitmap, turn it into a System.DrawingBitmap object,
            // and then extract the bytes for transmission to UWP app
            if (t != IntPtr.Zero)
            {
                // copy the GDI Hbitmap into a System.Drawing.Bitmap
                Bitmap ratingBitmap = Bitmap.FromHbitmap(t);

                // We're done with the GDI Hbitmap, release it immediately, 
                // as it is a native Win32 resource
                API.DeleteObject(t);

                using (MemoryStream memStream = new MemoryStream())
                {
                    // save the rating bitmap into a memoryStream so we can extract the bytes
                    ratingBitmap.Save(memStream, System.Drawing.Imaging.ImageFormat.Png);
                    
                    // copy the png in the stream to a byte array
                    imageBytes = memStream.ToArray();
                }

                ValueSet valueSet = new ValueSet();

                // we switch on the value of the verb in the UWP app that receives this valueSet
                valueSet.Add("verb", "getimageResults");

                // add the image data
                valueSet.Add("imagebytes", imageBytes);

                AppServiceResponse response = null;

                // put this on the thread pool
                Task.Run(async () =>
                {
                    response = await connection.SendMessageAsync(valueSet);

                }).GetAwaiter().GetResult();

                // we don't need to do anything here, but this is the pattern if you need it
                if (response?.Status == AppServiceResponseStatus.Success)
                {
                    //string responseMessage = response.Message["response"].ToString();
                    //if (responseMessage == "success")
                    //{
                    //    // do whatever your scenario requires here
                    //}             
                }
            }
        }

        /// <summary>
        /// Our UWP app service is closing, so shut ourselves down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            System.Environment.Exit(0);
        }

    }
}
