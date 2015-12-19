using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IZ
{
    class MatrixSimdParallel : IMatrix<MatrixSimdParallel>, IMatrixAdditional<MatrixSimdParallel>
    {
        private Vector<float>[] _mas;
        private int _size;
        private readonly object _locker = new object();
        private static readonly int SimdSize = Vector<float>.Count;
        private float _maxNumber;
        private int _maxNumberIndex;

        public MatrixSimdParallel()
        {
        }

        public MatrixSimdParallel(int size)
        {
            _size = size;
            _mas = new Vector<float>[size * size / SimdSize];
        }

        public Vector<float> this[int index]
        {
            get { return _mas[index]; }
            set { _mas[index] = value; }
        }

        public float this[int row, int col]
        {
            get
            {
                var index = row * _size + col;
                return _mas[index / SimdSize][index % SimdSize];
            }
            set
            {
                int index = row * _size + col;
                var newVectorArray = new float[SimdSize];
                _mas[index / SimdSize].CopyTo(newVectorArray);
                newVectorArray[index % SimdSize] = value;
                _mas[index / SimdSize] = new Vector<float>(newVectorArray);
            }
        }

        public float Max(out int row, out int col)
        {
            _maxNumber = float.MinValue;
            _maxNumberIndex = -1;
            var processorCount = Environment.ProcessorCount;
            var tasks = new List<Task>();
            for (int i = 0; i < processorCount; i++)
            {
                var i1 = i;
                tasks.Add(Task.Factory.StartNew(() => MaxAsync(_mas.Length * i1 / processorCount, _mas.Length * (i1 + 1) / processorCount)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            col = _maxNumberIndex % _size;
            row = _maxNumberIndex / _size;
            return _maxNumber;
        }

        private void MaxAsync(int start, int end)
        {
            var max = float.MinValue;
            var maxVector = new Vector<float>(float.MinValue);
            int index = -1;
            var indexes = Vector<int>.Zero;
            for (int i = start; i < end; i++)
            {
                var t = Vector.Max(maxVector, _mas[i]);
                var changed = Vector.GreaterThan(t - maxVector, Vector<float>.Zero);
                indexes = Vector.Negate(changed) * (new Vector<int>(i)) + (changed + Vector<int>.One) * indexes;
                maxVector = t;
            }
            for (int i = 0; i < SimdSize; i++)
            {
                if (max < maxVector[i])
                {
                    max = maxVector[i];
                    index = i;
                }
            }
            index = indexes[index] * SimdSize + index;
            lock (_locker)
            {
                if (_maxNumber < max)
                {
                    _maxNumberIndex = index;
                    _maxNumber = max;
                }
            }
        }

        public float[] Mult(float[] v)
        {
            var res = new float[_size];
            var processorCount = Environment.ProcessorCount;
            var tasks = new List<Task>();
            var vector = new Vector<float>[_size / SimdSize];
            int index = 0;
            for (int j = 0; j < vector.Length; j++)
            {
                vector[j] = new Vector<float>(v, index);
                index += SimdSize;
            }
            for (int i = 0; i < processorCount; i++)
            {
                var i1 = i;
                tasks.Add(Task.Factory.StartNew(() => MultAsync(vector, res, _size * i1 / processorCount, _size * (i1 + 1) / processorCount)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            return res;
        }

        private void MultAsync(Vector<float>[] vector, float[] res, int start, int end)
        {
            int i = start * vector.Length;
            var sumArray = new float[SimdSize];
            for (var k = start; k < end; k++)
            {
                var sum = Vector<float>.Zero;
                for (int j = 0; j < vector.Length; j++)
                {
                    sum += _mas[i++] * vector[j];
                }
                sum.CopyTo(sumArray);
                res[k] = sumArray.Sum();
            }
        }

        public static MatrixSimdParallel Transpose(MatrixSimdParallel m)
        {
            var res = new MatrixSimdParallel(m._size);
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

        public MatrixSimdParallel MultType1(MatrixSimdParallel m)
        {
            var result = new MatrixSimdParallel(_size);
            var transposeMatrix = Transpose(m);
            var processorCount = Environment.ProcessorCount;
            var tasks = new List<Task>();
            for (int i = 0; i < processorCount; i++)
            {
                var i1 = i;
                tasks.Add(Task.Factory.StartNew(() => MultType1Async(transposeMatrix, result, _size * i1 / processorCount, _size * (i1 + 1) / processorCount)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            return result;
        }

        private void MultType1Async(MatrixSimdParallel m, MatrixSimdParallel result, int start, int end)
        {
            var lineLength = _size / SimdSize;
            var sumArray = new float[SimdSize];
            for (var i = start; i < end; i++)
            {
                for (var j = 0; j < _size; j++)
                {
                    var temp = Vector<float>.Zero;
                    for (var k = 0; k < lineLength; k++)
                    {
                        temp += this[i * lineLength + k] * m[j * lineLength + k];
                    }
                    temp.CopyTo(sumArray);
                    result[i, j] = sumArray.Sum();
                }
            }
        }

        public MatrixSimdParallel MultType1OtherMethod(MatrixSimdParallel m)
        {
            var result = new MatrixSimdParallel(_size);
            var transposeMatrix = Transpose(m);
            var processorCount = Environment.ProcessorCount;
            var tasks = new List<Task>();
            for (int row = 0; row < _size; row++)
            {
                for (int i = 0; i < processorCount; i++)
                {
                    var i1 = i;
                    var row1 = row;
                    tasks.Add(
                        Task.Factory.StartNew(
                            () =>
                                MultType1OtherMethodAsync(transposeMatrix, result, _size * i1 / processorCount,
                                    _size*(i1 + 1)/processorCount, row1)));
                }
                foreach (var task in tasks)
                {
                    task.Wait();
                }
            }
            return result;
        }

        private void MultType1OtherMethodAsync(MatrixSimdParallel m, MatrixSimdParallel result, int start, int end, int row)
        {
            var lineLength = _size / SimdSize;
            var sumArray = new float[SimdSize];

            for (var j = start; j < end; j++)
            {
                var temp = Vector<float>.Zero;
                for (var k = 0; k < lineLength; k++)
                {
                    temp += this[row * lineLength + k] * m[j * lineLength + k];
                }
                temp.CopyTo(sumArray);
                var sum = sumArray.Sum();
                lock (_locker)
                {
                    result[row, j] = sum;
                }
            }
        }

        public MatrixSimdParallel MultType2(MatrixSimdParallel m)
        {
            var element = new Element
            {
                DepthRemaining = Environment.ProcessorCount,
                Locker = _locker
            };
            return MultType2Parallel(m, element);
        }

        private MatrixSimdParallel MultType2Parallel(MatrixSimdParallel m, Element element)
        {
            if (_size <= 128)
            {
                var result = new MatrixSimdParallel(_size);
                MultType1Async(Transpose(m), result, 0, _size);
                return result;
            }
            var a = DevideMatrix();
            var b = m.DevideMatrix();

            var p = new MatrixSimdParallel[7];
            var c = new MatrixSimdParallel[4];
            var tasks = new ConcurrentDictionary<int, Task>();

            CheckOpportunityRunAsyncOrRunSync(tasks, p, () => (a.Item1 + a.Item4).MultType2Parallel(b.Item1 + b.Item4, element), 1, element);
            CheckOpportunityRunAsyncOrRunSync(tasks, p, () => (a.Item3 + a.Item4).MultType2Parallel(b.Item1, element), 2, element);
            CheckOpportunityRunAsyncOrRunSync(tasks, p, () => a.Item1.MultType2Parallel(b.Item2 - b.Item4, element), 3, element);
            CheckOpportunityRunAsyncOrRunSync(tasks, p, () => a.Item4.MultType2Parallel(b.Item3 - b.Item1, element), 4, element);
            CheckOpportunityRunAsyncOrRunSync(tasks, p, () => (a.Item1 + a.Item2).MultType2Parallel(b.Item4, element), 5, element);
            CheckOpportunityRunAsyncOrRunSync(tasks, p, () => (a.Item3 - a.Item1).MultType2Parallel(b.Item1 + b.Item2, element), 6, element);
            CheckOpportunityRunAsyncOrRunSync(tasks, p, () => (a.Item2 - a.Item4).MultType2Parallel(b.Item3 + b.Item4, element), 7, element);

            foreach (var task in tasks)
            {
                task.Value.Wait();
            }

            CheckOpportunityRunAsyncOrRunSync(tasks, c, () => p[0] + p[3] - p[4] + p[6], 1, element);
            CheckOpportunityRunAsyncOrRunSync(tasks, c, () => p[2] + p[4], 2, element);
            CheckOpportunityRunAsyncOrRunSync(tasks, c, () => p[1] + p[3], 3, element);
            CheckOpportunityRunAsyncOrRunSync(tasks, c, () => p[0] - p[1] + p[2] + p[5], 4, element);

            foreach (var task in tasks)
            {
                task.Value.Wait();
            }

            return Combine(c[0], c[1], c[2], c[3]);
        }

        private void CheckOpportunityRunAsyncOrRunSync(ConcurrentDictionary<int, Task> tasks, MatrixSimdParallel[] p, Func<MatrixSimdParallel> func, int key, Element element)
        {
            var startSync = false;
            if (element.DepthRemaining != 0)
            {
                lock (element.Locker)
                {
                    if (element.DepthRemaining != 0)
                    {
                        element.DepthRemaining--;
                        tasks[key] = Task.Run(func).ContinueWith(m =>
                        {
                            p[key - 1] = m.Result;
                            lock (element.Locker)
                            {
                                element.DepthRemaining++;
                            }
                            Task t;
                            tasks.TryRemove(key, out t);
                        });
                    }
                    else
                    {
                        startSync = true;
                    }
                }
            }
            if (element.DepthRemaining == 0 || startSync)
            {
                p[key - 1] = func();
            }
        }

        private Tuple<MatrixSimdParallel, MatrixSimdParallel, MatrixSimdParallel, MatrixSimdParallel> DevideMatrix()
        {
            var halfSize = _size / 2;
            var m1 = new MatrixSimdParallel(halfSize);
            var m2 = new MatrixSimdParallel(halfSize);
            var m3 = new MatrixSimdParallel(halfSize);
            var m4 = new MatrixSimdParallel(halfSize);
            var k = 0;
            var lineLength = _size / SimdSize;
            for (int i = 0; i < halfSize; i++)
            {
                for (int j = 0; j < halfSize / SimdSize; j++)
                {
                    m1[k] = this[i * lineLength + j];
                    m2[k] = this[i * lineLength + j + halfSize / SimdSize];
                    m3[k] = this[i * lineLength + j + _size * halfSize / SimdSize];
                    m4[k] = this[i * lineLength + j + (_size + 1) * halfSize / SimdSize];
                    k++;
                }
            }
            return new Tuple<MatrixSimdParallel, MatrixSimdParallel, MatrixSimdParallel, MatrixSimdParallel>(m1, m2, m3, m4);
        }

        private MatrixSimdParallel Combine(MatrixSimdParallel c11, MatrixSimdParallel c12, MatrixSimdParallel c21, MatrixSimdParallel c22)
        {
            var res = new MatrixSimdParallel(_size);
            var halfSize = _size / 2;
            var k = 0;
            var lineLength = _size / SimdSize;

            for (int i = 0; i < halfSize; i++)
            {
                for (int j = 0; j < halfSize / SimdSize; j++)
                {
                    res[i * lineLength + j] = c11[k];
                    res[i * lineLength + j + halfSize / SimdSize] = c12[k];
                    res[i * lineLength + j + _size * halfSize / SimdSize] = c21[k];
                    res[i * lineLength + j + (_size + 1) * halfSize / SimdSize] = c22[k];
                    k++;
                }
            }
            return res;
        }

        public static MatrixSimdParallel operator +(MatrixSimdParallel m1, MatrixSimdParallel m2)
        {
            var res = new MatrixSimdParallel(m1._size);
            for (int i = 0; i < m1._mas.Length; i++)
            {
                res[i] = m1[i] + m2[i];
            }
            return res;
        }

        public static MatrixSimdParallel operator -(MatrixSimdParallel m1, MatrixSimdParallel m2)
        {
            var res = new MatrixSimdParallel(m1._size);
            for (int i = 0; i < m1._mas.Length; i++)
            {
                res[i] = m1[i] - m2[i];
            }
            return res;
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
            _mas = new Vector<float>[size * size / SimdSize];
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
            var res = new float[_size * _size];
            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    res[i * _size + j] = this[i, j];
                }
            }
            return res;
        }
    }
}
