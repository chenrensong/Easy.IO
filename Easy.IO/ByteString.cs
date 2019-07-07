using System;
using System.IO;
using System.Text;

namespace Easy.IO
{
    public class ByteString
    {
        static char[] HEX_DIGITS =
           { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        /** A singleton empty {@code ByteString}. */
        public static ByteString EMPTY = ByteString.Of();
        private byte[] _data;
        private int _hashCode; // Lazily computed; 0 if unknown.
        private string _utf8; // Lazily computed.

        internal ByteString(byte[] data)
        {
            this._data = data; // Trusted internal constructor doesn't clone data.
        }

        /**
         * Returns a new byte string containing a clone Of the bytes Of {@code data}.
         */
        public static ByteString Of(params byte[] data)
        {
            if (data == null) throw new IllegalArgumentException("data == null");
            byte[] clone = data.Copy();
            return new ByteString(clone);
        }

        /**
         * Returns a new byte string containing a copy Of {@code byteCount} bytes Of {@code data} starting
         * at {@code offset}.
         */
        public static ByteString Of(byte[] data, int offset, int byteCount)
        {
            if (data == null) throw new IllegalArgumentException("data == null");
            Util.CheckOffsetAndCount(data.Length, offset, byteCount);
            var newData = data.Copy(offset, byteCount);
            return new ByteString(newData);
        }

        public static ByteString Of(ByteBuffer data)
        {
            if (data == null) throw new IllegalArgumentException("data == null");
            byte[] copy = data.ToArray();
            return new ByteString(copy);
        }

        /** Returns a new byte string containing the {@code UTF-8} bytes Of {@code s}. */
        public static ByteString EncodeUtf8(string s)
        {
            if (s == null) throw new IllegalArgumentException("s == null");
            ByteString byteString = new ByteString(Util.UTF_8.GetBytes(s));
            byteString._utf8 = s;
            return byteString;
        }

        /** Returns a new byte string containing the {@code charset}-encoded bytes Of {@code s}. */
        public static ByteString EncodeString(string s, Encoding charset)
        {
            if (s == null) throw new IllegalArgumentException("s == null");
            if (charset == null) throw new IllegalArgumentException("charset == null");
            return new ByteString(charset.GetBytes(s));
        }

        /** Constructs a new {@code string} by decoding the bytes as {@code UTF-8}. */
        public string Utf8
        {
            get
            {
                string result = _utf8;
                // We don't care if we double-allocate in racy code.
                return result != null ? result : (_utf8 = Util.UTF_8.GetString(_data));
            }
        }

        /** Constructs a new {@code string} by decoding the bytes using {@code charset}. */
        public string GetString(Encoding charset)
        {
            if (charset == null) throw new IllegalArgumentException("charset == null");
            return charset.GetString(_data);
        }

        /**
         * Returns this byte string encoded as <a
         * href="http://www.ietf.org/rfc/rfc2045.txt">Base64</a>. In violation Of the
         * RFC, the returned string does not wrap lines at 76 columns.
         */
        public string Base64()
        {
            return IO.Base64.encode(_data);
        }

        /** Returns the 128-bit MD5 hash Of this byte string. */
        public ByteString MD5()
        {
            return AlgorithmHelper.HashByteString(_data, Algorithm.MD5);
        }

        /** Returns the 160-bit SHA-1 hash Of this byte string. */
        public ByteString SHA1()
        {
            return AlgorithmHelper.HashByteString(_data, Algorithm.SHA1);
        }

        /** Returns the 256-bit SHA-256 hash Of this byte string. */
        public ByteString SHA256()
        {
            return AlgorithmHelper.HashByteString(_data, Algorithm.SHA256);
        }

        /** Returns the 512-bit SHA-512 hash Of this byte string. */
        public ByteString SHA512()
        {
            return AlgorithmHelper.HashByteString(_data, Algorithm.SHA512);
        }

        /** Returns the 160-bit SHA-1 HMAC Of this byte string. */
        public ByteString HMACSha1(ByteString key)
        {
            return AlgorithmHelper.HashByteString(_data, Algorithm.HMACSHA1, key);
        }

        /** Returns the 256-bit SHA-256 HMAC Of this byte string. */
        public ByteString HMACSHA256(ByteString key)
        {
            return AlgorithmHelper.HashByteString(_data, Algorithm.HMACSHA256, key);
        }

        /** Returns the 512-bit SHA-512 HMAC Of this byte string. */
        public ByteString HMACSHA512(ByteString key)
        {
            return AlgorithmHelper.HashByteString(_data, Algorithm.HMACSHA512, key);
        }

        /**
         * Returns this byte string encoded as <a href="http://www.ietf.org/rfc/rfc4648.txt">URL-safe
         * Base64</a>.
         */
        public string Base64Url()
        {
            return IO.Base64.encodeUrl(_data);
        }

        /**
         * Decodes the Base64-encoded bytes and returns their value as a byte string.
         * Returns null if {@code base64} is not a Base64-encoded sequence Of bytes.
         */
        public static ByteString DecodeBase64(string base64)
        {
            if (base64 == null) throw new IllegalArgumentException("base64 == null");
            byte[] decoded = IO.Base64.decode(base64);
            return decoded != null ? new ByteString(decoded) : null;
        }

        /** Returns this byte string encoded in hexadecimal. */
        public string Hex()
        {
            char[] result = new char[_data.Length * 2];
            int c = 0;
            foreach (var b in _data)
            {
                result[c++] = HEX_DIGITS[(b >> 4) & 0xf];
                result[c++] = HEX_DIGITS[b & 0xf];
            }
            return new string(result);
        }

        /** Decodes the hex-encoded bytes and returns their value a byte string. */
        public static ByteString DecodeHex(string hex)
        {
            if (hex == null) throw new IllegalArgumentException("hex == null");
            if (hex.Length % 2 != 0) throw new IllegalArgumentException("Unexpected hex string: " + hex);

            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                int d1 = DecodeHexDigit(hex[i * 2]) << 4;
                int d2 = DecodeHexDigit(hex[i * 2 + 1]);
                result[i] = (byte)(d1 + d2);
            }
            return Of(result);
        }

