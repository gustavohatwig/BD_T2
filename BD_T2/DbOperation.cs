using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BD_T2
{
    public class DbOperation
    {
        public TransactionOperation Operation { get; set; }
        public string Data { get; set; }
        public string OriginalCommand { get; set; }

        public DbOperation(string data, TransactionOperation operation)
        {
            Operation = operation;
            Data = data;
        }

        public DbOperation(string data, TransactionOperation operation, string originalCommand)
        {
            Operation = operation;
            Data = data;
            OriginalCommand = originalCommand;
        }

        public override string ToString()
        {
            return OriginalCommand;
        }
    }
            
}
