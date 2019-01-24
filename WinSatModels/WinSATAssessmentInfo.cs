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

namespace WinSatModels
{
    /// <summary>
    /// Mirror of the Win32 IProvideWinSATAssessmentInfo.
    /// </summary>
    public class WinSATAssessmentInfo
    {
        /// <summary>
        /// The Windows Experience Index subscore value.
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// The name of the WinSAT Component, i.e. "Memory"
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The text describing the WinSAT component represented by Title property.
        /// </summary>
        public string Description { get; set; }

        public WinSATAssessmentInfo(float score, string title, string description)
        {
            Score = score;
            Title = title;
            Description = description;
        }
    }
}
