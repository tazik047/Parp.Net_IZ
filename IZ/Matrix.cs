using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IZ
{
    class Matrix
    {
        private readonly int[] _mas;
        private readonly int _size;

        public Matrix(int size)
        {
            _size = size;
            _mas = new int[size*size];
        }

        public int this[int row, int col]
        {
            get { return _mas[row*_size + col]; }
            set { _mas[row*_size + col] = value; }
        }

        public void FillMatrix()
        {
            var rnd = new Random();
            FillMatrix(rnd);
        }

        public void FillMatrix(Random rnd)
        {
            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    this[i, j] = rnd.Next(100);
                }
            }
        }

        public void Print()
        {
            Console.WriteLine("Start print Matrix:");
            for (var i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    Console.Write("{0}\t", this[i, j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine("End print Matrix.\n");
        }

        public int Max(out int row, out int col)
        {
            var max = int.MinValue;
            row = col = -1;
            for (var i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    if (this[i, j] > max)
                    {
                        max = this[i, j];
                        row = i;
                        col = j;
                    }
                }
            }
            return max;
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

        public int[] Mult(int[] vector)
        {
            var res = new int[_size];
            for (var i = 0; i < _size; i++)
            {
                var sum = 0;
                for (var j = 0; j < _size; j++)
                {
                    sum += this[i, j] * vector[j];
                }
                res[i] = sum;
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
                        result[i, j] += this[i, k] * m[k, j];
                    }
                }
            }
            return result;
        }

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
            var c22 = p1 - p2 + p3 + p6;

            return Combine(c11, c12, c21, c22);
        }

        private Tuple<Matrix, Matrix, Matrix, Matrix> DevideMatrix()
        {
            var halfSize = _size / 2;
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
            var halfSize = _size / 2;
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
                    if(this[i,j]!=m[i,j])
                        return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            return _mas.GetHashCode();
        }
    }
}
