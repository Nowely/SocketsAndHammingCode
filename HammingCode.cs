using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SAHC
{
    //TODO кодирование и декодирование слов распареллелить
    public class HammingCode
    {
        /// <summary> Lenght of information word </summary>
        private int WordLenght { get; }

        /// <summary> Possible count of control bits. True value is some or some + 1. Need for calculate true value </summary>
        private int PossibleControlBitsCount => (int) Math.Round(Math.Log2(WordLenght + 1));

        /// <summary> Count of control bits for insert </summary>
        private int ControlBitsCount => WordLenght + PossibleControlBitsCount < Pow2(PossibleControlBitsCount)
            ? PossibleControlBitsCount
            : PossibleControlBitsCount + 1;

        /// <summary> Size of sending package. Must be mod8 == 0 because package send in array of byte </summary>
        private int PackageSize => WordLenght + ControlBitsCount + (8 - (WordLenght + ControlBitsCount) % 8);

        /// <summary> Number of insignificant bits in each package which need to remove </summary>
        private int InsignificantBitsNumber => PackageSize - (WordLenght + ControlBitsCount);
        
        public HammingCode(int wordLenght)
        {
            if (wordLenght < 6)
                throw new ArgumentException("This algorithm while doesn't work for less 6 value!", nameof(wordLenght));

            WordLenght = wordLenght;
        }

        /// <summary> Encode message on byte use Hamming code </summary>
        /// <param name="message"> Message for encode </param>
        /// <param name="maxErrorsPerWord"></param>
        /// <returns> Encoding message </returns>
        public byte[] Encode(string message, int maxErrorsPerWord = 0)
        {
            Log.Write(LogType.Start, message);

            var bytes = Encoding.UTF8.GetBytes(message);
            var words = DivideOnWordLenghtBits(bytes);
            Array.ForEach(words, Encode);
            var unionBits = Concat(words);
            return ToByteArray(unionBits);
        }

        /// <summary> Decode bytes on message </summary>
        /// <param name="bytes"> Encoding message </param>
        /// <returns> Decoding message </returns>
        public string Decode(byte[] bytes)
        {
            var words = DivideOnPackageBits(bytes);
            Array.ForEach(words, Decode);
            var unionBits = Concat(words);
            RemoveExtraBits(unionBits);
            var message = Encoding.UTF8.GetString(ToByteArray(unionBits));

            Log.Write(LogType.Finish, message);
            return message;
        }

        private void Encode(List<bool> word)
        {
            InsertControlBits();
            CalculateControlBits(word);
            AddInsignificantBits();


            // Вставка контрольных битов 
            void InsertControlBits()
            {
                var index = 1;

                while (index < word.Count)
                {
                    word.Insert(index - 1, false);
                    index *= 2;
                }
            }

            void AddInsignificantBits()
            {
                for (var i = 0; i < InsignificantBitsNumber; i++)
                    word.Add(false);
            }
        }


        private void Decode(List<bool> word)
        {
            RemoveInsignificantBits();
            Correct(word);
            RemoveControlBits();

            
            void RemoveInsignificantBits()
            {
                word.RemoveRange(word.Count - InsignificantBitsNumber, InsignificantBitsNumber);
            }

            void RemoveControlBits()
            {
                for (var i = ControlBitsCount - 1; i >= 0; i--)
                {
                    word.RemoveAt(Pow2(i) - 1);
                }
            }
        }


        private void Correct(List<bool> word)
        {
            var controlBits = GetControlBits();
            CalculateControlBits(word);
            var controlDecodeBits = GetControlBits();
            var errorBitPosition = GetErrorBitPosition(controlBits, controlDecodeBits);
            word[errorBitPosition] = !word[errorBitPosition];


            bool[] GetControlBits()
            {
                var bools = new bool[ControlBitsCount];
                for (var i = 0; i < ControlBitsCount; i++)
                {
                    bools[i] = word[Pow2(i) - 1];
                    word[Pow2(i) - 1] = false;
                }

                return bools;
            }

            static int GetErrorBitPosition(bool[] controlBits, bool[] controlDecodeBits)
            {
                var position = 0;
                for (var i = 0; i < controlBits.Length; i++)
                {
                    if (controlBits[i] != controlDecodeBits[i])
                        position += Pow2(i) - 1;
                }

                return position;
            }
        }

        /// <summary> Разбиение массива битов на длину слова </summary>
        /// <param name="bytes"> Массив байтов </param>
        /// <returns> Массив слов </returns>
        private List<bool>[] DivideOnWordLenghtBits(byte[] bytes)
        {
            var bits = new BitArray(bytes);
            Log.Write(LogType.BitsCountForEncode, bits.Count);

            var result = bits.Split(WordLenght);

            // Add extra bits for complete set 
            while (result[^1].Count < WordLenght)
            {
                result[^1].Add(false);
            }

            return result;
        }

        /// <summary> Вычисление контрольных бит </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        private void CalculateControlBits(IList<bool> word)
        {
            var controlBitsCount = (int) Math.Floor(Math.Log2(word.Count + 1));
            var bitArrayWord = new BitArray(word.ToArray());


            var bitArrays = new BitArray[controlBitsCount];

            // Initialize arrays
            for (var i = 0; i < bitArrays.Length; i++)
                bitArrays[i] = new BitArray(word.Count);

            // Fill control bits array
            for (var i = 0; i < bitArrays.Length; i++)
            for (var j = Pow2(i) - 1; j < word.Count; j += Pow2(i + 1))
            {
                var controlLenght = 0;
                while (controlLenght < Pow2(i))
                {
                    bitArrays[i][j] = true;
                    controlLenght++;
                }
            }

            // Calculate
            for (var i = 0; i < controlBitsCount; i++)
            {
                word[(int) Math.Pow(2, i) - 1] = CalculateControlBit(i);
            }

            bool CalculateControlBit(int index) =>
                bitArrayWord.And(bitArrays[index]).Cast<bool>().Count(o => o) % 2 == 1;
        }

        private static int Pow2(int i) => (int) Math.Pow(2, i);
        
        private List<bool>[] DivideOnPackageBits(byte[] bytes)
        {
            var bits = new BitArray(bytes);
            return bits.Split(PackageSize);
        }

        private void RemoveExtraBits(List<bool> bits)
        {
            var isDeleteEndByte = true;

            searchAgain:
            for (var i = 1; i <= 8; i++)
            {
                isDeleteEndByte &= !bits[^i];
                if (!isDeleteEndByte) break;
            }


            if (isDeleteEndByte)
            {
                bits.RemoveRange(bits.Count - 8, 8);
                goto searchAgain;
            }

            while (bits.Count % 8 != 0)
            {
                bits.RemoveAt(bits.Count - 1);
            }
        }


        /// <summary> Convert to byte array </summary>
        private static byte[] ToByteArray(List<bool> bits)
        {
            var bitArray = new BitArray(bits.ToArray());
            byte[] result = new byte[(bitArray.Length - 1) / 8 + 1];
            bitArray.CopyTo(result, 0);
            return result;
        }

        /// <summary> Concatenates two or more arrays into a single one </summary>
        private static List<bool> Concat(List<bool>[] arrays)
        {
            return (from array in arrays from arr in array select arr).ToList();
        }
    }
}