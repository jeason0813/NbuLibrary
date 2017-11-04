//using System;
//using System.Collections.Generic;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using NbuLibrary.Core.EntityLogic;
//using NbuLibrary.Core.Services.tmp;

//namespace NbuLibrary.Test.EntityLogic
//{
//    [TestClass]
//    public class DefinitionTests
//    {
//        [TestMethod]
//        public void Test_EnsureDefinition()
//        {
//            EntityDefinitionServiceImpl defService = new EntityDefinitionServiceImpl();

//            var defs = new List<EntityDefinition>();

//            #region Build Defs

//            var authorEnt = new EntityDefinition()
//            {
//                Name = "Author",
//                Properties = new List<PropertyDefinition>() {
//                    new StringProperty("FirstName"),
//                    new StringProperty("LastName"),
//                    new PropertyDefinition(){
//                        Name = "Age",
//                        Type = PropertyTypes.Integer,
//                        Nullable = true
//                    },
//                },
//            };

//            var bookEnt = new EntityDefinition()
//            {
//                Name = "Book",
//                Properties = new List<PropertyDefinition>() {
//                    new StringProperty("Title"),
//                    new StringProperty("Sequence", 256),
//                    new PropertyDefinition("Price", PropertyTypes.Number)
//                }
//            };

//            #endregion

//            defs.Add(authorEnt);
//            defs.Add(bookEnt);


//            List<EntityRelation> rels = new List<EntityRelation>() {
//                new EntityRelation("Book", "Author", "Author", RelationTypes.ManyToOne)
//            };

//            defService.Ensure(defs, rels);

//            Assert.IsNotNull(defService.GetDefinitionByName("Author"));
//            Assert.IsNotNull(defService.GetDefinitionByName("Book"));

//            var rel = defService.GetRelation("Author", "Author");
//            Assert.AreEqual("Author", rel.LeftEntity);
//            Assert.AreEqual("Book", rel.RightEntity);
//            Assert.AreEqual(RelationTypes.OneToMany, rel.Type);
//            Assert.AreEqual(RelationTypes.ManyToOne, rel.GetTypeFor("Book"));
//            Assert.AreEqual("Author", rel.Role);

//            int found = 0;
//            foreach (var d in defService.GetAll())
//            {
//                if (d.Name == "Author" || d.Name == "Book")
//                    found++;
//            }
//            Assert.AreEqual(2, found);

//            //#region Extend author

//            //var authorEntEx = new EntityDefinition()
//            //{
//            //    Name = "Author",
//            //    Properties = new List<PropertyDefinition>() {
//            //        new PropertyDefinition("LastName", typeof(string), length:256),
//            //        new PropertyDefinition("Age", typeof(int)),
//            //        new PropertyDefinition("Biography", typeof(string), length: PropertyDefinition.MAX_LENGTH)
//            //    }
//            //};

//            //#endregion
            
//            //defService.Ensure(new List<EntityDefinition>() { authorEntEx }, null);
//            //var res = defService.GetDefinitionByName("Author");
//            //var bioProp = res.Properties.Find(p => p.Name == "Biography");
//            //Assert.IsNotNull(bioProp, "New property was not added during definition extension.");
//            //var incProp = res.Properties.Find(p => p.Name == "LastName");
//            //Assert.IsNotNull(incProp);
//            //Assert.AreEqual(256, incProp.Length, "Changed property not updated.");
//        }

//        [TestMethod]
//        public void Test_GetDefinition()
//        {
//            EntityDefinitionServiceImpl defService = new EntityDefinitionServiceImpl();
//            var bookDef = defService.GetDefinitionByName("Book");
//            Assert.IsNotNull(bookDef);
//            var priceProp = bookDef.Properties.Find(p => p.Name == "Price");
//            Assert.IsNotNull(priceProp);
//            Assert.AreEqual(priceProp.Type, PropertyTypes.Number);

//            var auhtorDef = defService.GetDefinitionByName("Author");
//            Assert.IsNotNull(auhtorDef);

//            Assert.AreSame(auhtorDef.Relations[0], bookDef.Relations[0]);
//            Assert.IsNotNull(defService.GetRelation("Author", "Author"));
//            Assert.IsNotNull(defService.GetRelation("Book", "Author"));
//            Assert.AreSame(defService.GetRelation("Author", "Author"), defService.GetRelation("Book", "Author"));
//        }
//    }
//}
