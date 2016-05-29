using GestionSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Entitades
{
    public class RelUserGroupEntity : EntidadSQL
    {
        public override string GetTabla()
        {
            return "dbo.tblRelUserGroup";
        }

        [CampoBD(esIndice = true)]
        protected int id;
        public int Id
        {
            get { return id; }
        }
        
        [CampoBD]
        protected int idUsuario;

        public int IdUsuario
        {
            get { return idUsuario; }
            set { idUsuario = value; }
        }
        
        [CampoBD]
        protected int idGrupo;
        public int IdGrupo
        {
            get { return idGrupo; }
            set { idGrupo = value; }
        }

        public RelUserGroupEntity()
        {
            id = -1;
            idUsuario = -1;
            idGrupo = -1;
        }
    }
}
