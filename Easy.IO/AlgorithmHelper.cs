using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Easy.IO
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
                    provider = new MD5CryptoServiceProvider();
                    break;
                case Algorithm.SHA1:
                    provider = new SHA1CryptoServiceProvider();
                    break;
                case Algorithm.SHA256:
                    provider = new SHA256CryptoServiceProvider();
                    break;
                case Algorithm.SHA384:
                    provider = new SHA384CryptoServiceProvider();
                    break;
                case Algorithm.SHA512:
                    provider = new SHA512CryptoServiceProvider();
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
