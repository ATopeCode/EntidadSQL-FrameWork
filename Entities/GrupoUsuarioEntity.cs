using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GestionSql;
using System.Runtime.Serialization;

namespace Entidades
{
    public class GrupoUsuarioEntity : EntidadSQL
    {
        public override string GetTabla()
        {
            return "dbo.tblGrupoUsuario";
        }

        [CampoBD(esIndice = true)]
        protected int id;
        public int Id
        {
            get
            {
                return id;
            }
        }

        [CampoBD]
        protected string nombre;
        public string Nombre
        {
            get
            {
                return nombre;
            }

            set
            {
                nombre = value;
            }
        }

        public GrupoUsuarioEntity()
        {
            id = -1;
            nombre = string.Empty;
        }
    }
}
