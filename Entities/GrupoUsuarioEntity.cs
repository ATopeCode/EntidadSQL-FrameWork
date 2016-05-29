using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GestionSql;
using System.Runtime.Serialization;

namespace Entidades
{
    [DataContract(Namespace="")]
    public class GrupoUsuarioEntity : EntidadSQL
    {
        public override string GetTabla()
        {
            return "dbo.tblGrupoUsuario";
        }

        [DataMember]
        [CampoBD(esIndice = true)]
        protected int id;
        public int Id
        {
            get
            {
                return id;
            }
        }

        [DataMember]
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