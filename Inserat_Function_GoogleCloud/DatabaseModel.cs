using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace Inserat_Function_GoogleCloud
{
    public class Inserat : DBClass
    {
        public ObjectId KategorieID { get; set; }
        public string Email { get; set; }

        public string Telefon { get; set; }

        public string Beschreibung { get; set; }

        public string Username { get; set; }

        public string Status { get; set; }

        public string Titel { get; set; }

        public string Adresse { get; set; }

        public string Strasse { get; set; }

        public string Ort { get; set; }

        public string UserID { get; set; }

        public DateTime Datum { get; set; }

        public decimal Preis { get; set; }

        public string Typ { get; set; }

        public string Zustand { get; set; }

        public List<string> Bilder { get; set; }
    }


    public class Kategorie : DBClass
    {  
        public string Name { get; set; }
    }
}
