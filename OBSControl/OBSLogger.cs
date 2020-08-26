using IPA.Logging;
using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable
namespace OBSControl
{
    public class OBSLogger : IOBSLogger
    {
        public OBSLoggerSettings LoggerSettings => OBSLoggerSettings.None;
        public void Log(string message, OBSLogLevel level)
        {
            Logger.log?.Log(level.ToIPALogLevel(), "[OBSWebSocket] " + message);
        }

        public void Log(Exception ex, OBSLogLevel level)
        {
            var ipaLevel = level.ToIPALogLevel();
            if(ex is System.Net.Sockets.SocketException sockEx)
            {
                switch (sockEx.SocketErrorCode)
                {
                    case System.Net.Sockets.SocketError.ConnectionRefused: 
                        // Likely OBS/OBSWebsocket not running, don't log.
                        return;
                    default:
                        break;
                }
            }
            Logger.log?.Log(ipaLevel, "[OBSWebSocket] Exception in OBSWebSocket:");
            Logger.log?.Log(ipaLevel, ex);
        }
    }

    public static class OBSLoggerExtensions
    {
        public static IPA.Logging.Logger.Level ToIPALogLevel(this OBSLogLevel logLevel)
        {
            switch (logLevel)
            {
                case OBSLogLevel.Debug:
                    return IPA.Logging.Logger.Level.Debug;
                case OBSLogLevel.Info:
                    return IPA.Logging.Logger.Level.Info;
                case OBSLogLevel.Warning:
                    return IPA.Logging.Logger.Level.Warning;
                case OBSLogLevel.Error:
                    return IPA.Logging.Logger.Level.Error;
                default:
                    return IPA.Logging.Logger.Level.Debug;
            }
        }
    }
}
