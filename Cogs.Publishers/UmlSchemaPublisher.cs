﻿// Copyright (c) 2017 Colectica. All rights reserved
// See the LICENSE file in the project root for more information.
using Cogs.Model;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.Diagnostics;

namespace Cogs.Publishers
{
    /// <summary>
    /// Generate an uml schema using the Garden of Eden approach, all elements and type definitions are declared globally
    /// </summary>
    public class UmlSchemaPublisher
    {
        /// path to write output in
        public string TargetDirectory { get; set; }
        /// boolean to determine whether to replace existing or not
        public bool Overwrite { get; set; }

        // list of all IDs created. Used to ensure no duplicates
        private List<string> IdList = new List<string>();

        public void Publish(CogsModel model)
        {
            if (TargetDirectory == null)
            {
                throw new InvalidOperationException("Target directory must be specified");
            }
            if (Overwrite && Directory.Exists(TargetDirectory))
            {
                Directory.Delete(TargetDirectory, true);
            }
            // TODO: if Overwrite is false and Directory.Exists(TargetDirectory)) throw an error and exit

            Directory.CreateDirectory(TargetDirectory);

            XNamespace xmins = "http://www.omg.org/spec/XMI/20110701";
            XNamespace umlns = "http://www.omg.org/spec/UML/20110701";
            XElement xmodel = new XElement("packagedElement", new XAttribute(xmins + "type", "uml:Package"), 
                new XAttribute(xmins + "id", "TestProject"), new XAttribute("name", "RestaurantMenu"));
            // loop through classes
            foreach (var item in model.ItemTypes)
            {
                // Create class
                var newItem = new XElement(new XElement("packagedElement", new XAttribute(xmins + "type", "uml:Class"),
                           new XAttribute(xmins + "id", CreateId(item.Name)),
                           new XAttribute("name", item.Name)));
                string extends = item.ExtendsTypeName;
                // loop through properties of class and add to class
                foreach(var property in item.Properties)
                {
                    var newProperty = new XElement("ownedAttribute", new XAttribute(xmins+ "type", "uml:Property"),
                           new XAttribute(xmins + "id", CreateId(item.Name + "." + property.Name)),
                           new XAttribute("name", property.Name));
                    newProperty.Add(new XElement("type", new XAttribute(xmins + "idref", property.DataTypeName)));
                    if(property.MinCardinality != null)
                    {
                        newProperty.Add(new XElement("lowerValue", new XAttribute(xmins + "type", "uml:LiteralInteger"),
                            new XAttribute(xmins + "id", CreateId(item.Name + "." + property.Name + ".MinCardinality")),
                            new XAttribute("value", property.MinCardinality)));
                    }
                    if (property.MaxCardinality != null)
                    {
                        var attribute = new XAttribute("value", property.MaxCardinality);
                        // if max is "n" change to "*"
                        if (property.MaxCardinality.Equals("n"))
                        {
                            attribute = new XAttribute("value", "*");
                        }
                        newProperty.Add(new XElement("upperValue", new XAttribute(xmins + "type", "uml:LiteralUnlimitedNatural"),
                            new XAttribute(xmins + "id", CreateId(item.Name + "." + property.Name + ".MaxCardinality")),
                            attribute));
                    }
                    newItem.Add(newProperty);
                }
                // adds pointers for inheritance where applicable
                if (!string.IsNullOrWhiteSpace(extends))
                {
                    newItem.Add(new XElement("generalization",
                        new XAttribute(xmins + "type", "uml:Generalization"),
                        new XAttribute(xmins + "id", CreateId(item.Name + ".Generalization")),
                        new XAttribute("general", extends)));
                }
                // add class to model
                xmodel.Add(newItem);
            }

            //create document header
            XDocument xDoc = new XDocument(
               new XDeclaration("1.0", "utf-8", null),
               new XElement(xmins + "XMI", new XAttribute(XNamespace.Xmlns + "uml", "http://www.omg.org/spec/UML/20110701"),
               new XAttribute(XNamespace.Xmlns + "xmi", "http://www.omg.org/spec/XMI/20110701"),
               new XElement(xmins + "Documentation", new XAttribute("exporter", "Enterprise Architect"), new XAttribute("exporterVersion", "6.5")),
               new XElement(umlns + "Model", new XAttribute(xmins + "type", "uml:Model"), new XAttribute("name", "EA_Model"), xmodel)));

            //write collection to file
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(TargetDirectory, "uml" + ".xmi.xml")))
            {
                XmlTextWriter writer = new XmlTextWriter(outputFile);
                writer.Formatting = Formatting.Indented;
                xDoc.WriteTo(writer);
                writer.Flush();
            }
        }

        // helper method that takes a string and checks that the ID has not been previously created
        // returns the string if valid, otherwise throws InvalidOperationException
        private string CreateId(string name)
        {
            if (IdList.Contains(name))
            {
                Console.WriteLine("ERROR: name '%s' used twice", name);
                throw new InvalidOperationException();
            }
            IdList.Add(name);
            return name;
        }
    }
}