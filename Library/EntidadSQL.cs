//Programmed by: Silverio Martinez Garcia - http://www.atopecode.net -

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using GestionSql;
using System.Runtime.Serialization;
using System.Data.SqlClient;
using System.Data;
 
namespace GestionSql
{  
    /// <summary>
    /// Esta clase abstracta está diseñada para ser heredada por una clase que represente una tabla en
    /// una B.D. SQL. 
    /// </summary>
    [DataContract]
    public abstract class EntidadSQL
    {
        /// <summary>
        /// Indica como se debe de realizar la conexión con la B.D. Según como se creó el objeto que hereda de 'Entidad'.
        /// La enumeración es un tipo de dato, pero la declaro dentro de la clase porque fuera de ella no se utiliza.
        /// </summary>
        public enum TipoConexion { Manual, ArchivoConfiguracion };
 
        private Type tipoClase;
        private static string dataSource, initialCatalog, userID, password; //Campos para conexión Manual a la B.D.
        private static string nombreConnectionStringWebConfig = "conex"; //Campo para conexión a la B.D. por medio de una cadena en el archivo de configuración de la App.
        private static TipoConexion tipoConex = TipoConexion.ArchivoConfiguracion;
 
         ///<summary>
         ///Esta propiedad hace uso de la reflexión para buscar aquel campo/propiedad de 
         ///la clase descendiente de 'Entidad' que implementa el atributo '[CampoBD(esIndice=true)]' 
         ///y poder leer y escribir en él. Ya que en esta clase abstracta 'Entidad' no se conoce dicho campo.
         ///<para>
         ///Excepciones: Si el campo definido como Identificador en la clase Entidad no es de tipo 'int'.
         ///             O si no está definido ningún campo de la clase Entidad como Identificador, o si hay
         ///             más de 1 campo Índice.
         ///</para>
         ///</summary>
        private int CampoID
        {
            get
            {
                int valor;
                Object objectValor;
                Object infoID = InfoCampoID();
                if (infoID is FieldInfo) { objectValor = (infoID as FieldInfo).GetValue(this); }
                else { objectValor = (infoID as PropertyInfo).GetValue(this,null); }
                 
                try
                {//Por si el campo que se estableció como Identificador en la clase derivada no es int.
                    valor = (int)objectValor;
                }
                catch (Exception ex)
                {
                    throw new Exception("El campo Índice de la clase Entidad " + NombreClase() + " debe ser de tipo 'int'"); 
                }
                return valor;
            }
 
            set
            {
                Object infoID = InfoCampoID();
 
                try
                {//Por si el campo que se estableció como Identificador en la clase derivada no es int.
                    if (infoID is FieldInfo) { (infoID as FieldInfo).SetValue(this, value); }
                    else { (infoID as PropertyInfo).SetValue(this, value, null); }
                }
                catch (Exception ex)
                {
                    throw new Exception("El campo Índice de la clase Entidad " + NombreClase() + " debe ser de tipo 'int'"); 
                }
            }
        }
 
        /// <summary>
        /// Este constructor crea el objeto 'Entidad' con el nombre de la tabla que tiene asociada en la B.D.
        /// La conexión con el servidor SQL se realizará por medio de un archivo de configuración de aplicaciones
        /// con la etiqueta "connectionStrings" que contenga la cadena name="conex" con la información necesaria para
        /// realizar la conexión.
        /// </summary>
        /// <param name="_tabla">El nombre de la tabla en la B.D. que corresponde con clase Entidad.</param>
        public EntidadSQL()
        {//Este constructor solo existe para poder usarlo desde la clase descendiente, porque esta clase es abstracta.
         //Esta clase es abstracta y no se pueden declarar objetos de ella.
         //Al crear una clase que hereda de 'Entidad', primero se llama a este constructor, y luego al de la
         //clase derivada.
            tipoClase = this.GetType();
            CampoID = -1;
            TransactionSql = null;
        }
 
 
        /// <summary>
        /// Método Abstracto (Virtual de definición obligatoria) que se debe definir en la clase derivada para
        /// devolver el nombre de la tabla correspondiente a la 'Entidad' en la B.D.
        /// </summary>
        /// <returns>Nombre de la tabla relacionada con la clase derivada de 'Entidad'.</returns>
        public abstract string GetTabla();
 
        /// <summary>
        /// Método que devuelve el nombre de la clase que hereda de 'Entidad'.
        /// </summary>
        /// <returns>El nombre de la clase.</returns>
        public string NombreClase()
        {
            return tipoClase.Name;
        }
 
