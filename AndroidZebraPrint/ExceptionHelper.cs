using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace AndroidZebraPrint
{
    public static class ExceptionHelper
    {
        public static int LineNumber(this Exception e)
        {
            int linenum = 0;

            try
            {
                String message = e.StackTrace;
                char[] seperator = { ':' };
                string[] splitMessage = message.Split(seperator);
                linenum = Convert.ToInt32(splitMessage[2]);
            }
            catch
            {
                //Stack trace is not available!
            }
            return linenum;
        }
    }
}