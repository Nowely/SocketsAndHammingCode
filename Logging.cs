using System;

namespace SAHC
{
    public enum LogType
    {
        Start,
        Finish,
        BitsCountForEncode,
        BitsCountForDecode
    }

    public static class Log
    {
        /// <summary> Log to the console if debug mode. </summary>
        public static void Write(LogType type, object value)
        {
#if DEBUG
            var message = GetMessage(type, value);
            Console.WriteLine(message);
#endif
        }

        private static string GetMessage(LogType type, object value)
        {
            return type switch
            {
                LogType.Start => $"Start encoding the message: {value}",
                LogType.Finish => $"Finish decoding the message: {value}",
                LogType.BitsCountForEncode => $"Count of bits for encode: {value}",
                LogType.BitsCountForDecode => $"Count of bits for decode: {value}",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}