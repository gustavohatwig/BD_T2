using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BD_T2
{
    internal class Lock
    {
        public int Transaction { get; set; }
        public string Data { get; set; }
        public LockType LockType { get; set; }

        public Lock(int transaction, string data, LockType lockType)
        {
            Transaction = transaction;
            Data = data;
            LockType = lockType;
        }
    }

    internal enum LockType
    {
        Shared,
        Exclusive
    }
}
