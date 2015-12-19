using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IZ
{
    internal class MatrixParallel : IMatrix<MatrixParallel>, IMatrixAdditional<MatrixParallel>
    {
        private float[] _mas;
        private int _size;
        private readonly object _locker = new object();
        private float _maxNumber;
        private int _maxNumberIndex;

        public MatrixParallel()
        {
        }

        public MatrixParallel(int size)
        {
            _size = size;
            _mas = new float[size*size];
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
            int index = -1;
            for (int i = start; i < end; i++)
            {
                if (_mas[i] > max)
                {
                    max = _mas[i];
                    index = i;
                }
            }
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
            for (int i = 0; i < processorCount; i++)
            {
                var i1 = i;
                tasks.Add(Task.Factory.StartNew(() => MultAsync(v, res, _size * i1 / processorCount, _size * (i1 + 1) / processorCount)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            return res;
        }

        private void MultAsync(float[] vector, float[] res, int start, int end)
        {
            int i = start * vector.Length;
            for (var k = start; k < end; k++)
            {
                var sum = 0f;
                for (int j = 0; j < vector.Length; j++)
                {
                    sum += _mas[i++] * vector[j];
                }
                res[k] = sum;
            }
        }

        public MatrixParallel MultType1(MatrixParallel m)
        {
            var result = new MatrixParallel(_size);
            var processorCount = Environment.ProcessorCount;
            var tasks = new List<Task>();
            for (int i = 0; i < processorCount; i++)
            {
                var i1 = i;
                tasks.Add(Task.Factory.StartNew(() => MultType1Async(m, result, _size * i1 / processorCount, _size * (i1 + 1) / processorCount)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            return result;
        }

        private void MultType1Async(MatrixParallel m, MatrixParallel result, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                for (var j = 0; j < _size; j++)
                {
                    var temp = 0f;
                    for (var k = 0; k < _size; k++)
                    {
                        temp += this[i, k] * m[k, j];
                    }
                    result[i, j] = temp;
                }
            }
        }

        public MatrixParallel MultType1OtherMethod(MatrixParallel m)
        {
            var result = new MatrixParallel(_size);
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
                                MultType1OtherMethodAsync(m, result, _size * i1 / processorCount,
                                    _size * (i1 + 1) / processorCount, row1)));
                }
                foreach (var task in tasks)
                {
                    task.Wait();
                }
            }
            return result;
        }

        private void MultType1OtherMethodAsync(MatrixParallel m, MatrixParallel result, int start, int end, int row)
        {
            for (var j = start; j < end; j++)
            {
                var temp = 0f;
                for (var k = 0; k < _size; k++)
                {
                    temp += this[row, k] * m[k, j];
                }
                lock (_locker)
                {
                    result[row, j] = temp;
                }
            }
        }

        public MatrixParallel MultType2(MatrixParallel m)
        {
            var element = new Element
            {
                DepthRemaining = Environment.ProcessorCount,
                Locker = _locker
            };
            return MultType2Parallel(m, element);
        }

        private MatrixParallel MultType2Parallel(MatrixParallel m, Element element)
        {
            if (_size <= 128)
            {
                var result = new MatrixParallel(_size);
                MultType1Async(m, result, 0, _size);
                return result;
            }
            var a = DevideMatrix();
            var b = m.DevideMatrix();

            var p = new MatrixParallel[7];
            var c = new MatrixParallel[4];
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

        private void CheckOpportunityRunAsyncOrRunSync(ConcurrentDictionary<int, Task> tasks, MatrixParallel[] p, Func<MatrixParallel> func, int key, Element element)
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

        public static MatrixParallel operator +(MatrixParallel m1, MatrixParallel m2)
        {
            var res = new MatrixParallel(m1._size);
            for (int i = 0; i < m1._size; i++)
            {
                for (int j = 0; j < m1._size; j++)
                {
                    res[i, j] = m1[i, j] + m2[i, j];
                }
            }
            return res;
        }

        public static MatrixParallel operator -(MatrixParallel m1, MatrixParallel m2)
        {
            var res = new MatrixParallel(m1._size);
            for (int i = 0; i < m1._size; i++)
            {
                for (int j = 0; j < m1._size; j++)
                {
                    res[i, j] = m1[i, j] - m2[i, j];
                }
            }
            return res;
        }

        private Tuple<MatrixParallel, MatrixParallel, MatrixParallel, MatrixParallel> DevideMatrix()
        {
            var halfSize = _size/2;
            var m1 = new MatrixParallel(halfSize);
            var m2 = new MatrixParallel(halfSize);
            var m3 = new MatrixParallel(halfSize);
            var m4 = new MatrixParallel(halfSize);
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
            return new Tuple<MatrixParallel, MatrixParallel, MatrixParallel, MatrixParallel>(m1, m2, m3, m4);
        }

        private MatrixParallel Combine(MatrixParallel c11, MatrixParallel c12, MatrixParallel c21, MatrixParallel c22)
        {
            var res = new MatrixParallel(_size);
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

        public float this[int row, int col]
        {
            get { return _mas[row * _size + col]; }
            set { _mas[row * _size + col] = value; }
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
            _mas = new float[size * size];
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
            return _mas;
        }
    }
}