        private static int DecodeHexDigit(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            throw new IllegalArgumentException("Unexpected hex digit: " + c);
        }

        /**
         * Reads {@code count} bytes from {@code in} and returns the result.
         *
         * @throws java.io.EOFException if {@code in} has fewer than {@code count}
         *     bytes to read.
         */
        public static ByteString Read(Stream @in, int byteCount)
        {
            if (@in == null) throw new IllegalArgumentException("in == null");
            if (byteCount < 0) throw new IllegalArgumentException("byteCount < 0: " + byteCount);

            byte[] result = new byte[byteCount];
            for (int offset = 0, read; offset < byteCount; offset += read)
            {
                read = @in.Read(result, offset, byteCount - offset);
                if (read == -1)
                {
                    throw new Exception();
                }
            }
            return new ByteString(result);
        }

        /**
         * Returns a byte string equal to this byte string, but with the bytes 'A'
         * through 'Z' replaced with the corresponding byte in 'a' through 'z'.
         * Returns this byte string if it contains no bytes in 'A' through 'Z'.
         */
        public ByteString ToAsciiLowercase()
        {
            // Search for an uppercase character. If we don't find one, return this.
            for (int i = 0; i < _data.Length; i++)
            {
                byte c = _data[i];
                if (c < 'A' || c > 'Z') continue;

                // If we reach this point, this string is not not lowercase. Create and
                // return a new byte string.
                byte[] lowercase = _data.Copy();
                lowercase[i++] = (byte)(c - ('A' - 'a'));
                for (; i < lowercase.Length; i++)
                {
                    c = lowercase[i];
                    if (c < 'A' || c > 'Z') continue;
                    lowercase[i] = (byte)(c - ('A' - 'a'));
                }
                return new ByteString(lowercase);
            }
            return this;
        }

