using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GestionSql
{
    /// <summary>
    /// Esta clase define una cadena "string" para una condición u operacion en una consulta SQL utilizando el valor de la consulta como
    /// un parámetro (variable=@valor).
    /// </summary>
    public class CadenaParametro
    {
        private string nombreCampo;
        private string operador;
        private string nombreParametro;
        private object valorCampo;
        private string preString;
        private string postString;

        public string NombreParametro
        {
            get
            {
                return nombreParametro;
            }
        }

        public object ValorParametro
        {
            get
            {
                return valorCampo;
            }
        }

        public CadenaParametro(string _nombreCampo, string _operador, object _valorCampo, string _preString = "", string _postString = "")
        {
            nombreCampo = _nombreCampo;
            operador = _operador;
            valorCampo = _valorCampo;
            preString = _preString;
            postString = _postString;

            //Se añade la "@" al nombre del Campo para formar el nombre del Parámetro.
            nombreParametro = nombreCampo.Trim();
            nombreParametro = '@' + nombreParametro;
        }

        public override string ToString()
        {
            string cadenaParametro = string.Format("{0} {1} {2} {3} {4}", preString, nombreCampo, operador, nombreParametro, postString);

            return cadenaParametro;
        }

    }
}