        #region Conexion SQL
        /// <summary>
        /// Método static que establece los valores de la cadena de conexión a la B.D. SQL para una conexión manual.
        /// Es un método de la clase 'Entidad' porque es static, y por lo tanto es común a todas las clases
        /// derivadas de 'Entidad'.
        /// </summary>
        /// <param name="_dataSource">Dirección ip del servidor SQL\Nombre estancia SQL.</param>
        /// <param name="_initialCatalog">Nombre de la Base de Datos en el servidor.</param>
        /// <param name="_userID">Nombre de usuario para establecer la conexión.</param>
        /// <param name="_password">Password del usuario para establecer la conexión.</param>
        public static void ConfigConex(string _dataSource, string _initialCatalog, string _userID, string _password)
        {
            dataSource = _dataSource;
            initialCatalog = _initialCatalog;
            userID = _userID;
            password = _password;
 
            tipoConex = TipoConexion.Manual;
        }
 
        /// <summary>
        /// Método static que establece una conexión a la B.D. por medio de una cadena de conexión que está en el archivo de configuración
        /// de la Aplicación.
        /// Es un método de la clase 'Entidad' porque es static, y por lo tanto es común a todas las clases
        /// derivadas de 'Entidad'. 
        /// </summary>
        /// <param name="_nombreConnectionStringWebConfig">Nombre del campo 'ConnectionStrings' del archivo de configuración que
        /// contiene la cadena de conexión a la B.D.</param>
        public static void ConfigConex(string _nombreConnectionStringWebConfig)
        {
            nombreConnectionStringWebConfig = _nombreConnectionStringWebConfig;
            tipoConex = TipoConexion.ArchivoConfiguracion;
        }
 
        /// <summary>
        /// Método static que establece que la conexión de todas las clases 'Entidades' con el servidor SQL
        /// se realizará por medio de la información descrita en un archivo de configuración de aplicaciones o 
        /// por medio del valor que se le asignen a las variables de conexión con el método 
        /// ConfigConex(dataSource,initialCatalog,userID,password).
        /// <param name="_tipoConexion">
        /// Manual.- Se toma el valor de la variables de conexión que tiene la clase 'Entidad'. Se modifican dichas
        /// variables con la otra sobrecarga de este método.
        /// ArchivoConfiguracion.- Se toma el valor para la cadena de conexión de un arhivo de configuración de aplicación
        /// que contenga la línea 'name="conex"' dentro de la etiqueta 'connectionStrings'.
        /// </param>
        /// 
        /// </summary>
        public static void ConfigConex(TipoConexion _tipoConexion)
        {
            tipoConex = _tipoConexion;
        }
 
        //Se usa este campo para compartir conexión (y misma transacción) entre diferentes operaciones en la B.D.
        //Si un objeto que herede de 'EntidadSQL' no tiene un objeto 'TransactionSql' en este campo, o sea, que vale 'null',
        //entonces para cada operación Sql (Insert, Update, Select y Delete) se creará un objeto local 'AccesoSql' en dicho
        //método y se abrirá y cerrará una conexión por cada operación o método. Si se quiere realizar una Transacción Sql entre
        //varias operaciones, todos los objetos 'EntidadSQL' de la transacción deben compartir la misma conexión, para eso se utiliza
        //este campo.
        private TransactionSQL transactionSql;
        public TransactionSQL TransactionSql
        {
            get
            {
                return transactionSql;
            }
 
            set
            {
                transactionSql = value;
            }
        }
 
        public static AccesoSql CrearAccesoSQL()
        {//Crea y devuelve el objeto 'AccesoSQL' según si los datos para la conexión al servidor SQL están en
            //un archivo de configuración de la aplicación o se recibieron en el constructor de la clase 'Entidad'.
            AccesoSql asql;
 
            if (tipoConex == TipoConexion.Manual)
            {
                asql = new AccesoSql(dataSource, initialCatalog, userID, password);
            }
            else
            {
                asql = (nombreConnectionStringWebConfig=="") ? new AccesoSql() : new AccesoSql(nombreConnectionStringWebConfig);
            }
 
            return asql;
        }
        #endregion
 