        /**
         * Returns a byte string equal to this byte string, but with the bytes 'a'
         * through 'z' replaced with the corresponding byte in 'A' through 'Z'.
         * Returns this byte string if it contains no bytes in 'a' through 'z'.
         */
        public ByteString ToAsciiUppercase()
        {
            // Search for an lowercase character. If we don't find one, return this.
            for (int i = 0; i < _data.Length; i++)
            {
                byte c = _data[i];
                if (c < 'a' || c > 'z') continue;

                // If we reach this point, this string is not not uppercase. Create and
                // return a new byte string.
                byte[] lowercase = _data.Copy();
                lowercase[i++] = (byte)(c - ('a' - 'A'));
                for (; i < lowercase.Length; i++)
                {
                    c = lowercase[i];
                    if (c < 'a' || c > 'z') continue;
                    lowercase[i] = (byte)(c - ('a' - 'A'));
                }
                return new ByteString(lowercase);
            }
            return this;
        }

        /**
         * Returns a byte string that is a substring Of this byte string, beginning at the specified
         * index until the end Of this string. Returns this byte string if {@code beginIndex} is 0.
         */
        public ByteString Substring(int beginIndex)
        {
            return Substring(beginIndex, _data.Length);
        }

        /**
         * Returns a byte string that is a substring Of this byte string, beginning at the specified
         * {@code beginIndex} and ends at the specified {@code endIndex}. Returns this byte string if
         * {@code beginIndex} is 0 and {@code endIndex} is the Length Of this byte string.
         */
        public ByteString Substring(int beginIndex, int endIndex)
        {
            if (beginIndex < 0) throw new IllegalArgumentException("beginIndex < 0");
            if (endIndex > _data.Length)
            {
                throw new IllegalArgumentException("endIndex > Length(" + _data.Length + ")");
            }
            int subLen = endIndex - beginIndex;
            if (subLen < 0)
            {
                throw new IllegalArgumentException("endIndex < beginIndex");
            }
            if ((beginIndex == 0) && (endIndex == _data.Length))
            {
                return this;
            }
            var newData = _data.Copy(beginIndex, subLen);
            return new ByteString(newData);
        }

        /** Returns the byte at {@code pos}. */
        public byte GetByte(int pos)
        {
            return _data[pos];
        }

        /**
         * Returns the number Of bytes in this ByteString.
         */
        public int Size()
        {
            return _data.Length;
        }

        /**
         * Returns a byte array containing a copy Of the bytes in this {@code ByteString}.
         */
        public byte[] ToByteArray()
        {
            return _data;
        }

        /** Returns the bytes Of this string without a defensive copy. Do not mutate! */
        public byte[] InternalArray()
        {
            return _data;
        }

        /**
         * Returns a {@code ByteBuffer} view Of the bytes in this {@code ByteString}.
         */
        public ByteBuffer AsByteBuffer()
        {
            return ByteBuffer.Allocate(_data);
        }

        /** Writes the contents Of this byte string to {@code out}. */
        public void Write(Stream @out)
        {
            if (@out == null) throw new IllegalArgumentException("out == null");
            @out.Write(_data, 0, _data.Length);
        }

        ///** Writes the contents Of this byte string to {@code buffer}. */
        public void Write(EasyBuffer buffer)
        {
            buffer.Write(_data, 0, _data.Length);
        }

        /**
         * Returns true if the bytes Of this in {@code [offset..offset+byteCount)} equal the bytes Of
         * {@code other} in {@code [otherOffset..otherOffset+byteCount)}. Returns false if either range is
         * out Of bounds.
         */
        public bool RangeEquals(int offset, ByteString other, int otherOffset, int byteCount)
        {
            return other.RangeEquals(otherOffset, this._data, offset, byteCount);
        }

        /**
         * Returns true if the bytes Of this in {@code [offset..offset+byteCount)} equal the bytes Of
         * {@code other} in {@code [otherOffset..otherOffset+byteCount)}. Returns false if either range is
         * out Of bounds.
         */
        public bool RangeEquals(int offset, byte[] other, int otherOffset, int byteCount)
        {
            return offset >= 0 && offset <= _data.Length - byteCount
                && otherOffset >= 0 && otherOffset <= other.Length - byteCount
                && Util.ArrayRangeEquals(_data, offset, other, otherOffset, byteCount);
        }

        public bool StartsWith(ByteString prefix)
        {
            return RangeEquals(0, prefix, 0, prefix.Size());
        }

        public bool StartsWith(byte[] prefix)
        {
            return RangeEquals(0, prefix, 0, prefix.Length);
        }

