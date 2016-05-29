//Programmed by: Silverio Martinez Garcia - http://www.atopecode.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.IO;
 
 
namespace GestionSql
{
    public class FilasDB : List<Object[]> { }
 
    public class AccesoSql:IDisposable
    {
        private SqlConnection conex;
        private SqlTransaction transaction = null;
 
        public AccesoSql(string nombreConnectionStringWebConfig = "conex")
        {
            conex = new SqlConnection();
            try
            {
                conex.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings[nombreConnectionStringWebConfig].ToString();
                conex.Open();
            }
            catch (Exception ex)
            {
                GestionExcepcionWeb ge = new GestionExcepcionWeb(ex);
                ge.Log();
            }
        }
 
        public AccesoSql(string servidor, string nombreBD, string user, string password)
        {
            conex = new SqlConnection();
            string sentencia;
            sentencia=string.Format("Data Source={0};Initial catalog={1};User Id={2};Password={3}",servidor,nombreBD,user,password);
            conex.ConnectionString = sentencia;
            
            try
            {
                conex.Open();
            }
            catch (Exception ex)
            {
                GestionExcepcionWeb ge = new GestionExcepcionWeb(ex);
                ge.Log();
            }
 
        }
 
        #region Transaccion SQL
        //Este método crea una Transaccion SQL para la conexion y se guarda como un campo de este objeto 'AccesoSql'.
        public bool BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if(transaction == null)
            {//Solo puede haber una Transacción creada por objeto 'AccesoSql', si ya existe una, no se crea otra.
                try
                {
                    transaction = conex.BeginTransaction(isolationLevel);
                    return true;
                }
                catch(Exception ex)
                {
                    GestionExcepcionWeb.WriteLog(ex);
                }
            }
 
            return false;
        }
 
        public bool CommitTransaction()
        {
 
            if (transaction != null)
            {
                try
                {
                    transaction.Commit();
                    return true;
                }
 
                catch (Exception ex)
                {
                    GestionExcepcionWeb.WriteLog(ex);
                }
            }
 
            return false;
        }
 
        public bool RollBackTransaction()
        {
            if(transaction != null)
            {
                try
                {
                    transaction.Rollback();
                    return true;
                }
                catch(Exception ex)
                {
                    GestionExcepcionWeb.WriteLog(ex);
                }
            }
 
            return false;
        }
        #endregion
 
