using System;
using System.Collections.Generic;

namespace Easy.IO
{
    public class Options : List<ByteString>
    {
        internal ByteString[] _byteStrings;
        internal int[] _trie;

        private Options(ByteString[] byteStrings, int[] trie)
        {
            this._byteStrings = byteStrings;
            this._trie = trie;
        }

        public static Options Of(params ByteString[] byteStrings)
        {
            if (byteStrings.Length == 0)
            {
                // With no choices we must always return -1. Create a trie that selects from an empty set.
                return new Options(new ByteString[0], new int[] { 0, -1 });
            }

            // Sort the byte strings which is required when recursively building the trie. Map the sorted
            // indexes to the caller's indexes.
            List<ByteString> list = new List<ByteString>(byteStrings);
            list.Sort();
            List<int> indexes = new List<int>();
            for (int i = 0; i < list.Count; i++)
            {
                indexes.Add(-1);
            }
            for (int i = 0; i < list.Count; i++)
            {
                int sortedIndex = list.BinarySearch(byteStrings[i]);
                indexes[sortedIndex] = i;
            }
            if (list[0].Size() == 0)
            {
                throw new ArgumentException("the empty byte string is not a supported option");
            }

            // Strip elements that will never be returned because they follow their own prefixes. For
            // example, if the caller provides ["abc", "abcde"] we will never return "abcde" because we
            // return as soon as we encounter "abc".
            for (int a = 0; a < list.Count; a++)
            {
                ByteString prefix = list[a];
                for (int b = a + 1; b < list.Count;)
                {
                    ByteString byteString = list[b];
                    if (!byteString.StartsWith(prefix)) break;
                    if (byteString.Size() == prefix.Size())
                    {
                        throw new ArgumentException("duplicate option: " + byteString);
                    }
                    if (indexes[b] > indexes[a])
                    {
                        list.RemoveAt(b);
                        indexes.Remove(b);
                    }
                    else
                    {
                        b++;
                    }
                }
            }

            var trieBytes = new EasyBuffer();
            buildTrieRecursive(0L, trieBytes, 0, list, 0, list.Count, indexes);

            int[] trie = new int[intCount(trieBytes)];
            for (int i = 0; i < trie.Length; i++)
            {
                trie[i] = trieBytes.ReadInt();
            }
            if (!trieBytes.exhausted())
            {
                throw new AssertionException();
            }
            var cloneStrings = byteStrings.Clone();

            return new Options((ByteString[])cloneStrings /* Defensive copy. */, trie);
        }

