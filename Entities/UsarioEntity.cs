using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GestionSql;
using System.Runtime.Serialization;

namespace Entitades
{
    public class UsuarioEntity : EntidadSQL
    {
        public override string GetTabla()
        {
            return "dbo.tblUsuario";
        }

        [CampoBD(esIndice = true)]
        protected int id;
        public int Id
        {
            get { return id; }
        }

        [CampoBD]
        protected int idPais;
        public int IdPais
        {
            get { return idPais; }
            set { idPais = value; }
        }

        [CampoBD]
        protected string nombre;
        public string Nombre
        {
            get { return nombre; } 
            set { nombre = value; }
        }

        [CampoBD]
        protected string password;
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        [CampoBD]
        protected string email;
        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        [CampoBD]
        protected string ciudad;
        public string Ciudad
        {
            get { return ciudad; }
            set { ciudad = value; }
        }

        [CampoBD]
        protected string direccion;
        public string Direccion
        {
            get { return direccion; }
            set { direccion = value; }
        }
        
        public UsuarioEntity()
        {
            id = -1;
            idPais = -1;
            nombre = "";
            password = "";
            email = "";
            ciudad = "";
            direccion = "";
        }

    }