        /// <summary>
        /// Este método ejecuta una sentencia sql en la B.D.
        /// </summary>
        /// <param name="sentencia">Cadena de caracteres con la sentencia sql (insertar, consultar, borrar...)</param>
        /// <returns>Devuelve el resultado de cada sentencia en una Lista de Array de Objects.</returns>
        public FilasDB Ejecuta(string sentencia)
        {
            FilasDB filas = new FilasDB();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conex;
                cmd.CommandText = sentencia;
 
                if (transaction != null)
                {//Si se creó una transacción para la conexión.
                    cmd.Transaction = transaction;
                }
 
                try
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Object[] campos = new object[reader.FieldCount];
                            reader.GetValues(campos);
                            filas.Add(campos);
                        }
                        //reader.Close(); No se pone porque al acabar el using, se llama a reader.Dispose()
                        //que a su vez llama a reader.Close();
                    }
                }
                catch (Exception ex)
                {
                    //Título en el archivo de Logs para separar por párrafos todos los mensajes
                    //que pertenecen a la misma operación.
                    GestionExcepcionWeb.WriteLog("ERROR SQL SERVER:");
                    GestionExcepcionWeb ge = new GestionExcepcionWeb(ex);
                    ge.Log();
 
                    if(transaction != null)
                    {//Si se están ejecutando los comandos SQL dentro de una transacción, si se produce alguna Excepción en algún
                     //comando, hay que lanzar una excepción para recogerla desde el código que utiliza el objeto 'AccesoSql' y capturarla
                     //para ejecutar el método 'RollBack()' de este objeto.
                        GestionExcepcionWeb.WriteLog("FALLO EN TRANSACCIÓN SQL: La siguiente operación no se pudo completar con éxito: \n" + cmd.CommandText);
                        throw new TransactionSqlException("No se pudo completar la Transacción Sql con éxito.");
                    }
                }
 
                return filas;
            }
        }
 
        /// <summary>
        /// Ejecuta una sentencia sql pero con parámetros "@".
        /// </summary>
        /// <param name="comandoSql">Objeto SqlCommand con la instrucción Sql y la conexión ya asignados.</param>
        /// <returns></returns>
        public FilasDB Ejecuta(SqlCommand comandoSql)
        {
            FilasDB filas = new FilasDB();
            using (SqlCommand cmd = comandoSql)
            {
                cmd.Connection = conex;
 
                if (transaction != null)
                {//Si se creó una transacción para la conexión.
                    cmd.Transaction = transaction;
                }
 
                try
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Object[] campos = new object[reader.FieldCount];
                            reader.GetValues(campos);
                            filas.Add(campos);
                        }
                        //reader.Close(); No se pone porque al acabar el using, se llama a reader.Dispose()
                        //que a su vez llama a reader.Close();
                    }
                }
                catch (Exception ex)
                {
                    //Título en el archivo de Logs para separar por párrafos todos los mensajes
                    //que pertenecen a la misma operación.
                    GestionExcepcionWeb.WriteLog("ERROR SQL SERVER:");
                    GestionExcepcionWeb ge = new GestionExcepcionWeb(ex);
                    ge.Log();
 
                    if (transaction != null)
                    {//Si se están ejecutando los comandos SQL dentro de una transacción, si se produce alguna Excepción en algún
                        //comando, hay que lanzar una excepción para recogerla desde el código que utiliza el objeto 'AccesoSql' y capturarla
                        //para ejecutar el método 'RollBack()' de este objeto.
                        GestionExcepcionWeb.WriteLog("FALLO EN TRANSACCIÓN SQL: La siguiente operación no se pudo completar con éxito: \n" + cmd.CommandText);
                        throw new TransactionSqlException("No se pudo completar la Transacción Sql con éxito.");
                    }
                }
 
                return filas;
            }
        }
 
        /// <summary>
        /// Devuelve el tipo de dato SQL de un objeto .NET
        /// </summary>
        /// <param name="valor">Objeto cuyo valor corresponde a un campo de la B.D.</param>
        /// <returns>Enumeración que indica con el tipo de dato correspondiente en Transaq-SQL.</returns>
        private SqlDbType TipoNetToSql(Object valor)
        {
            if (valor is bool)
            {
                return SqlDbType.Bit;
            }
 
            if (valor is byte)
            {
                return SqlDbType.TinyInt;
            }
 
            if (valor is DateTime)
            {
                return SqlDbType.DateTime;
            }
 
            if (valor is Decimal)
            {
                return SqlDbType.Decimal;
            }
 
            if ((valor is double) || (valor is float))
            {
                return SqlDbType.Float;
            }
             
            if (valor is Single)
            {
                return SqlDbType.Real;
            }
 
            if (valor is Guid)
            {
                return SqlDbType.UniqueIdentifier;
            }
 
            if (valor is Int16)
            {
                return SqlDbType.SmallInt;
            }
 
            if (valor is Int32)
            {
                return SqlDbType.Int;
            }
 
            if (valor is Int64)
            {
                return SqlDbType.BigInt;
            }
 
            if (valor is string)
            {
                return SqlDbType.NVarChar;
            }
 
            return SqlDbType.Text;
        }
         
        //Insert con parámetros Sql.
        public int Insert(string tabla, string[] campos, Object[] valores)
        {//Los parámetros evitan la Inyección Sql porque tratan su contenido como un literal, y las
         //instrucciones inyectadas Sql no se ejecutan en el servidor.
 
            string sentencia = "Insert into " + tabla;
            sentencia += string.Format(" ({0}) ", string.Join(",", campos));
 
            sentencia += "values (";
            for (int ind = 0; ind < campos.Length; ind++)
            {//El nombre del parámetro es igual al de  su campo correspondiente con la @ al principio.
                sentencia += "@" + campos[ind];
                sentencia+=(ind==campos.Length-1) ? ") " : ", ";
            }
 
            sentencia += "select @@Identity";
 
            using (SqlCommand cmd = new SqlCommand(sentencia))
            {
                //Parámetros Sql:
                for (int ind = 0; ind < valores.Length; ind++)
                {
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@" + campos[ind];
                    param.SqlDbType = TipoNetToSql(valores[ind]);
                    param.Value = valores[ind];
                    cmd.Parameters.Add(param);
                }
 
                //Al usar el "select @@identity", Ejecuta() devuelve el "id" del nuevo registro.
                FilasDB filas = Ejecuta(cmd);
                if (filas.Count > 0)
                {//Si no se pudo insertar el registro, filas no tienen ninguna fila.
                    return Convert.ToInt32(filas[0][0]);
 
                }
                else
                {//Si no se pudo insertar el registro en la tabla, se devuelve id -1.
                    return -1;
                }
            }
        }
         
        //Update con parámetros sql.
        public int Update(string tabla, string[] campos, Object[] valores, params CadenaParametro[] condicion)
        {//Los parámetros evitan la Inyección Sql porque tratan su contenido como un literal, y las
         //instrucciones inyectadas Sql no se ejecutan en el servidor.
 
            string cadenaCondicion = string.Empty;
            foreach (CadenaParametro stringParametro in condicion)
            {//Se crea la condición concatenando todos los objetos 'CadenaParametro' por si la condición incluye una comparación
                //con más de 1 campo.
                cadenaCondicion += stringParametro.ToString() + " ";
            }
 
            if(cadenaCondicion == string.Empty)
            {//Si no hay una condición en la instrucción de modificación, se modifican todos los registros/filas para la tabla,
             //y hay que evitar que eso se produzca por error.
                return 0;
            }
 
            string sentencia = "Update " + tabla + " set ";
            for (int ind = 0; ind < campos.Length; ind++)
            {//Se usa el mismo nombre del campo para su parámetro correspondiente pero con la @ delante. 
                sentencia += campos[ind] + "=@" + campos[ind];
                sentencia += (ind == (campos.Length - 1)) ? " " : ", ";
            }
 
            sentencia += "where " + cadenaCondicion + " ";
            sentencia += "select @@rowcount";
            //Al usar el "select @@rowcount", Ejecuta() devuelve el nº de registros afectados.
 
            using (SqlCommand cmd = new SqlCommand(sentencia))
            {
                //Parámetros Sql para los campos de la tabla:
                for (int ind = 0; ind < valores.Length; ind++)
                {
                    SqlParameter paramCampo = new SqlParameter();
                    paramCampo.ParameterName = "@" + campos[ind];
                    paramCampo.SqlDbType = TipoNetToSql(valores[ind]);
                    paramCampo.Value = valores[ind];
                    cmd.Parameters.Add(paramCampo);
                }
 
                //Parámetros Sql para la Condición:
                foreach(CadenaParametro parametro in condicion)
                {
                    SqlParameter paramCondicion = new SqlParameter();
                    paramCondicion.ParameterName = parametro.NombreParametro;
                    paramCondicion.SqlDbType = TipoNetToSql(parametro.ValorParametro);
                    paramCondicion.Value = parametro.ValorParametro;
 
                    cmd.Parameters.Add(paramCondicion);
                }
 
                FilasDB filas = Ejecuta(cmd);
                if (filas.Count > 0)
                {
                    return Convert.ToInt32(filas[0][0]);
                }
                else
                {
                    return 0;
                }
            }
        }
 
        /// <summary>
        /// Este método elimina de la base de datos uno o más registros.
        /// </summary>
        /// <param name="tabla">La tabla de la que se quieren borrar los registros.</param>
        /// <param name="condicion">Indica que registro/s se quieren borrar.</param>
        /// <returns>Devuelve el nº de registros eliminados de la tabla en la B.D.</returns>
        public int Delete(string tabla, params CadenaParametro[] condicion)
        {
            string cadenaCondicion = string.Empty;
            foreach (CadenaParametro stringParametro in condicion)
            {//Se crea la condición concatenando todos los objetos 'CadenaParametro' por si la condición incluye una comparación
                //con más de 1 campo.
                cadenaCondicion += stringParametro.ToString() + " ";
            }
 
            if(cadenaCondicion == "")
            {//Si no hay condición, se borran todos los registros/filas de la B.D. y no se puede dejar que eso pase por error.
                return 0;
            }
 
            string sentencia = "Delete from " + tabla + " ";
            sentencia += "where " + cadenaCondicion;
            sentencia += "select @@rowcount";
            //Al usar el "select @@rowcount", Ejecuta() devuelve el nº de registros afectados.
 
            using (SqlCommand cmd = new SqlCommand(sentencia))
            {
                //Se añaden los Parámetros SQL de la condicion a la consulta:
                foreach (CadenaParametro parametro in condicion)
                {
                    SqlParameter paramSql = new SqlParameter();
                    paramSql.ParameterName = parametro.NombreParametro;
                    paramSql.SqlDbType = TipoNetToSql(parametro.ValorParametro);
                    paramSql.Value = parametro.ValorParametro;
 
                    cmd.Parameters.Add(paramSql);
                }
 
                FilasDB filas = Ejecuta(cmd);
 
                if (filas.Count > 0)
                {
                    return Convert.ToInt32(filas[0][0]);
                }
                else
                {
                    return 0;
                }
            }
        }
 
        /// <summary>
        /// Este método realiza una consulta en la B.D.
        /// </summary>
        /// <param name="tabla">La tabla en la que se realiza la consulta.</param>
        /// <param name="campos">Nombre de los campos que se quieren consultar.</param>
        /// <param name="condicion">Indica que registros de la tabla se quieren consultar.</param>
        /// <param name="orderCampo">Indica el campo por el que se ordena la consulta.</param>
        /// <returns>Devuelve una Lista de Array de Objects. En cada fila de la lista, hay un array
        /// de Objects, cada Object es un campo del registro, y cada fila de la lista es un registro de
        /// la tabla.</returns>
        public FilasDB Select(string tabla, string[] campos, string orderCampo = "", params CadenaParametro[] condicion)
        {
            FilasDB filas = new FilasDB();
            string cadenaCondicion = string.Empty;
 
            foreach(CadenaParametro stringParametro in condicion)
            {//Se crea la condición concatenando todos los objetos 'CadenaParametro' por si la condición incluye una comparación
             //con más de 1 campo.
                cadenaCondicion += stringParametro.ToString() + " ";
            }
 
 
            string sentencia = "Select " + string.Join(", ", campos);
            sentencia += " from " + tabla;
            sentencia += cadenaCondicion != string.Empty ? " where " + cadenaCondicion : cadenaCondicion;
            sentencia += orderCampo != string.Empty ? " order by " + orderCampo : orderCampo;
 
            using (SqlCommand cmd = new SqlCommand(sentencia))
            {
                //Se añaden los Parámetros SQL de la condicion a la consulta:
                foreach(CadenaParametro parametro in condicion)
                {
                    SqlParameter paramSql = new SqlParameter();
                    paramSql.ParameterName = parametro.NombreParametro;
                    paramSql.SqlDbType = TipoNetToSql(parametro.ValorParametro);
                    paramSql.Value = parametro.ValorParametro;
 
                    cmd.Parameters.Add(paramSql);
                }
 
                //Al hacer la consulta Ejecuta() devuelve una lista con todas las filas seleccionadas.
                //Cada fila es una Array de Objects donde cada Object es un campo del registro/fila.
                filas = Ejecuta(cmd);
 
                //Para los campos que son Nullables en la B.D. le asigno el valor null en su respectiva referencia a objeto.
                foreach (Object[] registro in filas)
                {
                    for (int ind = 0; ind < registro.Length; ind++)
                    {
                        if (registro[ind] is DBNull)
                        {
                            registro[ind] = null;
                        }
                    }
                }
            }
 
                return filas;
        }
              
        /// <summary>
        /// Este método realiza una consulta en la B.D. en la tabla indicada com parámetro pero selecciona solo
        /// aquellos registros indicados por su posición en dicha tabla. Permitiendo así poder seleccionar
        /// registros por medio de paginación.
        /// </summary>
        /// <param name="posIni">Nº de fila o posición del primer registro que encabeza la lista a devolver.</param>
        /// <param name="posFin">Nº de fila o posición del último registro de la lista a devolver.</param>
        /// <param name="tabla">Nombre de la tabla en la B.D.</param>
        /// <param name="condicion">Indica la condición que deben cumplir aquellos campos que se van a enumerar.</param>
        /// <param name="campos">Array de strings con los nombres de los campos en la B.D.</param>
        /// <param name="orderAsc">Indica como se deben ordenar los campos: true:Menor a Mayor. false:Mayor a Menor.</param>
        /// <param name="orderCampo">Indica el campo de la tabla por el que se deben ordenar los registros/filas.</param>
        /// <returns>Lista de Array de 'Object' con los valores de los campos de los registros/filas
        /// cuya posición en la tabla de la B.D. va desde la fila 'posIni' a 'posFin'.</returns>
        public FilasDB SelectPage(string tabla, string[]campos, int posIni, int posFin, string condicion="", string orderCampo="", bool orderAsc=true)
        {/*El Row_Number() crea un campo con el nº de fila que ocupa el registro en la consulta
           para poder usar dicha consulta como una subQuery y de ella escoger ciertos registros
           haciendo Paginación.*/
             
            if(orderCampo=="")
            {//No se ordena  por ningún campo en particular para calcular el ROW_NUMBER.
                orderCampo="(SELECT 1)";
            }
 
            string nombreCampos = string.Join(", ", campos);
            string dirOrdenacion=(orderAsc==true)?"asc":"desc";
            condicion = (condicion != "") ? "where " + condicion : string.Empty;
            string sentencia = "SELECT "+ nombreCampos + " from " +
                             "( SELECT ROW_NUMBER() OVER(order by " + orderCampo + " " + dirOrdenacion + ") AS [Posicion], " + nombreCampos +
                             " FROM " + tabla + " AS [tabla] "+condicion+ " )" +
                             "AS SubQuery1 " +
                             "where (Posicion>=" + posIni + " AND Posicion<=" + posFin + ")";
 
 
            //Al hacer la consulta Ejecuta() devuelve una lista con todas las filas seleccionadas.
            //Cada fila es una Array de Objects donde cada Object es un campo del registro/fila.
            FilasDB filas = Ejecuta(sentencia);
 
            //Para los campos que son Nullables en la B.D. le asigno el valor null en su respectiva referencia a objeto.
            foreach (Object[] registro in filas)
            {
                for (int ind = 0; ind < registro.Length; ind++)
                {
                    if (registro[ind] is DBNull)
                    {
                        registro[ind] = null;
                    }
                }
            }
            return filas;
        }
 
        /// <summary>
        /// Este método devuelve el nº total de filas/registros de una tabla en la B.D.
        /// </summary>
        /// <param name="tabla">El nombre de la tabla en la B.D.</param>
        /// <param name="condicion">Condición para determinar que filas se deben contar o no.</param>
        /// <returns>Nº de filas o registros de la tabla</returns>
        public long TotalFilas(string tabla, string condicion="")
        {
            long nfilas=0;
 
            condicion=condicion!=string.Empty?"where "+condicion:string.Empty;
            string sentencia="SELECT COUNT(*) FROM " + tabla + " " + condicion;
 
            FilasDB filas = Ejecuta(sentencia);
 
            if (filas.Count > 0)
            {
                nfilas=Convert.ToInt64(filas[0][0]);
            }
 
            return nfilas;
        }
 
        #region Metodo Select Foto
        public byte[] SelectFoto(string campo, string tabla, string condicion)
        {
            string comando = string.Format("Select {0} from {1} where {2}",
                                            campo, tabla, condicion);
          
            SqlCommand cm = new SqlCommand(comando, conex);
 
            try
            {
                using (SqlDataReader dr = cm.ExecuteReader())
                {
                    if (dr.Read())
                        if (dr[0] != DBNull.Value)
                            return (byte[])dr[0];
                        else
                            return null;
                    else
                        return null;
                }
            }
            catch (SqlException ex)
            {
                GestionExcepcionWeb ge = new GestionExcepcionWeb(ex);
                ge.Log();
                return null;
            }
        }
        #endregion
 
        #region Metodo Update Foto
        public bool UpdateFoto(string campo, string tabla, string condicion, byte[] valorBin)
        {
            string comando = string.Format("Update {0} set {1} = @pic where {2}",
                                            tabla, campo, condicion);
            using (SqlCommand cm = new SqlCommand(comando, conex))
            {
                SqlParameter spm = new SqlParameter("@pic", System.Data.SqlDbType.Image);
                spm.Value = valorBin;
                cm.Parameters.Add(spm);
                try
                {
                    cm.ExecuteNonQuery();
                    return true;
                }
                catch (SqlException ex)
                {
                    GestionExcepcionWeb ge = new GestionExcepcionWeb(ex);
                    ge.Log();
                    return false;
                }
            }
        }
 
        #endregion
 
        public bool CheckSqlInyection(object[] _campos, string _condicion)
        {
            string campo;
            bool peligro = false;
 
            if ((_condicion.Contains(';')) || (_condicion.Contains("--")) || (_condicion.Contains("/*")) ||
               (_condicion.Contains("*/")) || (_condicion.Contains("xp_")) || (_condicion.Contains("XP_")))
            {
                peligro = true;
                GestionExcepcionWeb.WriteLog("***PELIGRO: INTENTO DE ATAQUE INYECCIÓN SQL:***");
                GestionExcepcionWeb.WriteLog("Valor Condicíon SQL: " + _condicion);
            }
 
            if ((_campos != null)&&(!peligro))
            {
                foreach (object item in _campos)
                {
                    campo = item.ToString();
                    if ((campo.Contains(';')) || (campo.Contains("--")) || (campo.Contains("/*")) ||
                       (campo.Contains("*/")) || (campo.Contains("xp_")) || (campo.Contains("XP_"))
                        || (campo.Contains('\'')))
                    {
                        peligro = true;
 
                        GestionExcepcionWeb.WriteLog("***PELIGRO: INTENTO DE ATAQUE INYECCIÓN SQL:***");
                        GestionExcepcionWeb.WriteLog("Valor Campo SQL: " + campo);
 
                        break;
                    }
                }
            }
 
            return peligro;
        }
 
 
        #region Miembros de IDisposable
        /// <summary>
        /// Este método se llama al declarar el objeto del tipo "AccesoSql" dentro
        /// de una sentencia "using()" cuando finaliza la ejecución de dicha sentencia o
        /// cuando se sale de ella (return, throw, break...)
        /// </summary>
        public void Dispose()
        {
            CloseConnection();
        }
 
        public void CloseConnection()
        {
            try
            {
                conex.Close();
            }
            catch (SqlException ex)
            {
                GestionExcepcionWeb ge = new GestionExcepcionWeb(ex);
                ge.Log();
            }
        }
        #endregion
    }
 
    public delegate void DelExcepcion(string excepcion); //Es un tipo de Dato.
 
    public class GestionExcepcionWeb
    {
        private Exception Excepcion { get; set; }
        private string  Error { get; set; }
        public static event DelExcepcion OnExcepcion;
        private string nombreArchivo;
 
        public GestionExcepcionWeb(Exception excepcion, string _nombreArchivo="")
        {
            Excepcion = excepcion;
            nombreArchivo = _nombreArchivo;
        }
 
        public GestionExcepcionWeb(string error, string _nombreArchivo="")
        {
            Error = error;
            nombreArchivo = _nombreArchivo;
        }
 
        public bool Log()
        {
            bool todoOk = true; //Se devuelve 'true' si se pudo escribir el archivo de log en el disco duro.
            string texto="";
            //Es ASP.NET, el path del sitio web se obtiene con el método MapPath(). Porque se ejecuta la carpeta del Servidor Web IIS.
            string filename = HttpContext.Current.Server.MapPath("~") + "\\Logs";
 
            try
            {//Se crea el archivo .log con el error.
                if (!Directory.Exists(filename))
                {//Si no existe el directorio para los Logs, lo creo:
                    DirectoryInfo dirInfo = new DirectoryInfo(filename);
                    dirInfo.Create();
                }
 
                //Nombre del Archivo:
                if (nombreArchivo != string.Empty)
                {//Si se recibió nombre de archivo en el constructor de la clase, se usa ese.
                    filename += "\\" + nombreArchivo;
                }
                else
                {//Se crea un archivo a partir de la fecha actual.
                    filename += @"\Log" + DateTime.Now.ToString("yyyyMMdd") + ".log";
                }
 
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filename, true))
                {
                    texto = DateTime.Now.ToString("HH:mm - ");
                    if (Error != string.Empty)
                    {
                        texto += Error;
                    }
 
                    if (Excepcion != null)
                    {
                        texto += Excepcion.Message;
                    }
                    sw.WriteLine(texto);
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                //No se hace. Por si no hay permisos de escritura para el archivo del log. No se escribe y
                //el flujo del programa sigue con normalidad.
                todoOk=false;
            }
 
            if (OnExcepcion != null)
            {//Se ejecuta el método suscrito al evento, normalmente par visualizar el error.
                OnExcepcion(texto);
            }
 
            return todoOk;
        }
 
        public static bool WriteLog(string texto, string _nombreArchivo="")
        {
            GestionExcepcionWeb ge = new GestionExcepcionWeb(texto, _nombreArchivo);
            return ge.Log();
        }
 
        public static bool WriteLog(Exception ex, string _nombreArchivo="")
        {
            GestionExcepcionWeb ge = new GestionExcepcionWeb(ex, _nombreArchivo);
            return ge.Log();
        }
    }
 
}