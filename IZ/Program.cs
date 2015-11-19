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
            Console.WriteLine(Vector<float>.Count == 4 ? "SSE включено" : "AVX включено");

            while (MATRIX_SIZE <= 2048)
            {
                Console.WriteLine("Размер матрицы: {0}", MATRIX_SIZE);

                float[] masNumbers1 = new float[MATRIX_SIZE*MATRIX_SIZE];
                float[] masNumbers2 = new float[MATRIX_SIZE * MATRIX_SIZE];
                float[] vector = new float[MATRIX_SIZE * MATRIX_SIZE];

                FillMas(masNumbers1, rnd, MATRIX_SIZE);
                FillMas(masNumbers2, rnd, MATRIX_SIZE);
                FillMas(vector, rnd, MATRIX_SIZE);

                Console.WriteLine("Без SIMD:");
                TestMax<Matrix>(MATRIX_SIZE, masNumbers1);
                Console.WriteLine("С SIMD:");
                TestMax<MatrixSimd>(MATRIX_SIZE, masNumbers1);
                PrintSeparate();

                Console.WriteLine("Без SIMD:");
                var res1 = TestMultVector<Matrix>(MATRIX_SIZE, masNumbers1, vector);
                Console.WriteLine("С SIMD:");
                var res2 = TestMultVector<MatrixSimd>(MATRIX_SIZE, masNumbers1, vector);
                Console.WriteLine("Результаты {0}равны", Equals(res1, res2) ? "" : "НЕ ");
                PrintSeparate();

                Console.WriteLine("Без SIMD:");
                res1 = TestMultiply<Matrix>(MATRIX_SIZE, masNumbers1, masNumbers2);
                Console.WriteLine("С SIMD:");
                res2 = TestMultiply<MatrixSimd>(MATRIX_SIZE, masNumbers1, masNumbers2);
                Console.WriteLine("Результаты {0}равны", Equals(res1, res2) ? "" : "НЕ ");
                PrintSeparate();
                PrintSeparate();
                
                Console.WriteLine();
                MATRIX_SIZE *= 2;
            }
            Console.ReadLine();
        }

        public static void PrintSeparate()
        {
            Console.WriteLine("\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");
        }

        public static void FillMas(float[] mas, Random rnd, int size)
        {
            for (int i = 0; i < size * size; i++)
            {
                mas[i] = (float)rnd.NextDouble() * 1000;
            }
        }

        private static float[] TestMultiply<T>(int matrixSize, float[] masNumbers1, float[] masNumbers2) where T : IMatrix<T>, new()
        {
            T m1 = new T();
            m1.SetMas(masNumbers1, matrixSize);

            T m2 = new T();
            m2.SetMas(masNumbers2, matrixSize);

            T m3 = default(T), m4 = default(T);

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

            Console.WriteLine("Матрицы {0}равны.", Equals(m3.ToArray(), m4.ToArray()) ? "" : "НЕ ");
            return m4.ToArray();
        }

        private static float[] TestMultVector<T>(int matrixSize, float[] mas, float[] vector) where T : IMatrix<T>, new()
        {
            T m = new T();
            m.SetMas(mas, matrixSize);
            float[] res = null;
            countPerfomance(() =>
            {
                res = m.Mult(vector);
            });
            Console.WriteLine("Время для умножения на вектор: {0} (такты)", minTicks);
            return res;
        }

        static void TestMax<T>(int size, float[] masNumbers1) where T : IMatrix<T>, new()
        {
            T m = new T();
            m.SetMas(masNumbers1, size);
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
            for (int i = 0; i < 5; i++)
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

        static bool Equals(float[] mas1, float[] mas2)
        {
            for (int i = 0; i < mas1.Length; i++)
            {
                if (!EqFloat(mas1[i], mas2[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool EqFloat(float a, float b)
        {
            if (a == b)
            {
                return true;
            }
            var epsilon = 0.1f;
            return (Math.Abs(a - b) / Math.Max(a, b)) < epsilon;
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