        #region CampoID Reflexion
        /// <summary>
        /// Este método devuelve la información de Reflexión de aquel campo de la clase derivada de 'Entidad' que
        /// implemente el atributo [CampoBD(esIndice=true)] indicando que ese campo es el Índice de la tabla en la B.D.
        /// <para>Excepciones: Se lanza una excepction si la clase derivada de 'Entidad' tiene más de un campo Identificador 
        /// o sino tiene ninguno.
        /// </para>
        /// </summary>
        /// <returns>Object que puede ser un FieldInfo o un PropertyInfo.</returns>
        private Object InfoCampoID()
        {
            Object infoCampoID = null;
            int nids = 0; //Para contar cuantos campos índice se encuentran, solo puede haber 1.
 
            Object[] camposBD = InfoCamposBD(true);
            foreach (Object item in camposBD)
            {
                Object[] atributos;
                atributos = item is FieldInfo ? (item as FieldInfo).GetCustomAttributes(false) : (item as PropertyInfo).GetCustomAttributes(false);
                foreach (Object claseAtributo in atributos)
                {
                    if (claseAtributo is CampoBD)
                    {
                        if (((CampoBD)claseAtributo).esIndice)
                        {
                            infoCampoID = item; //Es un objeto de la clase 'FieldInfo' o 'PropertyInfo'.
                            nids = nids + 1;
                        }
                    }
                }
            }
 
            if (nids == 0)
            {
                throw new Exception("La clase Entidad " + NombreClase() + " debe tener un campo Identificador que implemente el atributo" +
                                    "[CampoBD(esIndice=true)] y debe ser de tipo 'int'");
 
            }
            if (nids > 1)
            {
                throw new Exception("La clase Entidad " + NombreClase() + " tiene más de un campo Identificador, solo puede haber 1.");
            }
 
            return infoCampoID;
        }
 
        /// <summary>
        /// Este método devuelve el nombre del campo Identificador que implementa el atributo '[CampoBD(esIndice=true)]' en la
        /// clase derivada de 'Entidad'.
        /// <para>Excepciones: Se lanza una excepction si la clase derivada de 'Entidad' tiene más de un campo Identificador 
        /// o sino tiene ninguno.
        /// </para>
        /// </summary>
        /// <returns>El nombre del campo Identificador de la Entidad.</returns>
        private string NombreCampoID()
        {
            Object infoID = InfoCampoID();
            string nombreID;
 
            if (infoID is FieldInfo)
            {//Si el 'id' de la clase descendiente de 'Entidad' es un campo:
                nombreID = ((FieldInfo)infoID).Name;
            }
            else
            {//Si el 'id' de la clase descendiente de 'Entidad' es una propiedad:
                nombreID = ((PropertyInfo)infoID).Name;
            }
 
            return nombreID;
        }
        #endregion
 
        #region CamposBD Reflexion
        /// <summary>
        /// Este método devuelve un array de Object que contiene los objetos 'FieldInfo' y/o 'PropertyInfo' (Reflexión)
        /// de la clase que hereda de 'Entidad' para aquellos campos que implementen el atributo 'CampoBD'.
        /// </summary>
        /// <param name="conId">Indica si se incluye la info para</param>
        /// <returns>El array de object con la info (Reflexión) de los campos/propiedades de la subclase, que sean campos de la BD</returns>
        private Object[] InfoCamposBD(bool conId = false)
        {
            FieldInfo[] camposInfo = tipoClase.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo[] propInfo = tipoClase.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
             
            //La lista 'infoCamposBD' contiene objects que serán de la clase 'FieldInfo' para los campos y 'PropertyInfo'
            //para las propiedades, haciendo uso del polimorfismo. A la hora de utilizar la lista que devuelve este método
            //habrá que comprobar en cada caso a que clase corresponde cada 'object'.
            List<Object> infoCamposBD = new List<Object>();
 
            for (int ind = 0; ind < camposInfo.Length; ind++)
            {//Se comprueba los CAMPOS de la clase que implementen el atributo 'CampoBD'.
                object[] atributos = camposInfo[ind].GetCustomAttributes(false);
                foreach (object item in atributos)
                {
                    if (item is CampoBD)
                    {//Si el atributo del campo es del tipo 'CampoBD' entonces se añade el nombre del campo a la lista.
                        if (conId)
                        {//Se añaden todos los campos.
                            infoCamposBD.Add(camposInfo[ind]);
                        }
                        else
                        {//No se añade el campo Índice de la tabla/entidad.
                            if (!(item as CampoBD).esIndice)
                            {
                                infoCamposBD.Add(camposInfo[ind]);
                            }
                        }
                        break;
                    }
                }
            }
 
            for (int ind = 0; ind < propInfo.Length; ind++)
            {//Se comprueba las PROPIEDADES de la clase que implementen el atributo 'CampoBD'.
                object[] atributos = propInfo[ind].GetCustomAttributes(false);
                foreach (object item in atributos)
                {
                    if (item is CampoBD)
                    {//Si el atributo del campo es del tipo 'CampoBD' entonces se añade el nombre del campo a la lista.
                        if (conId)
                        {//Se añaden todos los campos.
                            infoCamposBD.Add(propInfo[ind]);
                        }
                        else
                        {//No se añade el campo Índice de la tabla/entidad.
                            if (!(item as CampoBD).esIndice)
                            {
                                infoCamposBD.Add(propInfo[ind]);
                            }
                        }
                        break;
                    }
                }
            }
 
            return infoCamposBD.ToArray();
        }
 
