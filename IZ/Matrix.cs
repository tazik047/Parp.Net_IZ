using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IZ
{
    internal class Matrix
    {
        private readonly float[] _mas;
        private readonly int _size;

        public Matrix(int size)
        {
            _size = size;
            _mas = new float[size*size];
        }

        public float Max(out int row, out int col)
        {
            var max = float.MinValue;
            row = -1;
            for (int i = 0; i < _size*_size; i++)
            {
                if (_mas[i] > max)
                {
                    max = _mas[i];
                    row = i;
                }
            }
            col = row % _size;
            row /= _size;
            return max;
        }

        public float[] Mult(float[] vector)
        {
            var res = new float[_size];
            int i = 0;
            for (var k = 0; k < _size;k++)
            {
                for (int j = 0; j < _size; j++)
                {
                    res[k] += _mas[i++]*vector[j];
                }
            }
            return res;
        }

        public Matrix MultType1(Matrix m)
        {
            var result = new Matrix(_size);
            for (var i = 0; i < _size; i++)
            {
                for (var j = 0; j < _size; j++)
                {
                    result[i, j] = 0;
                    for (var k = 0; k < _size; k++)
                    {
                        result[i, j] += (float)Math.Round(this[i, k]*m[k, j]);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Алгоритм Штрассена
        /// </summary>
        public Matrix MultType2(Matrix m)
        {
            if (_size <= 32)
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
            var c22 = p1 + p3 - p2 + p6;

            return Combine(c11, c12, c21, c22);
        }

        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            var res = new Matrix(m1._size);
            for (int i = 0; i < m1._size; i++)
            {
                for (int j = 0; j < m1._size; j++)
                {
                    res[i, j] = m1[i, j] + m2[i, j];
                }
            }
            return res;
        }

        public static Matrix operator -(Matrix m1, Matrix m2)
        {
            var res = new Matrix(m1._size);
            for (int i = 0; i < m1._size; i++)
            {
                for (int j = 0; j < m1._size; j++)
                {
                    res[i, j] = m1[i, j] - m2[i, j];
                }
            }
            return res;
        }

        private Tuple<Matrix, Matrix, Matrix, Matrix> DevideMatrix()
        {
            var halfSize = _size/2;
            var m1 = new Matrix(halfSize);
            var m2 = new Matrix(halfSize);
            var m3 = new Matrix(halfSize);
            var m4 = new Matrix(halfSize);
            for (int i = 0; i < halfSize; i++)
            {
                for (int j = 0; j < halfSize; j++)
                {
                    m1[i, j] = this[i, j];
                    m2[i, j] = this[i, j + halfSize];
                    m3[i, j] = this[i + halfSize, j];
                    m4[i, j] = this[i + halfSize, j + halfSize];
                }
            }
            return new Tuple<Matrix, Matrix, Matrix, Matrix>(m1, m2, m3, m4);
        }

        private Matrix Combine(Matrix c11, Matrix c12, Matrix c21, Matrix c22)
        {
            var res = new Matrix(_size);
            var halfSize = _size/2;
            for (int i = 0; i < halfSize; i++)
            {
                for (int j = 0; j < halfSize; j++)
                {
                    res[i, j] = c11[i, j];
                    res[i, j + halfSize] = c12[i, j];
                    res[i + halfSize, j] = c21[i, j];
                    res[i + halfSize, j + halfSize] = c22[i, j];
                }
            }
            return res;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var m = obj as Matrix;

            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    if (!EqFloat(this[i, j], m[i, j]))
                        return false;
                }
            }
            return true;
        }



        public float this[int row, int col]
        {
            get { return _mas[row * _size + col]; }
            set { _mas[row * _size + col] = value; }
        }

        public void FillMatrix(Random rnd)
        {
            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    this[i, j] = (float)rnd.NextDouble()*1000;
                }
            }
        }

        public void Print()
        {
            Console.WriteLine("Начало матрицы:");
            for (var i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    Console.Write("{0}\t", this[i, j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine("Конец матрицы.\n");
        }

        public override int GetHashCode()
        {
            return _mas.GetHashCode();
        }

        private static bool EqFloat(float a, float b)
        {
            return Math.Abs(PrepareFloat(a) - PrepareFloat(b))<=5;
        }

        private static long PrepareFloat(float f)
        {
            while (Math.Abs(f)<10000)
            {
                f *= 10;
            }
            while (Math.Abs(f) >= 100000)
            {
                f /= 10;
            }
            return Convert.ToInt64(Math.Truncate(f));
        }


        public float MaxAbs(out int row, out int col)
        {
            var max = 0f;
            row = -1;
            for (int i = 0; i < _size * _size; i++)
            {
                if (Math.Abs(_mas[i]) > Math.Abs(max))
                {
                    max = _mas[i];
                    row = i;
                }
            }
            col = row % _size;
            row /= _size;
            return max;
        }
    }
}
