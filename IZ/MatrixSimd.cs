using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IZ
{
    class MatrixSimd : IMatrix<MatrixSimd>
    {
        protected Vector<float>[] Mas;
        protected int Size;

        private static readonly int SimdSize = Vector<float>.Count; 

        public MatrixSimd()
        {
        }

        public MatrixSimd(int size)
        {
            Size = size;
            Mas = new Vector<float>[size * size / SimdSize];
        }

        public float this[int row, int col]
        {
            get
            {
                var index = row * Size + col;
                return Mas[index/SimdSize][index%SimdSize];
            }
            set
            {
                int index = row * Size + col;
                var newVectorArray = new float[SimdSize];
                Mas[index/SimdSize].CopyTo(newVectorArray);
                newVectorArray[index%SimdSize] = value;
                Mas[index / SimdSize] = new Vector<float>(newVectorArray);
            }
        }

        public Vector<float> this[int index]
        {
            get { return Mas[index]; }
            set { Mas[index] = value; }
        }

        public void Print()
        {
            Console.WriteLine("Начало матрицы:");
            for (var i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    Console.Write("{0:000.0000}  ", this[i, j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine("Конец матрицы.\n");
        }

        public void SetMas(float[] mas, int size)
        {
            Size = size;
            Mas = new Vector<float>[size * size / SimdSize];
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    this[i, j] = mas[i * Size + j];
                }
            }
        }

        public float[] ToArray()
        {
            var res = new float[Size*Size];
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    res[i*Size + j] = this[i, j];
                }
            }
            return res;
        }

        public virtual float Max(out int row, out int col)
        {
            var max = float.MinValue;
            var maxVector = new Vector<float>(float.MinValue);
            col = -1;
            var indexes = Vector<int>.Zero;
            for (int i = 0; i < Mas.Length; i++)
            {
                var t = Vector.Max(maxVector, Mas[i]);
                var changed = Vector.GreaterThan(t - maxVector, Vector<float>.Zero);
                indexes = Vector.Negate(changed) * (new Vector<int>(i)) + (changed + Vector<int>.One) * indexes;
                maxVector = t;
            }
            for (int i = 0; i < SimdSize; i++)
            {
                if (max < maxVector[i])
                {
                    max = maxVector[i];
                    col = i;
                }
            }
            row = indexes[col]*SimdSize + col;
            col = row % Size;
            row /= Size;
            return max;
        }

        public static MatrixSimd operator +(MatrixSimd m1, MatrixSimd m2)
        {
            var res = new MatrixSimd(m1.Size);
            for (int i = 0; i < m1.Mas.Length; i++)
            {
                res[i] = m1[i] + m2[i];
            }
            return res;
        }

        public static MatrixSimd operator -(MatrixSimd m1, MatrixSimd m2)
        {
            var res = new MatrixSimd(m1.Size);
            for (int i = 0; i < m1.Mas.Length; i++)
            {
                res[i] = m1[i] - m2[i];
            }
            return res;
        }

        public virtual float[] Mult(float[] v)
        {
            var res = new float[Size];
            var vector = new Vector<float>[Size / SimdSize];
            int i = 0;
            for (int j = 0; j < vector.Length; j++)
            {
                vector[j] = new Vector<float>(v, i);
                i += SimdSize;
            }

            i = 0;
            var sumArray = new float[SimdSize];
            for (var k = 0; k < Size; k++)
            {
                var sum = Vector<float>.Zero;
                for (int j = 0; j < vector.Length; j++)
                {
                    sum += Mas[i++] * vector[j];
                }
                sum.CopyTo(sumArray);
                res[k] = sumArray.Sum();
            }
            return res;
        }

        public static MatrixSimd Transpose(MatrixSimd m)
        {
            var res = new MatrixSimd(m.Size);
            for (int i = 0; i < m.Size; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    res[i, j] = m[j, i];
                    res[j, i] = m[i, j];
                }
            }
            return res;
        }

        public virtual MatrixSimd MultType1(MatrixSimd m)
        {
            var result = new MatrixSimd(Size);
            var transposeMatrix = Transpose(m);
            var lineLength = Size / SimdSize;
            var sumArray = new float[SimdSize];
            for (var i = 0; i < Size; i++)
            {
                for (var j = 0; j < Size; j++)
                {
                    var temp = Vector<float>.Zero;
                    for (var k = 0; k < lineLength; k++)
                    {
                        temp += this[i * lineLength + k] * transposeMatrix[j * lineLength + k];
                    }
                    temp.CopyTo(sumArray);
                    result[i, j] = sumArray.Sum();
                }
            }
            return result;
        }

        public virtual MatrixSimd MultType2(MatrixSimd m)
        {
            if (Size <= 256)
                return MultType1(m);
            var a = DevideMatrix();
            var b = m.DevideMatrix();

            var p1 = (a.Item1 + a.Item4).MultType2(b.Item1 + b.Item4);
            var p2 = (a.Item3 + a.Item4).MultType2(b.Item1);
            var p3 = a.Item1.MultType2(b.Item2 - b.Item4);
            var p4 = a.Item4.MultType2(b.Item3 - b.Item1);
            var p5 = (a.Item1 + a.Item2).MultType2(b.Item4);
            var p6 = (a.Item3 - a.Item1).MultType2(b.Item1 + b.Item2);
            var p7 = (a.Item2 - a.Item4).MultType2(b.Item3 + b.Item4);

            var c11 = p1 + p4 - p5 + p7;
            var c12 = p3 + p5;
            var c21 = p2 + p4;
            var c22 = p1 - p2 + p3 + p6;

            return Combine(c11, c12, c21, c22);
        }

        protected Tuple<MatrixSimd, MatrixSimd, MatrixSimd, MatrixSimd> DevideMatrix()
        {
            var halfSize = Size / 2;
            var m1 = new MatrixSimd(halfSize);
            var m2 = new MatrixSimd(halfSize);
            var m3 = new MatrixSimd(halfSize);
            var m4 = new MatrixSimd(halfSize);
            int k = 0;
            int lineLength = Size / SimdSize;
            for (int i = 0; i < halfSize; i++)
            {
                for (int j = 0; j < halfSize / SimdSize; j++)
                {
                    m1[k] = this[i * lineLength + j];
                    m2[k] = this[i * lineLength + j + halfSize / SimdSize];
                    m3[k] = this[i * lineLength + j + Size * halfSize / SimdSize];
                    m4[k] = this[i * lineLength + j + (Size + 1) * halfSize / SimdSize];
                    k++;
                }
            }
            return new Tuple<MatrixSimd, MatrixSimd, MatrixSimd, MatrixSimd>(m1, m2, m3, m4);
        }

        protected MatrixSimd Combine(MatrixSimd c11, MatrixSimd c12, MatrixSimd c21, MatrixSimd c22)
        {
            var res = new MatrixSimd(Size);
            var halfSize = Size / 2;
            int k = 0;
            int lineLength = Size / SimdSize;

            for (int i = 0; i < halfSize; i++)
            {
                for (int j = 0; j < halfSize / SimdSize; j++)
                {
                    res[i * lineLength + j] = c11[k];
                    res[i * lineLength + j + halfSize / SimdSize] = c12[k];
                    res[i * lineLength + j + Size * halfSize / SimdSize] = c21[k];
                    res[i * lineLength + j + (Size + 1) * halfSize / SimdSize] = c22[k];
                    k++;
                }
            }
            return res;
        }
    }
}