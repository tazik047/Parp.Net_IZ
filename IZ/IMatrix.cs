using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IZ
{
    interface IMatrix<T> where T:IMatrix<T>
    {
        float Max(out int row, out int col);

        float[] Mult(float[] vector);

        T MultType1(T m);

        /// <summary>
        /// Алгоритм Штрассена
        /// </summary>
        T MultType2(T m);

        void FillMatrix(Random rnd);

        void Print();

        void CreateMatrix(int size, Random rnd);
    }
}
