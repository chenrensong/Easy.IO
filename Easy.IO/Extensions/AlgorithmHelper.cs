using System;
using System.Security.Cryptography;

namespace Easy.IO.Extensions
{
    public sealed class AlgorithmHelper
    {
        public static ByteString HashByteString(ByteString bytes, Algorithm algorithmType, ByteString key = null)
        {
            var buffer = bytes.ToByteArray();
            var bufferKey = key?.ToByteArray();
            var resultBuffer = ComputeHash(buffer, algorithmType, bufferKey);
            return ByteString.Of(resultBuffer);
        }

        public static ByteString HashByteString(byte[] bytes, Algorithm algorithmType, ByteString key = null)
        {
            var bufferKey = key?.ToByteArray();
            var resultBuffer = ComputeHash(bytes, algorithmType, bufferKey);
            return ByteString.Of(resultBuffer);
        }

        public static byte[] ComputeHash(byte[] bytes, Algorithm algorithmType, byte[] key = null)
        {
            HashAlgorithm provider = null;
            switch (algorithmType)
            {
                case Algorithm.MD5:
                    provider = MD5.Create();
                    break;
                case Algorithm.SHA1:
                    provider = SHA1.Create();
                    break;
                case Algorithm.SHA256:
                    provider = SHA256.Create();
                    break;
                case Algorithm.SHA384:
                    provider = SHA384.Create();
                    break;
                case Algorithm.SHA512:
                    provider = SHA512.Create();
                    break;
                case Algorithm.HMACSHA1:
                    provider = new HMACSHA1(key);
                    break;
                case Algorithm.HMACSHA256:
                    provider = new HMACSHA256(key);
                    break;
                case Algorithm.HMACSHA384:
                    provider = new HMACSHA384(key);
                    break;
                case Algorithm.HMACSHA512:
                    provider = new HMACSHA512(key);
                    break;
                case Algorithm.HMACMD5:
                    provider = new HMACMD5(key);
                    break;
                default:
                    break;
            }
            return ComputeHash(bytes, provider);
        }

        private static byte[] ComputeHash(byte[] bytes, HashAlgorithm provider)
        {
            try
            {
                byte[] outBytes = null;
                outBytes = provider.ComputeHash(bytes);
                return outBytes;
            }
            catch (Exception ex)
            {
                throw new Exception("hash error：" + ex.Message);
            }
        }

    }

    public enum Algorithm
    {
        MD5,
        SHA1,
        SHA256,
        SHA384,
        SHA512,
        HMACMD5,
        HMACSHA1,
        HMACSHA256,
        HMACSHA384,
        HMACSHA512
    }

}
