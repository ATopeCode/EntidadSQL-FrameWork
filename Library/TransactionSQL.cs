//Programmed by: Silverio Martinez Garcia - http://www.atopecode.net

using GestionSql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
 
namespace GestionSql
{
    public class TransactionSQL : IDisposable
    {
        /// <summary>
        ///Delegado (puntero a métodos) que ejecutará el método con el código de la transación.
        ///Los objetos 'EntidadSql' que se utilicen en el método pasado como parámetro deben de ser los mismos que se han añadido
        ///a este objeto 'TransactionSQL'.
        /// </summary>
        public Action transactionEventHandler;
 
        /// <summary>
        /// Objeto 'AccesoSql' con la conexión y la transacción a la B.D. Se hace común a todos los objetos derivados de 'EntidadSQL'
        /// que forman parte de la transacción.
        /// </summary>
        private AccesoSql _accesoSql;
        public AccesoSql accesoSql
        {
            get
            {
                return _accesoSql;
            }
        }
 
        /// <summary>
        /// Lista con los objetos derivados de 'EntidadSQL' (tablas) que participan en las operaciones de la Transacción.
        /// </summary>
        private List<EntidadSQL> EntidadesSql;
 
        public TransactionSQL(IsolationLevel isolatedLevel, params EntidadSQL[] entidadesSql)
        {
            BeginTransaction(isolatedLevel);
            EntidadesSql = new List<EntidadSQL>();
 
            AddEntidadSql(entidadesSql);
        }
 
        /// <summary>
        /// Este método crea el objeto 'AccesoSql' con una conexión abierta y una transacción. Se asigna dicho objeto al campo
        /// 'accesoSql' de todos los objetos derivados de la clase 'EntidadSQL' en la lista 'EntidadesSql'.
        /// </summary>
        /// <returns></returns>
        private void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _accesoSql = EntidadSQL.CrearAccesoSQL();
            _accesoSql.BeginTransaction(isolationLevel);
        }
 
        /// <summary>
        /// Se añaden objetos derivados de la clase abstracta 'EntidadSQL' a la lista de este objeto 'TransactionSQL'.
        /// Los objetos añadidos deben de ser los mismos que participen en las operaciones SQL en el método asignado al 
        /// delegado 'transactionEventHandler'.
        /// </summary>
        /// <param name="entidadesSql"></param>
        public void AddEntidadSql(params EntidadSQL[] entidadesSql)
        {
            foreach (EntidadSQL item in entidadesSql)
            {
                EntidadesSql.Add(item);
                item.TransactionSql = this;
            }
        }
 
        /// <summary>
        /// Este método ejecuta el código asignado al delegado 'transactionEventHandler'. Si se completan todas las operaciones
        /// con éxito ejecuta un 'CommitTranscation' y la transacción finaliza con éxito. En caso de no realizarse con éxito alguna
        /// de las operaciones en la B.D. se realiza un 'RollBackTransaction' y todas las operaciones de la transacción quedan canceladas,
        /// también se cierra la conexión con la B.D. y los objetos 'EntidadSQL' quedan liberados de este objeto 'TransactionSQL'.
        /// </summary>
        public void ExecuteTransaction()
        {
            if(transactionEventHandler != null)
            {
                try
                {
                    transactionEventHandler();
 
                    _accesoSql.CommitTransaction();
                }
                catch(Exception ex)
                {//Si se produce alguna Excepcion dentro del método asignado al delegado 'transactionEventHandler' se recoge en este
                 //'catch()' cancelando la 'Tansaccion' (RollBack) y se lanza una nueva excepción para recoger en el método que llama
                 //a 'ExecuteTransaction()'.
                    _accesoSql.RollBackTransaction();
                    Dispose();
 
                    if (ex is TransactionSqlException)
                    {//Si se produjo una Excepcion por fallo en alguna operación Sql se lanza una 'TransactionSqlExcepción' un mensaje genérico.
                        throw new TransactionSqlException("No se pudo completar la transacción con éxito.");
                    }
                    else
                    {//Si se produjo otro tipo de Excepción al ejecutar el método asignado al delegado 'transactionEventHandler' quiere
                     //decir que fué alguna Excepción escrita por el programador en el cuerpo de dicho método, entonces se lanza una
                     //una Excepción con el mismo mensaje.
                        throw new Exception(ex.Message);
                    }
                }
            }
            else
            {
                throw new Exception("No existe método asignado al delegado 'transactionEventHandler' para ejecutar la Transacción Sql.");
            }
        }
 
        /// <summary>
        /// Método de la interfaz 'IDisposable' para liberar recursos. Se ejecuta automáticamente cuando este objeto está dentro
        /// de un bloque using(){} y se termina dicho bloque o se sale de él por algún motivo (return, break, throw Exception).
        /// </summary>
        public void Dispose()
        {
            Close();
        }
 
        /// <summary>
        /// Este método es igual que el 'Dispose()' pero se ejecuta de forma manual. Se cierra la conexión con la B.D. y la Transacción
        /// se da por finalizada. Se liberan todos los objetos 'EntidadSQL' de la lista, de esta Transacción.
        /// </summary>
        public void Close()
        {
            //Se cierra la conexión con la B.D.
            _accesoSql.Dispose();
 
            //Se elimina el este objeto 'TransactionSQL' de todos los objetos 'EntidadSQL'.
            foreach(EntidadSQL item in EntidadesSql)
            {
                item.TransactionSql = null;
            }
        }
    }
}