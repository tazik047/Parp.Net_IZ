using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IZ
{
    class Program
    {
        static Stopwatch st = new Stopwatch();
        private static long minTicks;
        private static long minMilliseconds;

        static void Main(string[] args)
        {
            var rnd = new Random();

            var MATRIX_SIZE = 64;
            
            while (MATRIX_SIZE <= 2048)
            {
                IMatrix<Matrix> m1 = null, m2 = null, m3 = null, m4 = null;

                Console.WriteLine("Размер матрицы: {0}", MATRIX_SIZE);

                Console.WriteLine("Без SIMD:");
                TestMax<Matrix>(MATRIX_SIZE, rnd);
                Console.WriteLine("С SIMD:");
                TestMax<MatrixSimd>(MATRIX_SIZE, rnd);

                var vector = new float[MATRIX_SIZE];
                for (int i = 0; i < MATRIX_SIZE; i++)
                {
                    vector[i] = rnd.Next(100);
                }

                Console.WriteLine("Без SIMD:");
                TestMultVector<Matrix>(MATRIX_SIZE, rnd, vector);
                Console.WriteLine("С SIMD:");
                TestMultVector<MatrixSimd>(MATRIX_SIZE, rnd, vector);

                Console.WriteLine("Без SIMD:");
                TestMultiply<Matrix>(MATRIX_SIZE, rnd);
                Console.WriteLine("С SIMD:");
                TestMultiply<MatrixSimd>(MATRIX_SIZE, rnd);

                Console.WriteLine();
                MATRIX_SIZE *= 2;
            }
            Console.ReadLine();

        }

        private static void TestMultiply<T>(int matrixSize, Random rnd) where T : IMatrix<T>, new()
        {
            T m1 = new T();
            m1.CreateMatrix(matrixSize, rnd);

            T m2 = new T();
            m2.CreateMatrix(matrixSize, rnd);

            T m3 =default(T), m4 = default(T);

            countPerfomance(() =>
            {
                m3 = m1.MultType1(m2);
            });
            Console.WriteLine("Время для умножения 1 способом: {0} (мс)", minMilliseconds);

            countPerfomance(() =>
            {
                m4 = m1.MultType2(m2);
            });
            Console.WriteLine("Время для умножения 2 способом: {0} (мс)", minMilliseconds);

            Console.WriteLine("Матрицы {0}равны.", m3.Equals(m4) ? "" : "НЕ ");
        }

        private static void TestMultVector<T>(int matrixSize, Random rnd, float[] vector) where T : IMatrix<T>, new()
        {
            T m = new T();
            m.CreateMatrix(matrixSize, rnd);
            countPerfomance(() =>
            {
                float[] res = m.Mult(vector);
            });
            Console.WriteLine("Время для умножения на вектор: {0} (такты)", minTicks);
        }

        static void TestMax<T>(int size, Random rnd) where T : IMatrix<T>, new()
        {
            T m = new T();
            m.CreateMatrix(size, rnd);
            float max = 0;
            int row = 0, col = 0;

            countPerfomance(() =>
            {
                max = m.Max(out row, out col);
            });

            Console.WriteLine("Максимальное значение матрицы: m[{0}][{1}] = {2}", row, col, max);
            Console.WriteLine("Время для поиска максимального элемента: {0} (такты)", minTicks);
        }


        static void countPerfomance(Action a)
        {
            long resM = long.MaxValue;
            long resT = long.MaxValue;
            for (int i = 0; i < 2; i++)
            {
                st.Restart();
                a();
                st.Stop();
                if (resT > st.ElapsedTicks)
                {
                    resM = st.ElapsedMilliseconds;
                    resT = st.ElapsedTicks;
                }
            }
            minMilliseconds = resM;
            minTicks = resT;
        }

        static void Print(float[] vector)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                Console.Write("{0} ", vector[i]);
            }
            Console.WriteLine();
        }
    }
}
