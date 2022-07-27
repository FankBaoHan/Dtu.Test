using System;

namespace DTU.Test.Utils
{
    public class CommonUtils
    {
        public static string Array2Hex(Byte[] arr, int length)
        {
            return BitConverter.ToString(arr, 0, length).Replace("-", " ").ToUpper();
        }

        public static void AddLog(string content)
        {
            Console.WriteLine(DateTime.Now.ToString() + " : " + content);
        }

        public static string GetSn()
        {
            long i = 1;

            foreach (byte b in Guid.NewGuid().ToByteArray())
                i *= ((int)b + 1);

            return string.Format("{0:x}", i - DateTime.Now.Ticks).ToUpper();
        }
    }
}