        public bool EndsWith(ByteString suffix)
        {
            return RangeEquals(Size() - suffix.Size(), suffix, 0, suffix.Size());
        }

        public bool EndsWith(byte[] suffix)
        {
            return RangeEquals(Size() - suffix.Length, suffix, 0, suffix.Length);
        }

        public int IndexOf(ByteString other)
        {
            return IndexOf(other.InternalArray(), 0);
        }

        public int IndexOf(ByteString other, int fromIndex)
        {
            return IndexOf(other.InternalArray(), fromIndex);
        }

        public int IndexOf(byte[] other)
        {
            return IndexOf(other, 0);
        }

        public int IndexOf(byte[] other, int fromIndex)
        {
            fromIndex = Math.Max(fromIndex, 0);
            for (int i = fromIndex, limit = _data.Length - other.Length; i <= limit; i++)
            {
                if (Util.ArrayRangeEquals(_data, i, other, 0, other.Length))
                {
                    return i;
                }
            }
            return -1;
        }

        public int LastIndexOf(ByteString other)
        {
            return LastIndexOf(other.InternalArray(), Size());
        }

        public int LastIndexOf(ByteString other, int fromIndex)
        {
            return LastIndexOf(other.InternalArray(), fromIndex);
        }

        public int LastIndexOf(byte[] other)
        {
            return LastIndexOf(other, Size());
        }

        public int LastIndexOf(byte[] other, int fromIndex)
        {
            fromIndex = Math.Min(fromIndex, _data.Length - other.Length);
            for (int i = fromIndex; i >= 0; i--)
            {
                if (Util.ArrayRangeEquals(_data, i, other, 0, other.Length))
                {
                    return i;
                }
            }
            return -1;
        }


        public override bool Equals(object o)
        {
            if (o == this) return true;
            return o is ByteString
        && ((ByteString)o).Size() == _data.Length
        && ((ByteString)o).RangeEquals(0, _data, 0, _data.Length);
        }

        public override int GetHashCode()
        {
            int result = _hashCode;
            if (result != 0)
            {
                return result;
            }
            int hc = _data.Length;
            for (int i = 0; i < _data.Length; ++i)
            {
                hc = unchecked(hc * 314159 + _data[i]);
            }
            result = _hashCode = hc;
            return result;
        }

        public int CompareTo(ByteString byteString)
        {
            int sizeA = Size();
            int sizeB = byteString.Size();
            for (int i = 0, size = Math.Min(sizeA, sizeB); i < size; i++)
            {
                int byteA = GetByte(i) & 0xff;
                int byteB = byteString.GetByte(i) & 0xff;
                if (byteA == byteB) continue;
                return byteA < byteB ? -1 : 1;
            }
            if (sizeA == sizeB) return 0;
            return sizeA < sizeB ? -1 : 1;
        }

        /**
         * Returns a human-readable string that describes the contents Of this byte string. Typically this
         * is a string like {@code [text=Hello]} or {@code [hex=0000ffff]}.
         */
        public override string ToString()
        {
            if (_data.Length == 0)
            {
                return "[size=0]";
            }

            string text = this.Utf8;
            int i = CodePointIndexToCharIndex(text, 64);

            if (i == -1)
            {
                return _data.Length <= 64
                    ? "[hex=" + Hex() + "]"
                    : "[size=" + _data.Length + " hex=" + Substring(0, 64).Hex() + "…]";
            }

            string safeText = text.Substring(0, i)
                .Replace("\\", "\\\\")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
            return i < text.Length
                ? "[size=" + _data.Length + " text=" + safeText + "…]"
                : "[text=" + safeText + "]";
        }

        internal static int CodePointIndexToCharIndex(string s, int codePointCount)
        {
            for (int i = 0, j = 0, Length = s.Length, c; i < Length; i += Character.CharCount(c))
            {
                if (j == codePointCount)
                {
                    return i;
                }
                c = s.CodePointAt(i);
                if ((Character.IsISOControl(c) && c != '\n' && c != '\r')
                    || c == EasyBuffer.REPLACEMENT_CHARACTER)
                {
                    return -1;
                }
                j++;
            }
            return s.Length;
        }

    }

}