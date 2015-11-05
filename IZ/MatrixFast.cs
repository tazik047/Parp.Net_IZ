using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IZ
{
    class MatrixFast
    {
        private readonly Vector4[] _mas;
        private readonly int _size;

        public MatrixFast(int size)
        {
            _size = size;
            _mas = new Vector4[size * size / 4];
        }

        public float this[int row, int col]
        {
            get
            {
                var index = row*_size + col;
                switch (index%4)
                {
                    case 0:
                        return _mas[index/4].X;
                    case 1:
                        return _mas[index/4].Y;
                    case 2:
                        return _mas[index/4].Z;
                    default:
                        return _mas[index/4].W;
                }
            }
            set
            {
                int index = row*_size + col;
                switch (index%4)
                {
                    case 0:
                        _mas[index/4].X = value;
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

        public float Max(out int row, out int col)
        {
            var max = Vector4.Zero;
            var indexes = Vector4.Zero;
            Vector4 newMax;
            int counts = _size*_size/4;
            for (var i = 0; i < counts; i++)
            {
                newMax = Vector4.Max(this[i], max);
                /*max = newMax - max;
                if (max.X != 0) indexes.X = i;
                if (max.Y != 0) indexes.Y = i;
                if (max.Z != 0) indexes.Z = i;
                if (max.W != 0) indexes.W = i;*/
                if (max.X < newMax.X) indexes.X = i;
                if (max.Y <newMax.Y) indexes.Y = i;
                if (max.Z <newMax.Z) indexes.Z = i;
                if (max.W < newMax.W) indexes.W = i;
                max = newMax;
            }
            float max1, max2, ind1, ind2;
            if (max.X > max.Y)
            {
                max1 = max.X;
                ind1 = indexes.X*4;
            }
            else
            {
                max1 = max.Y;
                ind1 = indexes.Y*4 + 1;
            }
            if (max.Z > max.W)
            {
                max2 = max.Z;
                ind2 = indexes.Z*4 + 2;
            }
            else
            {
                max2 = max.W;
                ind2 = indexes.W*4 + 3;
            }
            if (max1 > max2)
            {
                row = ((int) ind1)/_size;
                col = ((int) ind1)%_size;
            }
            else
            {
                max1 = max2;
                row = ((int)ind2) / _size;
                col = ((int)ind2) % _size;
            }
            return max1;
        }

        public static MatrixFast operator +(MatrixFast m1, MatrixFast m2)
        {
            var res = new MatrixFast(m1._size);
            for (int i = 0; i < m1._size*m1._size/4; i++)
            {
                res[i] = m1[i] + m2[i];
            }
            return res;
        }

        public static MatrixFast operator -(MatrixFast m1, MatrixFast m2)
        {
            var res = new MatrixFast(m1._size);
            for (int i = 0; i < m1._size * m1._size / 4; i++)
            {
                res[i] = m1[i] - m2[i];
            }
            return res;
        }

        public float[] Mult(float[] vector)
        {
            var res = new float[_size];
            for (var i = 0; i < _size; i++)
            {
                var sum = 0f;
                for (var j = 0; j < _size; j++)
                {
                    sum += this[i, j] * vector[j];
                }
                res[i] = sum;
            }
            return res;
        }

        public MatrixFast MultType1(MatrixFast m)
        {
            var result = new MatrixFast(_size);
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

        public MatrixFast MultType2(MatrixFast m)
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

        private Tuple<MatrixFast, MatrixFast, MatrixFast, MatrixFast> DevideMatrix()
        {
            var halfSize = _size / 2;
            var m1 = new MatrixFast(halfSize);
            var m2 = new MatrixFast(halfSize);
            var m3 = new MatrixFast(halfSize);
            var m4 = new MatrixFast(halfSize);
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
            return new Tuple<MatrixFast, MatrixFast, MatrixFast, MatrixFast>(m1, m2, m3, m4);
        }

        private MatrixFast Combine(MatrixFast c11, MatrixFast c12, MatrixFast c21, MatrixFast c22)
        {
            var res = new MatrixFast(_size);
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

            var m = obj as MatrixFast;

            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    if (this[i, j] != m[i, j])
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
