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
        private Vector4[] _mas;
        private int _size;

        public MatrixSimd()
        {
        }

        public MatrixSimd(int size)
        {
            _size = size;
            _mas = new Vector4[size * size / 4];
        }

        public float this[int row, int col]
        {
            get
            {
                var index = row * _size + col;
                switch (index % 4)
                {
                    case 0:
                        return _mas[index / 4].X;
                    case 1:
                        return _mas[index / 4].Y;
                    case 2:
                        return _mas[index / 4].Z;
                    default:
                        return _mas[index / 4].W;
                }
            }
            set
            {
                int index = row * _size + col;
                switch (index % 4)
                {
                    case 0:
                        _mas[index / 4].X = value;
                        break;
                    case 1:
                        _mas[index / 4].Y = value;
                        break;
                    case 2:
                        _mas[index / 4].Z = value;
                        break;
                    default:
                        _mas[index / 4].W = value;
                        break;
                }
            }
        }

        public Vector4 this[int index]
        {
            get { return _mas[index]; }
            set { _mas[index] = value; }
        }

        public void Print()
        {
            Console.WriteLine("Начало матрицы:");
            for (var i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    Console.Write("{0:000.0000}  ", this[i, j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine("Конец матрицы.\n");
        }

        public void SetMas(float[] mas, int size)
        {
            _size = size;
            _mas = new Vector4[size * size / 4];
            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    this[i, j] = mas[i * _size + j];
                }
            }
        }

        public float[] ToArray()
        {
            return _mas.SelectMany(vector => new List<float> { vector.X, vector.Y, vector.Z, vector.W }).ToArray();
        }

        public float Max(out int row, out int col)
        {
            var max = new Vector4(float.MinValue);
            var indexes = Vector4.Zero;
            for (var i = 0; i < _mas.Length; i++)
            {
                var newMax = Vector4.Max(this[i], max);
                if (max.X < newMax.X) indexes.X = i;
                if (max.Y < newMax.Y) indexes.Y = i;
                if (max.Z < newMax.Z) indexes.Z = i;
                if (max.W < newMax.W) indexes.W = i;
                max = newMax;
            }
            float max1, max2, ind1, ind2;
            if (max.X > max.Y)
            {
                max1 = max.X;
                ind1 = indexes.X * 4;
            }
            else
            {
                max1 = max.Y;
                ind1 = indexes.Y * 4 + 1;
            }
            if (max.Z > max.W)
            {
                max2 = max.Z;
                ind2 = indexes.Z * 4 + 2;
            }
            else
            {
                max2 = max.W;
                ind2 = indexes.W * 4 + 3;
            }
            if (max1 > max2)
            {
                row = ((int)ind1) / _size;
                col = ((int)ind1) % _size;
            }
            else
            {
                max1 = max2;
                row = ((int)ind2) / _size;
                col = ((int)ind2) % _size;
            }
            return max1;
        }

        public static MatrixSimd operator +(MatrixSimd m1, MatrixSimd m2)
        {
            var res = new MatrixSimd(m1._size);
            for (int i = 0; i < m1._size * m1._size / 4; i++)
            {
                res[i] = m1[i] + m2[i];
            }
            return res;
        }

        public static MatrixSimd operator -(MatrixSimd m1, MatrixSimd m2)
        {
            var res = new MatrixSimd(m1._size);
            for (int i = 0; i < m1._size * m1._size / 4; i++)
            {
                res[i] = m1[i] - m2[i];
            }
            return res;
        }

        public float[] Mult(float[] v)
        {
            var res = new float[_size];
            var vector = new Vector4[_size / 4];
            int i = 0;
            for (int j = 0; j < vector.Length; j++)
            {
                vector[j] = new Vector4(v[i], v[i + 1], v[i + 2], v[i + 3]);
                i += 4;
            }

            i = 0;

            for (var k = 0; k < _size; k++)
            {
                var sum = Vector4.Zero;
                for (int j = 0; j < vector.Length; j++)
                {
                    sum += _mas[i++] * vector[j];
                }
                res[k] = sum.X + sum.Y + sum.Z + sum.W;
            }
            return res;
        }

        public static MatrixSimd Transpose(MatrixSimd m)
        {
            var res = new MatrixSimd(m._size);
            for (int i = 0; i < m._size; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    res[i, j] = m[j, i];
                    res[j, i] = m[i, j];
                }
            }
            return res;
        }

        public MatrixSimd MultType1(MatrixSimd m)
        {
            var result = new MatrixSimd(_size);
            var transposeMatrix = Transpose(m);
            var lineLength = _size / 4;
            for (var i = 0; i < _size; i++)
            {
                for (var j = 0; j < _size; j++)
                {
                    var temp = Vector4.Zero;
                    for (var k = 0; k < lineLength; k++)
                    {
                        temp += this[i * lineLength + k] * transposeMatrix[j * lineLength + k];
                    }
                    result[i, j] = temp.X + temp.Y + temp.Z + temp.W;
                }
            }
            return result;
        }

        public MatrixSimd MultType2(MatrixSimd m)
        {
            if (_size <= 128)
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

        private Tuple<MatrixSimd, MatrixSimd, MatrixSimd, MatrixSimd> DevideMatrix()
        {
            var halfSize = _size / 2;
            var m1 = new MatrixSimd(halfSize);
            var m2 = new MatrixSimd(halfSize);
            var m3 = new MatrixSimd(halfSize);
            var m4 = new MatrixSimd(halfSize);
            int k = 0;
            int lineLength = _size / 4;
            for (int i = 0; i < halfSize; i++)
            {
                for (int j = 0; j < halfSize / 4; j++)
                {
                    m1[k] = this[i * lineLength + j];
                    m2[k] = this[i * lineLength + j + halfSize / 4];
                    m3[k] = this[i * lineLength + j + _size * halfSize / 4];
                    m4[k] = this[i * lineLength + j + (_size + 1) * halfSize / 4];
                    k++;
                }
            }
            return new Tuple<MatrixSimd, MatrixSimd, MatrixSimd, MatrixSimd>(m1, m2, m3, m4);
        }

        private MatrixSimd Combine(MatrixSimd c11, MatrixSimd c12, MatrixSimd c21, MatrixSimd c22)
        {
            var res = new MatrixSimd(_size);
            var halfSize = _size / 2;
            int k = 0;
            int lineLength = _size / 4;

            for (int i = 0; i < halfSize; i++)
            {
                for (int j = 0; j < halfSize / 4; j++)
                {
                    res[i * lineLength + j] = c11[k];
                    res[i * lineLength + j + halfSize / 4] = c12[k];
                    res[i * lineLength + j + _size * halfSize / 4] = c21[k];
                    res[i * lineLength + j + (_size + 1) * halfSize / 4] = c22[k];
                    k++;
                }
            }
            return res;
        }
    }
}