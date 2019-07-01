using System;

namespace Easy.IO
{
    internal class Character
    {
        //https://blog.csdn.net/qq_35029061/article/details/85478738
        public const int MIN_SUPPLEMENTARY_CODE_POINT = 0x010000;

        public static int CharCount(int codePoint)
        {
            return codePoint >= MIN_SUPPLEMENTARY_CODE_POINT ? 2 : 1;
        }
        public static bool IsISOControl(char ch)
        {
            int code = (int)ch;
            return IsISOControl(code);
        }

        public static bool IsISOControl(int code)
        {
            if ((code >= 0x0 && code <= 0x1f) || (code >= 0x7f && code <= 0x9f))
            {
                return true;
            }
            else
            {
                return false;
            }
        }



    }
}