        /// <summary>
        /// Este método devuelve el nombre de los campos/propiedades que implementan el atributo 'CampoBD'
        /// que indica de dichos campos corresponden con los del tabla en la base de datos SQL.
        /// </summary>
        /// <param name="conId">Indica si se quiere incluir en la lista el nombre del campo que es el índice
        /// de la tabla/entidad SQL.</param>
        /// <para>
        /// Excepciones: Los campos de la clase Entidad que implementen el atributo 'CampoBD', deben llamarse 
        /// igual que los de la tabla de la B.D. Respetanto mayúsculas y minúsculas. El orden no tiene porque 
        /// ser el mismo.
        /// </para>
        /// <returns>Array de strings con los nombres de los campos/propiedades que pertenecen a la correspondiente 
        /// tabla en la B.D.</returns>
        public string[] NombresCamposBD(bool conId=false)
        {
            //Al usar el método 'InfoCamposBD' al igual que en el método 'ValoresCamposBD', me aseguro que
            //el orden de nombre y valores es el mismo para los campos/propiedades de la entidad/tabla.
            Object[] infoCampos = InfoCamposBD(conId);
            string[] camposBD = new string[infoCampos.Length];
 
            for (int ind = 0; ind < camposBD.Length; ind++)
            {
                if (infoCampos[ind] is FieldInfo)
                {//Es la info de un campo, no de una propiedad.
                    camposBD[ind] = ((FieldInfo)infoCampos[ind]).Name;
                }
                else
                {//Es la info de una propiedad, no de un campo.
                    camposBD[ind] = ((PropertyInfo)infoCampos[ind]).Name;
                }
            }
            return camposBD;
        }
 
        /// <summary>
        /// Este método devuelve el valor de los campos/propiedades que implementan el atributo 'CampoBD'
        /// que indica de dichos campos corresponden con los del tabla en la base de datos SQL.
        /// </summary>
        /// <param name="conId">Indica si se quiere incluir en la lista el nombre del campo que es el índice
        /// de la tabla/entidad SQL.</param>
        /// <returns>Array de strings con los nombres de los campos/propiedades que pertenecen a la correspondiente 
        /// tabla en la B.D.</returns>
        public object[] ValoresCamposBD(bool conId=false)
        {
            //Al usar el método 'InfoCamposBD' al igual que en el método 'NombreCamposBD', me aseguro que
            //el orden de nombre y valores es el mismo para los campos/propiedades de la entidad/tabla.
            Object[] infoCamposBD = InfoCamposBD(conId);
            object[] valoresCamposBD = new object[infoCamposBD.Length];
 
            for (int ind = 0; ind < valoresCamposBD.Length; ind++)
            {
                if (infoCamposBD[ind] is FieldInfo)
                {//Es la info de un campo.
                    valoresCamposBD[ind] = ((FieldInfo)infoCamposBD[ind]).GetValue(this);
                }
                else
                {//Es la info de una propiedad.
                    valoresCamposBD[ind] = ((PropertyInfo)infoCamposBD[ind]).GetValue(this, null);
                }
            }
            return valoresCamposBD;
        }
 
