using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BD_T2
{
    public class Transaction
    {
        public int Id { get; private set; }
        //Essa lista é usada pra reclamar quando há um lock após já ter executado um unlock... Violação 2PL.
        public List<DbOperation> ExecutedOperations { get; set; } 
        public Queue<DbOperation> OperationQueue { get; private set; }
        //Indica se a transação está quebrada, por ter violado 2PL
        public bool Broken { get; set; }

        public Transaction(int id)
        {
            Id = id;
            OperationQueue = new Queue<DbOperation>();
            ExecutedOperations = new List<DbOperation>();
            Broken = false;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var t = obj as Transaction;
            if (t != null)
                return t.Id == Id;

            int tId;
            if (!int.TryParse(obj.ToString(), out tId))
                return false;

            return tId == Id;
        }
    }
}
