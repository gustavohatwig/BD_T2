using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BD_T2
{
    public class TwoPhaseLocker
    {
        private const string Regex = @"^(?<Transacao>\d):(?<Operacao>LOCK-S|LOCK-X|READ|WRITE|UNLOCK):(?<Dados>[a-eA-E])$";

        private string commands;
        private List<Transaction> transactions;
        private List<Lock> locks;
        private List<KeyValuePair<Transaction, DbOperation>> realizedOperations; 
        private string Results
        {
            get { return string.Join("\n", realizedOperations.Select(o => o.Value.OriginalCommand)); }
        }

        private StringReader reader;
        private Regex regex;

        public TwoPhaseLocker(string readCommands)
        {
            this.commands = readCommands;
            reader = new StringReader(this.commands);
            regex = new Regex(Regex);
            realizedOperations = new List<KeyValuePair<Transaction, DbOperation>>();
            transactions = new List<Transaction>();
            locks = new List<Lock>();
        }

        public string Step()
        {
            string r = reader.ReadLine();
            if (r == null)
                return "end of file";

            if (!regex.IsMatch(r))
                return "linha de comando com formato inválido.";


            Match match = regex.Match(r);
            int transaction = int.Parse(match.Groups[1].Value);
            TransactionOperation operation = match.Groups[2].Value.ToTransactionOperation();
            string dado = match.Groups[3].Value;
            DbOperation op = new DbOperation(dado, operation, r);

            //Pega a transação que já existe na lista de transações.
            Transaction t = transactions.FirstOrDefault(x => x.Equals(transaction));
            if (t == null)
            {
                t = new Transaction(transaction);
                transactions.Add(t);
            }

            //Se a transação estiver quebrada, executa o próximo comando.
            if (t.Broken)
                Step();
            {
                switch (operation)
                {
                    case TransactionOperation.Read:
                        if (locks.Any(l => l.LockType == LockType.Exclusive && l.Data == dado && !t.Equals(l.Transaction)))
                        {
                            t.OperationQueue.Enqueue(op);
                        }
                        else
                        {
                            //ToDo: aqui ainda deve verificar se tem o lock correto (shared ou exclusivo)
                            ExecuteOperation(t, op);
                        }
                        break;
                    case TransactionOperation.Write:
                        if (locks.Any(l => l.Data == dado && !t.Equals(l.Transaction)))
                        {
                            t.OperationQueue.Enqueue(op);
                        }
                        else
                        {
                            //ToDo: aqui ainda deve verificar se tem o lock correto (shared ou exclusivo)
                            ExecuteOperation(t, op);
                        }
                        break;
                    case TransactionOperation.SharedLock:
                        if (t.ExecutedOperations.Any(eo => eo.Operation == TransactionOperation.Unlock))
                        {
                            //Marca a transação como quebrada. Não irá executar as próximas operações pra ela.
                            t.Broken = true;
                            op.OriginalCommand += " (not executed, 2PL violation)";
                            realizedOperations.Add(new KeyValuePair<Transaction, DbOperation>(t, op));
                        }
                            //Se já houver um lock exclusivo, põe na fila de espera e executa a próxima operaçãp...
                        else if (locks.Any(l => l.LockType == LockType.Exclusive && l.Data == dado))
                        {
                            t.OperationQueue.Enqueue(op);
                            Step();
                        }
                            //Caso contrário, adiciona um lock compartilhado pro dado na lista de locks.
                        else
                        {
                            locks.Add(new Lock(transaction, dado, LockType.Shared));
                            ExecuteOperation(t, op);
                        }
                        break;
                    case TransactionOperation.ExclusiveLock:
                        if (t.ExecutedOperations.Any(eo => eo.Operation == TransactionOperation.Unlock))
                        {
                            //Marca a transação como quebrada. Não irá executar as próximas operações pra ela.
                            t.Broken = true;
                            op.OriginalCommand += " (not executed, 2PL violation)";
                            realizedOperations.Add(new KeyValuePair<Transaction, DbOperation>(t, op));
                        }
                        else if (
                            locks.Any(l => l.Data == dado && t.Equals(l.Transaction) && l.LockType == LockType.Exclusive))
                        {
                            //Já existe um lock exclusivo pra essa transação. Roda o comando seguinte.
                            Step();
                        }
                            //Se já houver um lock qualquer, põe na fila de espera e executa a próxima operação...
                        else if (locks.Any(l => l.Data == dado && !t.Equals(l.Transaction)))
                        {
                            t.OperationQueue.Enqueue(op);
                            Step();
                        }
                        //Caso contrário, adiciona um lock exclusivo pro dado na lista de locks.
                        else
                        {
                            locks.Add(new Lock(transaction, dado, LockType.Exclusive));
                            ExecuteOperation(t, op);
                        }
                        break;
                    case TransactionOperation.Unlock:
                        Lock lo = locks.First(x => x.Data == dado && t.Equals(x.Transaction));
                        locks.Remove(lo);
                        realizedOperations.Add(new KeyValuePair<Transaction, DbOperation>(t, op));
                        break;
                }
            }

            return Results;
        }

        private void ExecuteOperation(Transaction t, DbOperation op)
        {
            realizedOperations.Add(new KeyValuePair<Transaction, DbOperation>(t, op));
            t.ExecutedOperations.Add(op);
        }
    }

    public enum TransactionOperation
    {
        [Description("READ")]
        Read,
        [Description("WRITE")]
        Write,
        [Description("LOCK-X")]
        ExclusiveLock,
        [Description("LOCK-S")]
        SharedLock,
        [Description("UNLOCK")]
        Unlock
    }

    
}
