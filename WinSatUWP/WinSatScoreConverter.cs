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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace WinSatUWP
{
    /// <summary>
    /// This IValueConverter takes a float and returns a string representation to one decimal place.
    /// Without this conversion a score of 6.0 would be displayed as '6' and we want '6.0'.
    /// </summary>
    public class WinSatScoreConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // default return for null value
            string numToConvert = String.Format("{0:N1}", 0);

            if(null != value)
            {
                numToConvert = String.Format("{0:N1}", (float)value);
            }

            return numToConvert;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This IValueConverter takes a DateTime and returns a long date string.
    /// </summary>
    public class AssessmentTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(null != value)
            {
                string retVal = ((DateTime)value).ToLongDateString() +"  "+ ((DateTime)value).ToLongTimeString();

                return retVal;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
