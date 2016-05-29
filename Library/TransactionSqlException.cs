using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GestionSql
{
    public class TransactionSqlException : Exception
    {
        public TransactionSqlException(string message)
            :base(message)
        {
            //Se llama primero al constructor de la clase base antes de ejecutar este constructor.
        }
    }
}