        /// <summary>
        /// Este método Asigna los valores recibidos como parámetros a los campos de la clase Entidad que implementan
        /// el atributo '[CampoBD]'.
        /// <para>
        /// Excepciones: Se lanza excepción si el número de valores no coincide con el número de campos de la
        /// clase Entidad, o si se produce error en la conversión de tipos de algún campo.
        /// </para>
        /// </summary>
        /// <param name="campos">Array de objects con los valores a asignar. Deben de estar en el mismo orden
        /// que los devuelve el metodo NombresCamposBD().</param>
        /// <param name="conId">Indica si entre los valores recibidos como parámetros está incluido el campo
        /// Identificador de la Entidad.</param>
        public void AsignarValorCamposBD(Object[] campos, bool conId=false)
        {
            Object[] infoCampos = InfoCamposBD(conId);
 
            if (infoCampos.Length != campos.Length)
            {//Si hay distinta cantidad de campos que de valores, se lanza una excepcion.
                throw new Exception("No coinciden el número de campos y valores que se quieren asignar en la Entidad "+NombreClase());
            }
 
            try
            {
                for (int ind = 0; ind < infoCampos.Length; ind++)
                {
                    if (infoCampos[ind] is FieldInfo)
                    {//Es la info de un campo obtenida por reflexión.
                        (infoCampos[ind] as FieldInfo).SetValue(this, campos[ind]);
                    }
                    else
                    {//Es la info de una propiedad obtenida por reflexión.
                        (infoCampos[ind] as PropertyInfo).SetValue(this, campos[ind], null);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en la conversión al asignar valores a los campos de la Entidad " + NombreClase());
            }
        }
 
        /// <summary>
        /// Devuelve una cadena con los nombres y valores del objeto 'Entidad'.
        /// </summary>
        /// <returns>Nombre Campo: Valor Campo</returns>
        public override string ToString()
        {
            Object[] nombreCampos = NombresCamposBD(true);
            Object[] valorCampos = ValoresCamposBD(true);
            string cadena=string.Empty;
 
            for (int ind = 0; ind < nombreCampos.Length; ind++)
            {
                cadena += string.Format("{0}: {1} | ", nombreCampos[ind], valorCampos[ind]);
            }
 
            return cadena;
        }
        #endregion
 
        #region GestionSQL
        /// <summary>
        /// Este método realiza una Insercción de la clase 'Entidad' actual en la B.D. si el campo 'Identificador'
        /// tiene valor -1, con lo cual una vez insertado el registro/entidad en la B.D. el campo 'Identificador' cambia
        /// de valor. O en caso de que el campo 'Identificador' tenga ya un valor, es decir, el registro ya existe la B.D.,
        /// se actualiza el valor de sus campos con los valores actuales de la clase 'Entidad'.
        /// <para>
        /// Excepciones: Los campos de la clase Entidad que implementen el atributo 'CampoBD', deben llamarse 
        /// igual que los de la tabla de la B.D. Respetanto mayúsculas y minúsculas. El orden no tiene porque 
        /// ser el mismo.
        /// </para>
        /// </summary>
        /// <returns>True.- Se realializó la Insercción o Actualización con éxito. False.- No se realizó la Insercción
        /// o actualizacion con éxito.</returns>
        public virtual bool Guardar()
        {
            if (CampoID == -1)
            {
                return Insertar();
            }
            else
            {
                return Actualizar();
            }
        }
 
        /// <summary>
        /// Este método inserta un registro/fila con los valores del objeto 'Entidad' actual en la B.D. y tabla
        /// correspondientes.
        /// </summary>
        /// <param name="_accesoSql">Si es distinto de 'null' se utiliza el objeto AccesoSql recibido como parámetro
        /// para ejecutar la instrucción, así se mantiene la misma conexión entre varias instrucciones Sql y se permite
        /// el uso de transacciones Sql entre distintos objetos 'EntidadSql'.</param>
        /// <returns>True.- Si se pudo realizar la insercción. False.- Si no se pudo realizar la insercción.</returns>
        protected virtual bool Insertar()
        {
            bool cerrarConexion = (transactionSql == null) ? true : false;
             
            AccesoSql asql = (transactionSql == null) ? CrearAccesoSQL() : transactionSql.accesoSql;
            CampoID = asql.Insert(GetTabla(), NombresCamposBD(false), ValoresCamposBD(false));
             
            if (cerrarConexion)
            {//Si existe un objeto 'TransactionSQL' para este objeto, esta instrucción sql forma parte de una transacción y 
                //comparte conexión con otras, por eso solo se cierra la conexión a la B.D. en caso contrario.
                asql.Dispose();
            }
 
            return (CampoID != -1);
        }
 
        /// <summary>
        /// Este método actualiza el registro/fila de la B.D. correspondiente a esta clase 'Entidad' (mismo Id)
        /// con los valores actuales del objeto.
        /// <para>
        /// Excepciones: Los campos de la clase Entidad que implementen el atributo 'CampoBD', deben llamarse 
        /// igual que los de la tabla de la B.D. Respetanto mayúsculas y minúsculas. El orden no tiene porque 
        /// ser el mismo.
        /// </para>
        /// <param name="_accesoSql">Si es distinto de 'null' se utiliza el objeto AccesoSql recibido como parámetro
        /// para ejecutar la instrucción, así se mantiene la misma conexión entre varias instrucciones Sql y se permite
        /// el uso de transacciones Sql entre distintos objetos 'EntidadSql'.
        /// </param>
        /// </summary>
        /// <returns>True.- Se pudo actualizar el registro/entidad. False.- No se pudo actualizar el registro/entidad.</returns>
        protected virtual bool Actualizar()
        {
            int nRegs;
            CadenaParametro condicion = new CadenaParametro(NombreCampoID(), "=", CampoID);
            bool cerrarConexion = (transactionSql == null) ? true : false;
 
            AccesoSql asql = (transactionSql == null) ? CrearAccesoSQL() : transactionSql.accesoSql;
            nRegs = asql.Update(GetTabla(), NombresCamposBD(false), ValoresCamposBD(false), condicion);
 
            if (cerrarConexion)
            {//Si existe un objeto 'TransactionSQL' para este objeto, esta instrucción sql forma parte de una transacción y 
                //comparte conexión con otras, por eso solo se cierra la conexión a la B.D. en caso contrario.
                asql.Dispose();
            }
 
            return (nRegs > 0);
        }
 
 
        /// <summary>
        /// Este método borra el registro/fila de la B.D. correspondiente a esta clase 'Entidad' (mismo Id).
        /// </summary>
        /// <param name="_accesoSql">Si es distinto de 'null' se utiliza el objeto AccesoSql recibido como parámetro
        /// para ejecutar la instrucción, así se mantiene la misma conexión entre varias instrucciones Sql y se permite
        /// el uso de transacciones Sql entre distintos objetos 'EntidadSql'.
        /// </param>
        /// <returns>True.- Se pudo borrar el registro de la B.D. False.- No se pudo borrar el registro en la B.D.</returns>
        public virtual bool Borrar()
        {
            int nRegs;
            CadenaParametro condicion = new CadenaParametro(NombreCampoID(), "=", CampoID);
            bool cerrarConexion = (transactionSql == null) ? true : false;
 
            AccesoSql asql = (transactionSql == null) ? CrearAccesoSQL() : transactionSql.accesoSql;
            nRegs = asql.Delete(GetTabla(), condicion);
 
            if (cerrarConexion)
            {//Si existe un objeto 'TransactionSQL' para este objeto, esta instrucción sql forma parte de una transacción y 
                //comparte conexión con otras, por eso solo se cierra la conexión a la B.D. en caso contrario.
                asql.Dispose();
            }
 
            return (nRegs > 0);
        }
 
        /// <summary>
        /// Este método devuelve el nº de filas en la B.D. de tabla que representa la clase 'Entidad'.
        /// <param name="condicion">Condición para las filas que se deben contar. Si no hay condición,
        /// se cuentan todas las filas de la Tabla/Entidad.</param>
        /// </summary>
        /// <returns>Nº de filas en la base de datos.</returns>
        public static long TotalFilas<T>(string condicion="") where T:EntidadSQL, new()
        {
            long nFilas = 0;
            T entidad = new T();
 
            using (AccesoSql asql = CrearAccesoSQL())
            {
                nFilas = asql.TotalFilas(entidad.GetTabla(), condicion);
            }
 
            return nFilas;
        }
 
        /// <summary>
        /// Método static y genérico que realiza una consulta en la tabla de la B.D. correspondiente al tipo
        /// de clase 'Entidad' recibida como parámetro genérico.
        /// </summary>
        /// <typeparam name="T">Clase derivada de 'EntidadSQL' y que defina un constructor por defecto.</typeparam>
        /// <param name="condicion">string con la condicion en formato SQL para seleccionar la/s filas
        /// de la tabla correspondiente en la B.D.</param>
        /// <param name="orderByCampo">Nombre del campo por el que se deben de ordenar la filas devueltas 
        /// por la consulta.</param>
        /// <returns>Objeto List del tipo genérico recibido como parámetro en el método."/></returns>
        protected static List<T> Listar<T>(string orderByCampo = "", params CadenaParametro[] condicion) where T : EntidadSQL, new()
        {
            List<T> filasConsulta=new List<T>();
            FilasDB filas;
 
            T entidad = new T();
            using (AccesoSql asql = CrearAccesoSQL())
            {
                filas = asql.Select(entidad.GetTabla(), entidad.NombresCamposBD(true), orderByCampo, condicion);
            }
 
            foreach (Object[] item in filas)
            {
                T newEntidad = new T();
                newEntidad.AsignarValorCamposBD(item, true);
                filasConsulta.Add(newEntidad); 
            }
 
            return filasConsulta;
        }
 
        /// <summary>
        /// Método static y genérico que realiza una consulta en la tabla de la B.D. corespondiente al tipo
        /// de la clase 'Entidad' recibida como parámetro genérico pero seleccionando solo aquellas filas cuya
        /// posición esté dentro de los parámetros 'posIni' y 'posFin'.
        /// </summary>
        /// <typeparam name="T">Clase derivada de 'EntidadSQL' y que defina un constructor por defecto.</typeparam>
        /// <param name="posIni">Indica la posición de la primera fila resultado de la consulta.</param>
        /// <param name="posFin">Indica la posición de la última fila resultado de la consulta.</param>
        /// <param name="condicion">Solo se enumerarán aquellas filas que cumplan la condición indicada. Si no hay 
        /// condición, se seleccionan todas las filas dentro del margen de posición indicado.</param>
        /// <param name="orderCampo">Nombre del campo de la tabla por el que se ordenarán las filas. Si es igual a
        /// "" no se ordenará por ningún campo y se respetará la posición de las filas en B.D.</param>
        /// <param name="orderAsc">True: Se ordenan las filas de menor a mayor por el campo indicado.
        /// False: Se ordenan las filas de mayor a menor por el campo indicado.</param>
        /// <returns></returns>
        protected static List<T> ListarPage<T>(int posIni, int posFin, string condicion = "", string orderCampo = "", bool orderAsc = true)
            where T:EntidadSQL, new()
        {
            List<T> filasConsulta = new List<T>();
            FilasDB filas;
 
            T entidad = new T();
            using (AccesoSql asql = CrearAccesoSQL())
            {
                filas = asql.SelectPage(entidad.GetTabla(), entidad.NombresCamposBD(true), posIni, posFin, condicion, orderCampo, orderAsc);
            }
 
            foreach (Object[] item in filas)
            {
                T newEntidad = new T();
 
                newEntidad.AsignarValorCamposBD(item, true);
                filasConsulta.Add(newEntidad);
            }
 
            return filasConsulta;
        }
 
        /// <summary>
        /// Método static y común a todas las clases 'Entidades' que actualiza un campo en la B.D. con una imagen
        /// en formato byte[].
        /// </summary>
        /// <param name="campo">Nombre del campo para la imagen</param>
        /// <param name="tabla">Nombre de la tabla en la B.D.</param>
        /// <param name="condicion">Condición en formato SQL de la fila/filas donde se quiere modificar la imagen.</param>
        /// <param name="image">La imagen en formato byte[].</param>
        /// <param name="_accesoSql">Si es distinto de 'null' se utiliza el objeto AccesoSql recibido como parámetro
        /// para ejecutar la instrucción, así se mantiene la misma conexión entre varias instrucciones Sql y se permite
        /// el uso de transacciones Sql entre distintos objetos 'EntidadSql'.
        /// </param>
        /// <returns>True.- Si se consiguió actualizar el campo de la imagen en la B.D.
        /// False.- Si no se consiguió actualizar el campo de la imagen en la B.D.</returns>
        protected static bool UpdateImage(string campo, string tabla, string condicion, byte[] image, AccesoSql _accesoSql = null)
        {
            bool todoOK;
            bool cerrarConexion = (_accesoSql == null) ? true : false;
             
            AccesoSql asql = (_accesoSql == null) ? CrearAccesoSQL() : _accesoSql;
            todoOK = asql.UpdateFoto(campo, tabla, condicion, image);
 
            if (cerrarConexion)
            {//Si se recibió un objeto 'AccesoSql' como parámetro, esta instrucción sql forma parte de una transacción y 
                //comparte conexión con otras, por eso solo se cierra la conexión a la B.D. en caso contrario.
                asql.Dispose();
            }
 
 
            return todoOK;
        }
 
        /// <summary>
        /// Método static y común a todas las 'Entidades' que devuelve en formato byte[] una imagen de la B.D.
        /// </summary>
        /// <param name="campo">Nombre del campo con la imagen.</param>
        /// <param name="tabla">Nombre de la tabla en la B.D.</param>
        /// <param name="condicion">Condición en formato SQL de la fila de la que se quiere obtener la imagen.</param>
        /// <returns>La imagen en formato byte[]. / null.- Si la fila no tiene imagen.</returns>
        protected static byte[] SelectImage(string campo, string tabla, string condicion)
        {
            byte[] foto;
            using (AccesoSql asql = CrearAccesoSQL())
            {
                foto = asql.SelectFoto(campo, tabla, condicion);
            }
 
            return foto;
        }
 
        public static FilasDB EjecutaSQL(string sentenciaSQL, AccesoSql _accesoSql = null)
        {
            FilasDB filas;
            bool cerrarConexion = (_accesoSql == null) ? true : false;
 
            AccesoSql asql = (_accesoSql == null) ? CrearAccesoSQL() : _accesoSql;
            filas = asql.Ejecuta(sentenciaSQL);
 
            if (cerrarConexion)
            {//Si se recibió un objeto 'AccesoSql' como parámetro entonces esta instrucción sql forma parte de una transacción o 
                //comparte conexión con otras, por eso solo se cierra la conexión a la B.D. en caso contrario.
                asql.Dispose();
            }
 
            return filas;
        }
 
        public static FilasDB EjecutaSQL(SqlCommand cmdSQL, AccesoSql _accesoSql = null)
        {
            FilasDB filas;
            bool cerrarConexion = (_accesoSql == null) ? true : false;
 
            AccesoSql asql = (_accesoSql == null) ? CrearAccesoSQL() : _accesoSql;
            filas = asql.Ejecuta(cmdSQL);
             
            if (cerrarConexion)
            {//Si se recibió un objeto 'AccesoSql' como parámetro entonces esta instrucción sql forma parte de una transacción o 
                //comparte conexión con otras, por eso solo se cierra la conexión a la B.D. en caso contrario.
                asql.Dispose();
            }
 
            return filas;
        }
 
        /// <summary>
        /// Este método detecta si algún campo del objeto 'Entidad' contiene algún valor
        /// peligroso que pueda ser un ataque de inyección SQL y se ejecute dicha instrucción sql
        /// en el método 'EjecutaSQL' de la clase 'AccesoSql'.
        /// </summary>
        /// <returns>Devuelve la cadena/caracter que puede producir un ataque SQL.</returns>
        public virtual string CheckSQLInyection()
        {
            Object[] valoresCampos = ValoresCamposBD(false);
            string cadenaCampo;
 
            for (int ind = 0; ind < valoresCampos.Length; ind++)
            {
                cadenaCampo = valoresCampos[ind].ToString();
                if (cadenaCampo.Contains(';'))
                {
                    return ";";
                }
 
                if (cadenaCampo.Contains("--"))
                {
                    return "--";
                }
 
                if (cadenaCampo.Contains("/*"))
                {
                    return "/*";
                }
 
                if (cadenaCampo.Contains("*/"))
                {
                    return "*/";
                }
 
                if (cadenaCampo.Contains("xp_"))
                {
                    return "xp_";
                }
 
                if (cadenaCampo.Contains("XP_"))
                {
                    return "XP_";
                }
 
                if (cadenaCampo.Contains('\''))
                {
                    return "\'";
                }
            }
 
            return null;
        }
        #endregion
 
    }
 
 
 
    /// <summary>
    /// Esta clase se utiliza como atributo para los campos de las clases que hereden de la clase 'Entidad'.
    /// Los campos que tengan la clase 'CampoTabla' como atributo, son los campos de la tabla en la B.D. que corresponde
    /// con dicha clase entidad. 
    /// <param>
    /// Excepciones:Los campos de la clase entidad deben tener el mismo nombre que los campos de la
    /// tabla en la B.D.
    /// </param>
    /// </summary>
     
    //Solo puede usarse como atributo de campos o propiedades (no de clases ni métodos).
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
    public class CampoBD : Attribute
    {
        /// <summary>
        /// Indica si el campo al que hace referencia este atributo, es el 'Id' de la Entidad/Tabla SQL.
        /// </summary>
        public bool esIndice;
 
        public CampoBD()
        {
            esIndice = false;
        }
 
        public CampoBD(bool _esIndice)
        {
            esIndice = _esIndice;
        }
    }
}