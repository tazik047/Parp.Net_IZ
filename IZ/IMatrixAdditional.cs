using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IZ
{
    interface IMatrixAdditional<T> where T : IMatrix<T>
    {
        T MultType1OtherMethod(T m);
    }
}
