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
        private List<KeyValuePair<string, Queue<DbOperation>>> dados;
        private Queue<DbOperation> pendingNexts; 
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
            pendingNexts = new Queue<DbOperation>();
            dados = new List<KeyValuePair<string, Queue<DbOperation>>>();
        }

        private void ExecuteOperation(DbOperation op)
        {
            string dado = op.Data;
            Transaction t = op.Transaction;

            switch (op.Operation)
            {
                case TransactionOperation.Read:
                    if (
                        locks.Any(
                            l => l.LockType == LockType.Exclusive && l.Data == dado && !t.Equals(l.Transaction)))
                    {
                        //Read pendente enfileira na própria transação
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
                        //Write pendente enfileira na própria transação
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
                        //Pedidos de lock pendentes, enfileira nos dado.
                        dados.First(d => d.Key.Equals(dado)).Value.Enqueue(op);
                        Step();
                    }
                        //Caso contrário, adiciona um lock compartilhado pro dado na lista de locks.
                    else
                    {
                        locks.Add(new Lock(t.Id, dado, LockType.Shared));
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
                        locks.Any(
                            l => l.Data == dado && t.Equals(l.Transaction) && l.LockType == LockType.Exclusive))
                    {
                        //Já existe um lock exclusivo pra essa transação. Roda o comando seguinte.
                        Step();
                    }
                        //Se já houver um lock qualquer, põe na fila de espera e executa a próxima operação...
                    else if (locks.Any(l => l.Data == dado && !t.Equals(l.Transaction)))
                    {
                        //Pedidos de lock pendentes enfileira no dado
                        dados.First(d => d.Key.Equals(dado)).Value.Enqueue(op);
                        Step();
                    }
                        //Caso contrário, adiciona um lock exclusivo pro dado na lista de locks.
                    else
                    {
                        locks.Add(new Lock(t.Id, dado, LockType.Exclusive));
                        ExecuteOperation(t, op);
                    }
                    break;
                case TransactionOperation.Unlock:
                    IEnumerable<Lock> lo = locks.Where(x => x.Data == dado && t.Id.Equals(x.Transaction)).ToArray();
                    locks.RemoveAll(p => lo.Contains(p));

                    var lockQueue = dados.First(d => d.Key == dado).Value;
                    if (lockQueue.Any())
                    {
                        //Se não há mais nenhum lock no dado, simplesmente adiciona o próximo pedido de lock na fila de execução
                        if (!locks.Any(l => l.Data == dado))
                        {
                            var oper = lockQueue.Dequeue();
                            pendingNexts.Enqueue(oper);

                            while (oper.Transaction.OperationQueue.Any())
                            {
                                pendingNexts.Enqueue(oper.Transaction.OperationQueue.Dequeue());
                            }
                        }
                        else
                        {
                            //Há locks ainda no mesmo dado, então só adiciona o lock dessa fila se não for um lock exclusivo.
                            var oper = lockQueue.Peek();

                            if (oper.Operation != TransactionOperation.ExclusiveLock)
                            {
                                oper = lockQueue.Dequeue();
                                pendingNexts.Enqueue(oper);

                                while (oper.Transaction.OperationQueue.Peek() != null)
                                {
                                    pendingNexts.Enqueue(oper.Transaction.OperationQueue.Dequeue());
                                }
                            }
                        }
                    }

                    realizedOperations.Add(new KeyValuePair<Transaction, DbOperation>(t, op));
                    break;
            }
        }

        public string Step()
        {
            if(pendingNexts.Any())
                ExecuteOperation(pendingNexts.Dequeue());
            else
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


                //Verifica se o dado já existe na lista.
                if (!dados.Any(d => d.Key.Equals(dado)))
                {
                    dados.Add(new KeyValuePair<string, Queue<DbOperation>>(dado, new Queue<DbOperation>()));
                }

                //Pega a transação que já existe na lista de transações.
                Transaction t = transactions.FirstOrDefault(x => x.Equals(transaction));
                if (t == null)
                {
                    t = new Transaction(transaction);
                    transactions.Add(t);
                }

                DbOperation op = new DbOperation(dado, operation, t, r);

                //Se a transação estiver quebrada, executa o próximo comando.
                if (t.Broken)
                    Step();
                {
                    ExecuteOperation(op);
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
