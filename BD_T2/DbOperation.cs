﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BD_T2
{
    public class DbOperation
    {
        public Transaction Transaction { get; set; }
        public TransactionOperation Operation { get; set; }
        public string Data { get; set; }
        public string OriginalCommand { get; set; }

        public DbOperation(string data, TransactionOperation operation, Transaction transaction) : this(data, operation, transaction, null)
        {
        }

        public DbOperation(string data, TransactionOperation operation, Transaction transaction, string originalCommand)
        {
            Operation = operation;
            Data = data;
            Transaction = transaction;
            OriginalCommand = originalCommand;
        }

        public override string ToString()
        {
            return OriginalCommand;
        }
    }
            
}
