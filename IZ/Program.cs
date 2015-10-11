﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IZ
{
    class Program
    {
        private const int MATRIX_SIZE = 512;
        static void Main(string[] args)
        {
            var rnd = new Random();
            var st = new Stopwatch();

            var m1 = new Matrix(MATRIX_SIZE);
            m1.FillMatrix(rnd);
            var m2 = new Matrix(MATRIX_SIZE);
            m2.FillMatrix(rnd);

            int row, col;
            
            st.Restart();
            int max = m2.Max(out row, out col);
            st.Stop();
            Console.WriteLine("Max value of matrix2: m[{0}][{1}] = {2}\n", row, col, max);
            Console.WriteLine("Время для поиска максимального элемента: {0}", st.ElapsedMilliseconds);

            int[] vector = new int[MATRIX_SIZE];
            for (int i = 0; i < MATRIX_SIZE; i++)
            {
                vector[i] = rnd.Next(100);
            }

            st.Restart();
            int[] res = m1.Mult(vector);
            st.Stop();
            Console.WriteLine("Время для умножения на вектор: {0}", st.ElapsedMilliseconds);
            
            
            st.Start();
            Matrix m3 = m1.MultType1(m2);
            st.Stop();
            Console.WriteLine("Время для умножения 1 способом: {0}", st.ElapsedMilliseconds);

            st.Restart();
            var m4 = m1.MultType2(m2);
            st.Stop();
            Console.WriteLine("Время для умножения 2 способом: {0}", st.ElapsedMilliseconds);
        }



        static void Print(int[] vector, int size)
        {
            for (int i = 0; i < size; i++)
            {
                Console.Write("{0} ", vector[i]);
            }
            Console.WriteLine();
        }
    }
}
