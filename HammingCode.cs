using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SAHC
{
    public class HammingCode
    {
        private int _wordLenght;

        public HammingCode(int wordLenght)
        {
            _wordLenght = wordLenght;
        }


        /// <summary> Разбиение массива битов на длину слова </summary>
        /// <param name="bits"> Массив битов </param>
        /// <returns> Массив слов </returns>
        private bool[][] DivideOnWordLenght(BitArray bits)
        {
            var differenceToFull = _wordLenght - bits.Length % _wordLenght;
            var holisticArray = new bool[differenceToFull + bits.Length];
            bits.CopyTo(holisticArray, differenceToFull);

            //TODO не подгонять длину слова под стандарт, пусть один последний будет меньше
            var countOfWords = holisticArray.Length / _wordLenght;
            var result = new bool[countOfWords][];

            for (var i = 0; i < countOfWords; i++)
            {
                result[i] = new bool[_wordLenght];
                Array.Copy(holisticArray, i * _wordLenght, result[i], 0, _wordLenght);
            }

            return result;
        }

        /// <summary> Вставка контрольных битов </summary>
        /// <param name="word"></param>
        private static void InsertControlBits(IList<bool> word)
        {
            var index = 1;

            while (index < word.Count)
            {
                word.Insert(index - 1, false);
                index *= 2;
            }
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

            bool CalculateControlBit(int index) => bitArrayWord.And(bitArrays[index]).Cast<bool>().Count(o => o) % 2 == 1;
        }

        private static int Pow2(int i) => (int) Math.Pow(2, i);
        
        private bool[] Encode(bool[] word)
        {
            var result = word.ToList();

            InsertControlBits(result);
            CalculateControlBits(result);

            return result.ToArray();
        }

        //TODO кодирование слов распареллелить
        public byte[] Encode(string message, int maxErrorsPerWord = 0)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            var bits = new BitArray(bytes);
            var words = DivideOnWordLenght(bits);
            var wordsWithControlBits = words.Select(Encode);
            var unionBits = Union(wordsWithControlBits);
            
            return ToByteArray(unionBits);
        }

        public string Decode(byte[] bytes)
        {
            var bits = new BitArray(bytes);
            var words = DivideOnWordLenght(bits);
            var correctedWords = words.Select(Correct);
            
            var wordsWithoutControlBits = correctedWords.Select(RemoveControlBits);
            var unionBits = Union(wordsWithoutControlBits);
            var bitsToDecode = RemoveEmptyValue(unionBits);
            //remove лишнее
            return Encoding.UTF8.GetString(ToByteArray(bitsToDecode));
        }

        private bool[] RemoveEmptyValue(bool[] bits)
        {
            var result = bits.ToList();
            var isDeleteFirstByte = true;
            
            searchAgain:
            for (var i = 0; i < 8; i++)
            {
                isDeleteFirstByte &= !bits[i];
                if (!isDeleteFirstByte) break;
            }
            

            if (isDeleteFirstByte)
            {
                for (var i = 0; i < 8; i++)
                    result.RemoveRange(0,8);
                goto searchAgain;
            }

            while (result.Count % 8 != 0)
            {
                result.RemoveAt(0);
            }

            return result.ToArray();
        }

        private static bool[] RemoveControlBits(bool[] word)
        {
            var result = word.ToList();
            var controlBitsCount = (int) Math.Floor(Math.Log2(word.Length + 1));
            for (var i = controlBitsCount - 1; i >= 0; i--)
            {
                result.RemoveAt(Pow2(i) - 1);
            }

            return result.ToArray();
        }

        private bool[] Correct(bool[] word)
        {
            var result = word.ToList();
            
            var controlBits = GetControlBits(result);
            CalculateControlBits(result);
            var controlDecodeBits = GetControlBits(result);
            var errorBitPosition = GetErrorBitPosition(controlBits, controlDecodeBits);
            result[errorBitPosition] = !result[errorBitPosition];

            return result.ToArray();
            
            
            bool[] GetControlBits(List<bool> bits)
            {
                var controlBitsCount = (int) Math.Floor(Math.Log2(word.Length + 1));
                var bools = new bool[controlBitsCount];
                for (var i = 0; i < controlBitsCount; i++)
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


        private bool[] Union(IEnumerable<bool[]> wordsWithControlBits)
        {
            var result = Array.Empty<bool>();

            foreach (var bit in wordsWithControlBits) 
                result = result.Concat(bit).ToArray();
            return result;
        }

        public static byte[] ToByteArray(bool[] bits)
        {
            return ToByteArray(new BitArray(bits));
        }
        public static byte[] ToByteArray(BitArray bits)
        {
            byte[] ret = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(ret, 0);
            return ret;
        }
    }
}