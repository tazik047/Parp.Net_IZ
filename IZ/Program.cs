using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            //var max = 0f;

            while (MATRIX_SIZE <= 2048)
            {


                Console.WriteLine("Размер матрицы: {0}", MATRIX_SIZE);
                var m1 = new Matrix(MATRIX_SIZE);
                m1.FillMatrix(rnd);
                var m2 = new Matrix(MATRIX_SIZE);
                m2.FillMatrix(rnd);

                Matrix m3 = null, m4 = null;
                float max = 0;
                int row = 0, col = 0;
                

                countPerfomance(() =>
                {
                    max = m2.Max(out row, out col);
                });

                Console.WriteLine("Максимальное значение матрицы2: m[{0}][{1}] = {2}", row, col, max);
                Console.WriteLine("Время для поиска максимального элемента: {0} (такты)", minTicks);

                var vector = new float[MATRIX_SIZE];
                for (int i = 0; i < MATRIX_SIZE; i++)
                {
                    vector[i] = rnd.Next(100);
                }

                countPerfomance(() =>
                {
                    float[] res = m1.Mult(vector);
                });
                Console.WriteLine("Время для умножения на вектор: {0} (такты)", minTicks);

                
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

                Console.WriteLine((m3 - m4).MaxAbs(out row, out col));
                Console.WriteLine();

                Console.WriteLine("Матрицы {0}равны.", Matrix.IsEqual(m3,m4,MATRIX_SIZE) ? "" : "НЕ ");
                Console.WriteLine();
                //Console.ReadLine();
                MATRIX_SIZE *= 2;
            }
            Console.ReadLine();
        }


        static void countPerfomance(Action a)
        {
            long resM = long.MaxValue;
            long resT = long.MaxValue;
            for (int i = 0; i < 1; i++)
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

        static void Print(float[] vector, int size)
        {
            for (int i = 0; i < size; i++)
            {
                Console.Write("{0} ", vector[i]);
            }
            Console.WriteLine();
        }
    }
}
