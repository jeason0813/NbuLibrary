//using System;
//using System.IO;
//using System.Xml.Serialization;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using NbuLibrary.Core.Domain;
//using NbuLibrary.Web.Models;

//namespace NbuLibrary.Test.Web
//{
//    [TestClass]
//    public class ViewDefinitionTests
//    {
//        [TestMethod]
//        public void Test_ViewDefinitionSerialization()
//        {
//            ViewDefinitionStore store = new ViewDefinitionStore();
//            store.Definitions.Add(new ViewDefinition()
//            {
//                Label = "Потребител",
//                Name = "User",
//                Fields = new System.Collections.Generic.List<ViewField>() {
//                    new ViewField(){
//                        Label = "Име",
//                        Property = "FirstName",
//                        Type = FieldTypes.String,
//                        Edit = new StringEditor(0, 100)
//                    },
//                    new ViewField()
//                    {
//                        Label = "Тип",
//                        Property = "Type",
//                        Type = FieldTypes.Enum,
//                        Edit = new EnumEditor(typeof(UserTypes)){
//                            Map = new System.Collections.Generic.List<string>()
//                            {
//                                "Клиент",//0
//                                "Библиотекар", //1
//                                "Администратор" //2
//                            }
//                        }
//                    },
//                    new ViewFieldRelated()
//                    {
//                        Label = "Потребителска група",
//                        Property = "Name",
//                        Entity = "UserGroup",
//                        Role = "UserGroup",
//                        Edit = new EntityEditor("UserGroup")
//                    }
//                }
//            });

//            var ser = new XmlSerializer(typeof(ViewDefinitionStore));
//            ViewDefinitionStore result = null;
//            using(FileStream fs = new FileStream(@"D:\Users\KV\Documents\Visual Studio 2012\Projects\NbuLibrary\NbuLibrary.Web\viewdefinitionstore.xml", FileMode.Create))
//            {
//                ser.Serialize(fs, store);
//                fs.Position = 0;
                
//            }
//            using (FileStream fs = new FileStream(@"D:\Users\KV\Documents\Visual Studio 2012\Projects\NbuLibrary\NbuLibrary.Web\viewdefinitionstore.xml", FileMode.Open))
//            {
//                result = ser.Deserialize(fs) as ViewDefinitionStore;
//            }

//            Assert.IsNotNull(result);
//        }
//    }
//}
