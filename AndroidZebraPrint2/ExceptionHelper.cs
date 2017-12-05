using System;

namespace DakotaIntegratedSolutions
{
    public static class ExceptionHelper
    {
        public static int LineNumber(this Exception e)
        {
            var linenum = 0;

            try
            {
                var message = e.StackTrace;
                char[] seperator = { ':' };
                string[] splitMessage = message.Split(seperator);
                linenum = Convert.ToInt32(splitMessage[2]);
            }
            catch
            {
                // Stack trace is not available!
            }

            return linenum;
        }
    }
}