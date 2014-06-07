using System;
using System.Globalization;
using System.IO;
using log4net.ObjectRenderer;

namespace SimSonic.Console
{
    public class Log4NetExceptionRenderer : IObjectRenderer
    {
        public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
        {
            var exception = obj as Exception;
            if (exception == null)
                return;
            var info = exception.GetExceptionText();
            //var additional = exception.GetExtendedInfo();
            //if (!string.IsNullOrWhiteSpace(additional))
            //    info += "\r\nAdditional Info:" + additional;
            writer.Write(info);
        }
    }
    public static class ExceptionHelper
    {
        private const string ExceptionFormat = "\r\nException Class : {2}\r\nMessage : {0}\r\nStack Trace : {1}";

        /// <summary>
        /// Get exception text as stored in log.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static string GetExceptionText(this Exception exception)
        {
            if (exception != null)
            {
                String exceptionText = String.Format(CultureInfo.InvariantCulture, ExceptionFormat, exception.Message,
                                                     exception.StackTrace, exception.GetType());
                if (exception.InnerException != null)
                    exceptionText += "\r\nInner exception: " + GetExceptionText(exception.InnerException);
                return exceptionText;
            }
            return String.Empty;
        }
    }
}