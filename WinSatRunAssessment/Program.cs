using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WINSATLib;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinSatRunAssessment
{
    class Program
    {
        static bool m_assessmentComplete = false;

        /// <summary>
        /// Entry point of the demo WinSatRunAssessment application.
        /// </summary>
        /// <remarks>A single-threaded apartment state is required for COM interop. Don't remove the [STAThread] attribute.</remarks>
        [STAThread]
        static int Main(string[] args)
        {
            //Console.WriteLine("Attach debugger to WinSatRunAssessment.exe and press Enter");
            //Console.ReadLine();

            // Provides access to assessment state.
            //
            // If you get CS1752, "Interop type 'CQueryWinSATClass' cannot be embedded. Use the applicable interface instead",
            // then open References and right-click on WINSATLib to edit its properties. Change the 'Embed Interop Types' setting
            // from True to False.  No need to embed interop types for COM objects that are part of Windows, as WinSAT is.
            //
            CInitiateWinSATClass initWinSatClass = new CInitiateWinSATClass();

            _RemotableHandle dummy = new _RemotableHandle();

            // initiate the formal assessmment
            initWinSatClass.InitiateFormalAssessment(new UnManagedWinSatCallbacks(), dummy);

            // Stay alive until the assessment is complete. WinSAT progress reported on the console window.
            while (!m_assessmentComplete) { }

            return 0;
        }

        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //public delegate void WinSATCompleteDelegate(int hresult, string strDescription);

        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //public delegate void WinSATUpdateDelegate(uint uCurrentTick, uint uTickTotal, string strCurrentState);


        public class UnManagedWinSatCallbacks : IWinSATInitiateEvents
        {

            //public WinSATCompleteDelegate WinSATComplete;
            //public WinSATUpdateDelegate WinSATUpdate;

            //public UnManagedWinSatCallbacks()
            //{

            //    WinSATComplete = new WinSATCompleteDelegate(WinSATCompleteImpl);
            //    WinSATUpdate = new WinSATUpdateDelegate(WinSATUpdateImpl);

            //}

            // Is called when WinSAT completes the assessment or an error occurs.
            // HRESULT CWinSATCallbacks::WinSATComplete(HRESULT hr, LPCWSTR description)
            public void WinSATComplete(int hresult, string strDescription)
            {
                if(0 == hresult)
                {
                    Debug.WriteLine(string.Format("\n*** {0)", strDescription));
                }
                else
                {
                    Debug.WriteLine(string.Format("\n*** The assessment failed with {0} and description {1}", hresult, strDescription));
                }

                m_assessmentComplete = true;
            }


            // Is called when the assessment makes progress. Indicates the percentage of the assessment
            // that is complete and the current component being assessed.
            // HRESULT CWinSATCallbacks::WinSATUpdate(UINT currentTick, UINT tickTotal, LPCWSTR currentState)
            public void WinSATUpdate(uint uCurrentTick, uint uTickTotal, string strCurrentState)
            {
                if(uTickTotal > 0)
                {
                    Debug.WriteLine(string.Format("\n*** Percent Complete: {0}", 100 * uCurrentTick / uTickTotal));
                    Debug.WriteLine(string.Format("*** Current assessing {0}", strCurrentState));
                }
            }


        }

            

            

        
    }
}
