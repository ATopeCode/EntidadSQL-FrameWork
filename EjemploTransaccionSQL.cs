bool FuncionTransaccionSql()
{
//TRANSACCIÓN SQL:
        //MUY IMPORTANTE: Todos los objetos derivados de la clase abstracta 'EntidadSQL' que se vayan a utilizar en las operaciones
        //sql dentro del método que se asigne al delegado del objeto 'TransactionSQL' que se llama 'transactionEventHandler', deben
        //de añadirse primero al objeto 'TransactionSQL' para que este se asigne a si mismo en la propiedad 'TransactionSql' de cada objeto
        //y todos compartan la misma conexión (con al misma transacción) en sus operaciones. En caso contrario, si algún objeto EntidadSQL
        //no comparte la conexión con el resto, crea una propia (un AccesoSQL) y estará realizando su operación en una transacción distinta.
        using (TransactionSQL trans = new TransactionSQL(System.Data.IsolationLevel.ReadCommitted))
        {
            GrupoUsuarioEntity grupo = new GrupoUsuarioEntity();
            UsuarioEntity usuario = new UsuarioEntity();
            RelUserGroupEntity relUserGroup = new RelUserGroupEntity();
 
            trans.AddEntidadSql(grupo, usuario, relUserGroup);
 
            trans.transactionEventHandler += () =>
                {
                    grupo.Nombre = "Frikis";
                    grupo.Guardar();
 
                    usuario.Nombre = "Very";
                    //Si no se asigna un id válido de País al 'Usuario' se produce error porque existe una clave que relaciona ambas tablas.
                    //Se produce una excepción dentro del objeto 'TransactionSQL' mientras ejecuta este método y se ejecuta el 'RollBack'
                    //cancelando todas las operaciones de la transacción y esta no se lleva a cabo.
                    usuario.IdPais = 1; 
                    usuario.Guardar();
 
 
                    relUserGroup.IdGrupo = grupo.Id;
                    relUserGroup.IdUsuario = usuario.Id;
                    relUserGroup.Guardar();
 
                    grupo.Nombre = "Frikis Hackers";
                    bool noMeGusta = grupo.Guardar();
                    if(noMeGusta)
                    {
                        throw new Exception("No me gusta que me llamen Friki!");
                    }
                };
 
            try
            {
                trans.ExecuteTransaction();
            }
            catch (TransactionSqlException ex)
            {//Se produjo Excepcion en alguna de las operaciones Sql, se el objeto 'TransactionSQL' cerró la conexión con la B.D. y
                //canceló la transacción (RollBack). 
                //En SOAP se lanza una FaultExcpeción<> de Servicio Web con un mesaje genérico recibido por la propia
                //Excepción.
                //En REST no se lanzan Excepciones, el método del Servicio Web devuelve 'false'.
                return false;
            }
            catch (Exception ex)
            {//Si se produce cualquier tipo de Excepcion que no sea 'TransactionSqlException' entonces se recoge en este 'catch()'.
                //Son las Excepciones lanzadas desde el código del método que se asigna al delegado del objeto 'TransactionSQL' llamado
                //'transactionEventHandler'.
                //En SOAP se lanza una FaultExcpeción<> de Servicio Web con el mesaje recibido por la propia
                //Excepción.
                //En REST no se lanzan Excepciones, el método del Servicio Web devuelve 'false'.
                return false;
            }
 
        }
       
        return true;
}