        /**
         * Builds a trie encoded as an int array. Nodes in the trie are of two types: SELECT and SCAN.
         *
         * SELECT nodes are encoded as:
         *  - selectChoiceCount: the number of bytes to choose between (a positive int)
         *  - prefixIndex: the result index at the current position or -1 if the current position is not
         *    a result on its own
         *  - a sorted list of selectChoiceCount bytes to match against the input string
         *  - a heterogeneous list of selectChoiceCount result indexes (>= 0) or offsets (< 0) of the
         *    next node to follow. Elements in this list correspond to elements in the preceding list.
         *    Offsets are negative and must be multiplied by -1 before being used.
         *
         * SCAN nodes are encoded as:
         *  - scanByteCount: the number of bytes to match in sequence. This count is negative and must
         *    be multiplied by -1 before being used.
         *  - prefixIndex: the result index at the current position or -1 if the current position is not
         *    a result on its own
         *  - a list of scanByteCount bytes to match
         *  - nextStep: the result index (>= 0) or offset (< 0) of the next node to follow. Offsets are
         *    negative and must be multiplied by -1 before being used.
         *
         * This structure is used to improve locality and performance when selecting from a list of
         * options.
         */
        private static void buildTrieRecursive(
            long nodeOffset,
            EasyBuffer node,
            int byteStringOffset,
            List<ByteString> byteStrings,
            int fromIndex,
            int toIndex,
            List<int> indexes)
        {
            if (fromIndex >= toIndex) throw new AssertionException();
            for (int i = fromIndex; i < toIndex; i++)
            {
                if (byteStrings[i].Size() < byteStringOffset) throw new AssertionException();
            }

            ByteString from = byteStrings[fromIndex];
            ByteString to = byteStrings[toIndex - 1];
            int prefixIndex = -1;

            // If the first element is already matched, that's our prefix.
            if (byteStringOffset == from.Size())
            {
                prefixIndex = indexes[fromIndex];
                fromIndex++;
                from = byteStrings[fromIndex];
            }

            if (from.GetByte(byteStringOffset) != to.GetByte(byteStringOffset))
            {
                // If we have multiple bytes to choose from, encode a SELECT node.
                int selectChoiceCount = 1;
                for (int i = fromIndex + 1; i < toIndex; i++)
                {
                    if (byteStrings[i - 1].GetByte(byteStringOffset)
                        != byteStrings[i].GetByte(byteStringOffset))
                    {
                        selectChoiceCount++;
                    }
                }

                // Compute the offset that childNodes will get when we append it to node.
                long childNodesOffset = nodeOffset + intCount(node) + 2 + (selectChoiceCount * 2);

                node.WriteInt(selectChoiceCount);
                node.WriteInt(prefixIndex);

                for (int i = fromIndex; i < toIndex; i++)
                {
                    byte rangeByte = byteStrings[i].GetByte(byteStringOffset);
                    if (i == fromIndex || rangeByte != byteStrings[i - 1].GetByte(byteStringOffset))
                    {
                        node.WriteInt(rangeByte & 0xff);
                    }
                }

                EasyBuffer childNodes = new EasyBuffer();
                int rangeStart = fromIndex;
                while (rangeStart < toIndex)
                {
                    byte rangeByte = byteStrings[rangeStart].GetByte(byteStringOffset);
                    int rangeEnd = toIndex;
                    for (int i = rangeStart + 1; i < toIndex; i++)
                    {
                        if (rangeByte != byteStrings[i].GetByte(byteStringOffset))
                        {
                            rangeEnd = i;
                            break;
                        }
                    }

                    if (rangeStart + 1 == rangeEnd
                        && byteStringOffset + 1 == byteStrings[rangeStart].Size())
                    {
                        // The result is a single index.
                        node.WriteInt(indexes[rangeStart]);
                    }
                    else
                    {
                        // The result is another node.
                        node.WriteInt((int)(-1 * (childNodesOffset + intCount(childNodes))));
                        buildTrieRecursive(
                            childNodesOffset,
                            childNodes,
                            byteStringOffset + 1,
                            byteStrings,
                            rangeStart,
                            rangeEnd,
                            indexes);
                    }

                    rangeStart = rangeEnd;
                }

                node.Write(childNodes, childNodes.Size);

            }
            else
            {
                // If all of the bytes are the same, encode a SCAN node.
                int scanByteCount = 0;
                for (int i = byteStringOffset, max = Math.Min(from.Size(), to.Size()); i < max; i++)
                {
                    if (from.GetByte(i) == to.GetByte(i))
                    {
                        scanByteCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                // Compute the offset that childNodes will get when we append it to node.
                long childNodesOffset = nodeOffset + intCount(node) + 2 + scanByteCount + 1;

                node.WriteInt(-scanByteCount);
                node.WriteInt(prefixIndex);

                for (int i = byteStringOffset; i < byteStringOffset + scanByteCount; i++)
                {
                    node.WriteInt(from.GetByte(i) & 0xff);
                }

                if (fromIndex + 1 == toIndex)
                {
                    // The result is a single index.
                    if (byteStringOffset + scanByteCount != byteStrings[fromIndex].Size())
                    {
                        throw new AssertionException();
                    }
                    node.WriteInt(indexes[fromIndex]);
                }
                else
                {
                    // The result is another node.
                    var childNodes = new EasyBuffer();
                    node.WriteInt((int)(-1 * (childNodesOffset + intCount(childNodes))));
                    buildTrieRecursive(
                        childNodesOffset,
                        childNodes,
                        byteStringOffset + scanByteCount,
                        byteStrings,
                        fromIndex,
                        toIndex,
                        indexes);
                    node.Write(childNodes, childNodes.Size);
                }
            }
        }

        public new ByteString this[int i]
        {
            get
            {
                return _byteStrings[i];
            }
        }


        public new int Count
        {
            get
            {
                return _byteStrings.Length;
            }
        }

        private static int intCount(EasyBuffer trieBytes)
        {
            return (int)(trieBytes.Size / 4);
        }

        internal int IndexOf(object eMPTY)
        {
            throw new NotImplementedException();
        }
    }
}