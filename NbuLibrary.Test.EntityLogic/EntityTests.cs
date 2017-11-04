using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NbuLibrary.Core.DataModel;
using System.Data.SqlClient;
using NbuLibrary.Core.Sql;
using NbuLibrary.Core.Infrastructure;
using System.Xml;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Domain;
using System.Transactions;
using NbuLibrary.Core.Services.tmp;

namespace NbuLibrary.Test.EntityLogic
{
    public class TestDatabaseService : IDatabaseService
    {
        public class DatabaseContext : IDatabaseContext
        {
            [ThreadStatic]
            private static int refCount = 0;

            [ThreadStatic]
            private static SqlConnection activeConnection;


            private TransactionScope _scope;
            public DatabaseContext(string connectionString, bool useTransaction)
            {
                if (useTransaction)
                    _scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(10.0) });
                if (refCount == 0)
                {
                    activeConnection = new SqlConnection(connectionString);
                    activeConnection.Open();
                }
                refCount++;
            }

            public SqlConnection Connection
            {
                get { return activeConnection; }
            }

            public void Complete()
            {
                if (_scope != null)
                    _scope.Complete();
            }

            public void Dispose()
            {
                if (_scope != null)
                    _scope.Dispose();
                refCount--;
                if (refCount == 0)
                {
                    activeConnection.Dispose();
                    activeConnection = null;
                }
            }
        }

        public SqlConnection GetSqlConnection()
        {
            return new SqlConnection("Data Source=POWERPC;Initial Catalog=TestingNbuLib;Integrated Security=True");
        }


        public IDatabaseContext GetDatabaseContext(bool useTransaction)
        {
            return new DatabaseContext("Data Source=POWERPC;Initial Catalog=TestingNbuLib;Integrated Security=True", useTransaction);
        }


        public string GetRootPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }

    [TestClass]
    public class EntityModelBasicTests
    {
        [TestMethod]
        public void Test_ModelBuilder_BuildModel_General()
        {
            ModelBuilder b = new ModelBuilder("Author");
            b.AddString("FirstName", 128);
            b.AddString("LastName", 128);
            b.AddBoolean("IsAlive", true);
            b.AddInteger("NumberOfAwards");
            b.AddDateTime("Born");
            b.AddDecimal("Rating");

            var em = b.EntityModel;
            Assert.IsNotNull(em);

            //string prop
            var fname = em.Properties["firstNAME"];
            Assert.IsTrue(fname is StringPropertyModel);
            Assert.AreEqual(128, (fname as StringPropertyModel).Length);

            //boolean prop
            var isalive = em.Properties["isalive"];
            Assert.IsTrue(isalive is BooleanPropertyModel);
            Assert.AreEqual(true, isalive.DefaultValue);

            //integer prop
            var noa = em.Properties["numberofawards"];
            Assert.IsTrue(noa is NumberPropertyModel);
            Assert.AreEqual(true, (noa as NumberPropertyModel).IsInteger);

            //datetime prop
            var born = em.Properties["born"];
            Assert.IsTrue(born is DateTimePropertyModel);
            //TODO: more testing datetime prop building

            //decimal prop
            var rating = em.Properties["rating"];
            Assert.IsTrue(rating is NumberPropertyModel);
            Assert.AreEqual(false, (rating as NumberPropertyModel).IsInteger);
        }

        [TestMethod]
        public void Test_ModelBuilder_BuildModel_Relations()
        {
            ModelBuilder ba = new ModelBuilder("Author");
            ModelBuilder bb = new ModelBuilder("Book");
            bb.AddString("Title", 256);

            bb.AddRelationTo(ba.EntityModel, RelationType.ManyToOne, "Author");

            var author = ba.EntityModel;
            var book = bb.EntityModel;

            Assert.IsNotNull(author);
            Assert.AreEqual(1, author.Relations.Count);
            var rel = book.Relations["author", "book", "author"];
            Assert.AreEqual(book.Name, rel.Right.Name);
            Assert.AreEqual(RelationType.OneToMany, rel.Type);
            Assert.AreEqual(RelationType.ManyToOne, rel.TypeFor(book.Name));

            rel = author.GetRelation("Book", "AUTHOR");
            Assert.AreEqual(book.Name, rel.Right.Name);

            bb.AddRelationTo(new NomenclatureModel("Genre"), RelationType.OneToOne, "BookGenre");

            var nom = book.GetRelation("genre", "BookGenre");
            Assert.AreEqual("Genre", nom.Right.Name);
        }

        [TestMethod]
        public void Test_DomainModelSerializer()
        {

            var dm = new DomainModel();
            ModelBuilder ba = new ModelBuilder("Author");
            ba.AddIdentity("Id");
            ba.AddString("FirstName", 128);
            ba.AddString("LastName", 128);
            ba.AddComputed("FullName", "[LastName]+N', '+[FirstName]");
            ba.AddBoolean("IsAlive", true);
            ba.AddInteger("NumberOfAwards");
            ba.AddDateTime("Born");
            ba.AddDecimal("Rating");

            ba.EntityModel.Rules.Add(new FutureOrPastDateRuleModel((DateTimePropertyModel)ba.EntityModel.Properties["born"], TimeSpan.Zero, false));

            ModelBuilder bb = new ModelBuilder("Book");
            bb.AddIdentity("Id");
            bb.AddString("Title", 256);
            bb.AddString("ISBN", 20);
            bb.AddDecimal("Price");
            bb.AddEnum<Genre>("Genre");
            bb.AddUri("BookUri", "books");

            bb.Rules.AddRequired("title");
            bb.Rules.AddUnique("ISBN");

            ba.AddRelationTo(bb.EntityModel, RelationType.OneToMany, "Author");

            dm.Entities.Add(ba.EntityModel);
            dm.Entities.Add(bb.EntityModel);
            dm.Relations.Add(ba.EntityModel.Relations.Single());
            ModelBuilder mbRel = new ModelBuilder(ba.EntityModel.Relations.Single());
            mbRel.AddIdentity("id");
            mbRel.AddIdentity("rid");
            mbRel.AddIdentity("lid");
            mbRel.Rules.AddRequired("lid");
            mbRel.Rules.AddRequired("rid");

            DomainModelSerializer ser = new DomainModelSerializer();
            XmlDocument xmlDoc = ser.Serialize(dm);
            var dm2 = ser.Deserialize(xmlDoc);

            Assert.AreEqual(dm.Entities.Count, dm2.Entities.Count);
            Assert.AreEqual(dm.Relations.Count, dm2.Relations.Count);
            var en1 = dm.Entities.GetEnumerator();
            var en2 = dm2.Entities.GetEnumerator();
            while (en1.MoveNext() && en2.MoveNext())
            {
                Assert.AreEqual(en1.Current.Name, en2.Current.Name);
                Assert.AreEqual(en1.Current.Properties.Count, en2.Current.Properties.Count);
                Assert.AreEqual(en1.Current.Relations.Count, en2.Current.Relations.Count);
                Assert.AreEqual(en1.Current.Rules.Count, en2.Current.Rules.Count);
                var p1 = en1.Current.Properties.GetEnumerator();
                var p2 = en2.Current.Properties.GetEnumerator();
                while (p1.MoveNext() && p2.MoveNext())
                {
                    Assert.AreEqual(p1.Current.Name, p2.Current.Name);
                    Assert.AreEqual(p1.Current.Type, p2.Current.Type);
                    Assert.AreEqual(p1.Current.DefaultValue, p2.Current.DefaultValue);
                    Assert.AreEqual(p1.Current.GetType(), p2.Current.GetType());
                    if (p1.Current.Type == PropertyType.Enum)
                        Assert.AreEqual(((EnumPropertyModel)p1.Current).EnumType, ((EnumPropertyModel)p2.Current).EnumType);
                    else if (p1.Current.Type == PropertyType.Computed)
                        Assert.AreEqual(((ComputedPropertyModel)p1.Current).Formula, ((ComputedPropertyModel)p2.Current).Formula);
                    else if (p1.Current.Type == PropertyType.Number)
                        Assert.AreEqual(((NumberPropertyModel)p1.Current).IsInteger, ((NumberPropertyModel)p2.Current).IsInteger);
                    else if (p1.Current.Type == PropertyType.Sequence)
                    {
                        Assert.AreEqual(((SequencePropertyModel)p1.Current).SequenceType, ((SequencePropertyModel)p2.Current).SequenceType);
                        Assert.AreEqual(((SequencePropertyModel)p1.Current).SequenceId, ((SequencePropertyModel)p2.Current).SequenceId);
                    }
                }

                var r1 = en1.Current.Rules.GetEnumerator();
                var r2 = en2.Current.Rules.GetEnumerator();
                while (r1.MoveNext() && r2.MoveNext())
                {
                    Assert.AreEqual(r1.Current.GetType(), r2.Current.GetType());
                    if (r1.Current is RequiredRuleModel)
                        Assert.AreEqual((r1.Current as RequiredRuleModel).Property.Name, (r2.Current as RequiredRuleModel).Property.Name);
                    else if (r1.Current is UniqueRuleModel)
                    {
                        Assert.AreEqual((r1.Current as UniqueRuleModel).Properties.Count(), (r2.Current as UniqueRuleModel).Properties.Count());
                        var pren1 = (r1.Current as UniqueRuleModel).Properties.GetEnumerator();
                        var pren2 = (r2.Current as UniqueRuleModel).Properties.GetEnumerator();
                        while (pren1.MoveNext() && pren2.MoveNext())
                        {
                            Assert.AreEqual(pren1.Current.Name, pren2.Current.Name);
                        }
                    }
                    else if (r1.Current is FutureOrPastDateRuleModel)
                    {
                        var er1 = r1.Current as FutureOrPastDateRuleModel;
                        var er2 = r2.Current as FutureOrPastDateRuleModel;
                        Assert.AreEqual(er1.Future, er2.Future);
                        Assert.AreEqual(er1.Offset, er2.Offset);
                        Assert.AreEqual(er1.Property.Name, er2.Property.Name);
                    }
                }
            }

            var rn1 = dm.Relations.GetEnumerator();
            var rn2 = dm2.Relations.GetEnumerator();
            while (rn1.MoveNext() && rn2.MoveNext())
            {
                Assert.AreEqual(rn1.Current.Name, rn2.Current.Name);
                Assert.AreEqual(rn1.Current.Properties.Count, rn2.Current.Properties.Count);
                Assert.AreEqual(rn1.Current.Relations.Count, rn2.Current.Relations.Count);
                Assert.AreEqual(rn1.Current.Rules.Count, rn2.Current.Rules.Count);
                var p1 = rn1.Current.Properties.GetEnumerator();
                var p2 = rn2.Current.Properties.GetEnumerator();
                while (p1.MoveNext() && p2.MoveNext())
                {
                    Assert.AreEqual(p1.Current.Name, p2.Current.Name);
                    Assert.AreEqual(p1.Current.Type, p2.Current.Type);
                    Assert.AreEqual(p1.Current.DefaultValue, p2.Current.DefaultValue);
                    Assert.AreEqual(p1.Current.GetType(), p2.Current.GetType());
                }
            }
        }
    }

    [TestClass]
    public class DomainModelServiceTests
    {
        private IDomainModelService svc;

        [TestInitialize]
        public void Prepare()
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder();
            b.DataSource = "localhost";
            b.InitialCatalog = "TestingNbuLib";
            b.IntegratedSecurity = true;

            using (SqlConnection conn = new SqlConnection(b.ConnectionString))
            {
                conn.Open();
                DatabaseManager mgr = new DatabaseManager(conn);
                mgr.LoadSchema();
                while (mgr.Tables.Count > 0)
                    mgr.DropTable(mgr.Tables[0], true);

                mgr.LoadSchema();
                Assert.AreEqual(0, mgr.Tables.Count);
            }

            svc = new DomainModelService(new TestDatabaseService(), new IDomainChangeListener[] { new EntityRepositoryDomainListener() });
            svc.Save(new DomainModel());
        }

        [TestMethod]
        public void Test_DomainModelService_Basic()
        {
            var dm = new DomainModel();
            ModelBuilder ba = new ModelBuilder("Author");
            ba.AddIdentity("Id");
            ba.AddString("FirstName", 128);
            ba.AddString("LastName", 128);
            ba.AddComputed("FullName", "[LastName]+N', '+[FirstName]");
            ba.AddBoolean("IsAlive", true);
            ba.AddInteger("NumberOfAwards");
            ba.AddDateTime("Born");
            ba.AddDecimal("Rating");

            ba.EntityModel.Rules.Add(new FutureOrPastDateRuleModel((DateTimePropertyModel)ba.EntityModel.Properties["born"], TimeSpan.Zero, false));

            ModelBuilder bb = new ModelBuilder("Book");
            bb.AddIdentity("Id");
            bb.AddString("Title", 256);
            bb.AddString("ISBN", 20);
            bb.AddDecimal("Price");
            bb.AddEnum<Genre>("Genre");

            bb.Rules.AddRequired("title");
            bb.Rules.AddUnique("ISBN");

            ba.AddRelationTo(bb.EntityModel, RelationType.OneToMany, "Author");
            dm.Entities.Add(ba.EntityModel);
            dm.Entities.Add(bb.EntityModel);

            svc.Save(dm);

            Assert.IsNotNull(svc.Domain);
            Assert.IsNotNull(svc.Domain.Entities["author"]);
            Assert.IsNotNull(svc.Domain.Entities["book"]);
            Assert.IsNotNull(svc.Domain.Relations["book", "author", "author"]);
        }

        [TestMethod]
        public void Test_DomainModelService_Compare()
        {
            var dm = new DomainModel();
            ModelBuilder ba = new ModelBuilder("Author");
            ba.AddIdentity("Id");
            ba.AddString("FirstName", 128);
            ba.AddString("LastName", 128);
            ba.AddComputed("FullName", "[LastName]+N', '+[FirstName]");
            ba.AddBoolean("IsAlive", false);
            ba.AddInteger("NumberOfAwards");
            ba.AddDateTime("Born");
            ba.AddDecimal("Rating");
            ba.AddString("Uri", 20);

            ModelBuilder bb = new ModelBuilder("Book");
            bb.AddIdentity("Id");
            bb.AddString("Title", 256);
            bb.AddString("ISBN", 20);
            bb.AddDecimal("Price");

            bb.Rules.AddUnique("ISBN");
            bb.Rules.AddRequired("title");

            ba.AddRelationTo(bb.EntityModel, RelationType.OneToMany, "Author");

            dm.Entities.Add(ba.EntityModel);
            dm.Entities.Add(bb.EntityModel);

            svc.Save(dm);

            ModelBuilder bo = new ModelBuilder("Order");
            bo.AddIdentity("Number");
            bo.AddDecimal("Total");
            bo.AddBoolean("Paid");
            bo.AddDateTime("CreatedOn");
            bo.AddDateTime("ShipmentDate");
            bo.AddRelationTo(dm.Entities["book"], RelationType.ManyToMany, "Ordered");
            dm.Entities.Add(bo.EntityModel);


            ba.EntityModel.Properties.Remove(ba.EntityModel.Properties["IsAlive"]);
            (ba.EntityModel.Properties["LastName"] as StringPropertyModel).Length = 256;
            (ba.EntityModel.Properties["firstName"] as StringPropertyModel).Length = 256;
            ba.AddString("Nickname", 256);
            ba.Rules.AddUnique("Uri");
            (ba.EntityModel.Properties["Rating"] as NumberPropertyModel).DefaultValue = 5.0m;

            DomainModelChanges dmc = svc.CompareWithExisting(dm);
            Assert.AreEqual(2, dmc.EntityChanges.Count());
            Assert.AreEqual(1, dmc.EntityChanges.Count(ec => ec.Change == ChangeType.Created));
            var ecs = dmc.EntityChanges.Single(ec => ec.Change == ChangeType.Created);
            Assert.AreEqual(bo.EntityModel.Properties.Count, ecs.PropertyChanges.Count());
            Assert.AreEqual(1, dmc.EntityChanges.Count(ec => ec.Change == ChangeType.Modified));

            Assert.AreEqual(1, dmc.RelationChanges.Count());

            var change = dmc.EntityChanges.Single(e => e.New.Name == ba.EntityModel.Name);
            Assert.AreEqual(5, change.PropertyChanges.Count());
            Assert.AreEqual(3, change.PropertyChanges.Count(pc => pc.Change == ChangeType.Modified));
            Assert.AreEqual(1, change.PropertyChanges.Count(pc => pc.Change == ChangeType.Deleted));
            Assert.AreEqual(1, change.PropertyChanges.Count(pc => pc.Change == ChangeType.Created));
            Assert.AreEqual(1, change.RuleChanges.Count());
        }

        [TestMethod]
        public void Test_DomainModelService_Merge()
        {
            var dm = new DomainModel();
            ModelBuilder ba = new ModelBuilder("Author");
            ba.AddIdentity("Id");
            ba.AddString("FirstName", 128);
            ba.AddString("LastName", 128);
            ba.AddComputed("FullName", "[LastName]+N', '+[FirstName]");
            ba.AddBoolean("IsAlive", true);
            ba.AddInteger("NumberOfAwards");
            ba.AddDateTime("Born");
            ba.AddDecimal("Rating");

            ModelBuilder bb = new ModelBuilder("Book");
            bb.AddIdentity("Id");
            bb.AddString("Title", 256);
            bb.AddString("ISBN", 20);
            bb.AddDecimal("Price");
            bb.AddEnum<Genre>("Genre", Genre.Fantasy);

            bb.Rules.AddRequired("title");
            bb.Rules.AddUnique("ISBN");

            ba.AddRelationTo(bb.EntityModel, RelationType.OneToMany, "Author");

            dm.Entities.Add(ba.EntityModel);
            dm.Entities.Add(bb.EntityModel);

            svc.Save(dm);
            using (var conn = new TestDatabaseService().GetSqlConnection())
            {
                conn.Open();
                DatabaseManager dbm = new DatabaseManager(conn);
                dbm.LoadSchema();
                var tableA = dbm.Tables.Find(t => t.Name == "Author");
                Assert.IsNotNull(tableA);
                var fnCol = tableA.Columns.Find(c => c.Name.Equals("FullName", StringComparison.InvariantCultureIgnoreCase));
                Assert.IsNotNull(fnCol);
            }

            ModelBuilder bo = new ModelBuilder("Order");
            bo.AddDecimal("Total");
            bo.AddBoolean("Paid");
            bo.AddDateTime("CreatedOn");
            bo.AddDateTime("ShipmentDate");
            bo.AddRelationTo(dm.Entities["book"], RelationType.ManyToMany, "Ordered");
            dm.Entities.Add(bo.EntityModel);


            ba.EntityModel.Properties.Remove(ba.EntityModel.Properties["IsAlive"]);
            (ba.EntityModel.Properties["LastName"] as StringPropertyModel).Length = 256;
            (ba.EntityModel.Properties["firstName"] as StringPropertyModel).Length = 256;
            (bb.EntityModel.Properties["genre"] as EnumPropertyModel).DefaultValue = Genre.Horror;
            ba.AddString("Nickname", 256);


            svc.Merge(dm);

            Assert.AreEqual(3, svc.Domain.Entities.Count);
            //foreach (var em1 in dm.Entities)
            //{
            //    var em2 = svc.Domain.Entities[em1.Name];
            //    Assert.AreEqual(em1.Properties.Count, em2.Properties.Count);
            //}
            //TODO-tests:finish domainmodelservice_merge test

            //Computed columns are droped and recreated when upgrading the columns in their computed definition
            using (var conn = new TestDatabaseService().GetSqlConnection())
            {
                conn.Open();
                DatabaseManager dbm = new DatabaseManager(conn);
                dbm.LoadSchema();
                var tableA = dbm.Tables.Find(t => t.Name == "Author");
                Assert.IsNotNull(tableA);
                var fnCol = tableA.Columns.Find(c => c.Name.Equals("FullName", StringComparison.InvariantCultureIgnoreCase));
                Assert.IsNotNull(fnCol);
            }
        }

        [TestMethod]
        public void Test_DomainModelService_Union()
        {
            var dm = new DomainModel();
            ModelBuilder ba = new ModelBuilder("Author");
            ba.AddIdentity("Id");
            ba.AddString("FirstName", 128);
            ba.AddString("LastName", 128);
            ba.AddComputed("FullName", "[LastName]+N', '+[FirstName]");
            ba.AddBoolean("IsAlive", true);
            ba.AddInteger("NumberOfAwards");
            ba.AddDateTime("Born");
            ba.AddDecimal("Rating");

            ModelBuilder bb = new ModelBuilder("Book");
            bb.AddIdentity("Id");
            bb.AddString("Title", 256);
            bb.AddString("ISBN", 20);

            bb.Rules.AddRequired("title");
            bb.Rules.AddUnique("ISBN");

            ba.AddRelationTo(bb.EntityModel, RelationType.OneToMany, "Author");

            dm.Entities.Add(ba.EntityModel);
            dm.Entities.Add(bb.EntityModel);

            ModelBuilder bo = new ModelBuilder("Order");
            bo.AddDecimal("Total");
            bo.AddBoolean("Paid");
            bo.AddDateTime("CreatedOn");
            bo.AddDateTime("ShipmentDate");

            ModelBuilder bb2 = new ModelBuilder("Book");
            bb2.AddIdentity("Id");
            bb2.AddString("Title", 256);
            bb2.AddString("Barcode", 20);
            bb2.AddDecimal("Price");

            bo.AddRelationTo(bb2.EntityModel, RelationType.ManyToMany, "Ordered");

            var dm2 = new DomainModel();
            dm2.Entities.Add(bo.EntityModel);
            dm2.Entities.Add(bb2.EntityModel);

            var un = svc.Union(new DomainModel[] { dm, dm2 });

            Assert.AreEqual(3, un.Entities.Count);
            var author = un.Entities["author"];
            Assert.AreEqual(8, author.Properties.Count);
            Assert.AreEqual(5, un.Entities["book"].Properties.Count);
            Assert.AreEqual(4, un.Entities["Order"].Properties.Count);
            Assert.AreEqual(2, un.Relations.Count);
        }
    }

    //TODO: Test updaterelation
    [TestClass]
    public class EntityRepositoryTests
    {
        private static IDomainModelService dms;
        [ClassInitialize]
        public static void Prepare(TestContext ctx)
        {
            dms = new DomainModelService(new TestDatabaseService(), new IDomainChangeListener[] { new EntityRepositoryDomainListener() });
            ClearDatabase();
            CreateDomain();
        }

        [TestMethod]
        public void Test_EntityRepo_Read()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                var jordan = new Author()
                {
                    FirstName = "Robert",
                    LastName = "Jordan",
                    IsAlive = false,
                    Born = new DateTime(1948, 10, 17),
                    Rating = 10.0m
                };

                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    IsAlive = true,
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                jordan.Id = repository.Create(jordan);
                Assert.IsTrue(jordan.Id > 0);
                feist.Id = repository.Create(feist);
                Assert.IsTrue(feist.Id > 0);

                EntityQuery2 q = new EntityQuery2("author", jordan.Id);
                q.AddProperties("FirstName", "lastname", "isalive", "born", "rating");
                var e = repository.Read(q);
                Assert.AreEqual(5, e.Data.Count);
                foreach (var p in q.Properties)
                {
                    Assert.AreEqual(jordan.Data[p], e.Data[p]);
                }
            }
        }

        [TestMethod]
        public void Test_EntityRepo_ReadWithRel()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                var jordan = new Author()
                {
                    FirstName = "Robert",
                    LastName = "Jordan",
                    IsAlive = false,
                    Born = new DateTime(1948, 10, 17),
                    Rating = 10.0m
                };

                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    IsAlive = true,
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                var fb1 = new Book()
                {
                    Title = "The Apprentice",
                    Price = 19.90m
                };

                var fb2 = new Book()
                {
                    Title = "The Magician",
                    Price = 17.10m
                };

                repository.Create(jordan);
                repository.Create(feist);
                repository.Create(fb1);
                repository.Create(fb2);
                repository.Attach(feist, new Relation("author", fb1));
                repository.Attach(feist, new Relation("author", fb2));


                EntityQuery2 q = new EntityQuery2("book", fb1.Id);
                q.AddProperties("title", "price");
                q.Include("author", "author");
                var e = repository.Read(q);
                Assert.AreEqual(2, e.Data.Count);
                foreach (var p in q.Properties)
                {
                    Assert.AreEqual(fb1.Data[p], e.Data[p]);
                }

                Assert.AreEqual(1, e.RelationsData.Count);
                var authorRel = e.GetSingleRelation("author", "author");
                foreach (var d in feist.Data)
                {
                    Assert.AreEqual(d.Value, authorRel.Entity.Data[d.Key]);
                }

                //repository.Complete();
            }
        }

        [TestMethod]
        public void Test_EntityRepo_Search()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                #region prepare data
                var jordan = new Author()
                {
                    FirstName = "Robert",
                    LastName = "Jordan",
                    IsAlive = false,
                    Born = new DateTime(1948, 10, 17),
                    Rating = 10.0m
                };

                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    IsAlive = true,
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                var fb1 = new Book()
                {
                    Title = "The Apprentice",
                    Price = 19.90m
                };

                var fb2 = new Book()
                {
                    Title = "The Magician",
                    Price = 17.10m
                };


                var jb1 = new Book()
                {
                    Title = "The Shadow is Rising",
                    Price = 21.15m
                };
                var jb2 = new Book()
                {
                    Title = "The Eye of the World",
                    Price = 25.80m
                };

                repository.Create(jordan);
                repository.Create(feist);
                repository.Create(fb1);
                repository.Create(fb2);
                repository.Create(jb1);
                repository.Create(jb2);
                repository.Attach(feist, new Relation("author", fb1));
                repository.Attach(feist, new Relation("author", fb2));
                repository.Attach(jordan, new Relation("author", jb1));
                repository.Attach(jordan, new Relation("author", jb2));
                #endregion

                var query = new EntityQuery2("author");
                query.AddProperties("firstname", "lastname", "born");
                var res = repository.Search(query);

                Assert.AreEqual(2, res.Count());
                foreach (var a in res)
                {
                    var orig = a.Id == jordan.Id ? jordan : feist;
                    foreach (var p in query.Properties)
                    {
                        Assert.AreEqual(orig.Data[p], a.Data[p]);
                    }
                }

                //greater then
                EntityQuery2 q = new EntityQuery2("book");
                q.WhereGreaterThen("price", 19.0m);
                Assert.AreEqual(3, repository.Search(q).Count());

                //less then
                q = new EntityQuery2("book");
                q.WhereLessThen("price", 20.0m);
                Assert.AreEqual(2, repository.Search(q).Count());

                //is boolean
                q = new EntityQuery2("author");
                q.WhereIs("isalive", false);
                var r = repository.Search(q);
                Assert.AreEqual(1, r.Count());
                Assert.AreEqual(jordan.Id, r.Single().Id);

                //is string (ignore case)
                q = new EntityQuery2("author");
                q.WhereIs("lastname", "jordan");
                r = repository.Search(q);
                Assert.AreEqual(1, r.Count());
                Assert.AreEqual(jordan.Id, r.Single().Id);

                //starts with
                q = new EntityQuery2("author");
                q.WhereStartsWith("firstname", "ra");
                r = repository.Search(q);
                Assert.AreEqual(1, r.Count());
                Assert.AreEqual(feist.Id, r.Single().Id);

                //ends with
                q = new EntityQuery2("book");
                q.WhereEndsWith("title", "world");
                r = repository.Search(q);
                Assert.AreEqual(1, r.Count());
                Assert.AreEqual(jb2.Id, r.Single().Id);

                //less then
                q = new EntityQuery2("book");
                q.WhereAnyOf("id", new object[] { fb1.Id, jb1.Id, jb2.Id });
                Assert.AreEqual(3, repository.Search(q).Count());

                //between decimal
                q = new EntityQuery2("book");
                q.WhereBetween("price", 19.0m, 22.0m);
                Assert.AreEqual(2, repository.Search(q).Count());

                //between datetime
                q = new EntityQuery2("author");
                q.WhereBetween("born", new DateTime(1948, 1, 1), new DateTime(1949, 1, 1));
                r = repository.Search(q);
                Assert.AreEqual(1, r.Count());
                Assert.AreEqual(jordan.Id, r.Single().Id);

                q = new EntityQuery2("author");
                q.WhereBetween("born", new DateTime(1948, 1, 1), new DateTime(1949, 1, 1));
                q.WhereIs("isalive", true);
                Assert.AreEqual(0, repository.Search(q).Count());

                q = new EntityQuery2("author");
                q.WhereBetween("born", new DateTime(1960, 1, 1), new DateTime(1970, 1, 1));
                q.WhereIs("isalive", true);
                q.WhereStartsWith("firstname", "ra");
                Assert.AreEqual(1, repository.Search(q).Count());


                //repository.Complete();
            }
        }


        [TestMethod]
        public void Test_EntityRepo_Count()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                #region prepare data
                var jordan = new Author()
                {
                    FirstName = "Robert",
                    LastName = "Jordan",
                    IsAlive = false,
                    Born = new DateTime(1948, 10, 17),
                    Rating = 10.0m
                };

                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    IsAlive = true,
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                var fb1 = new Book()
                {
                    Title = "The Apprentice",
                    Price = 19.90m
                };

                var fb2 = new Book()
                {
                    Title = "The Magician",
                    Price = 17.10m
                };


                var jb1 = new Book()
                {
                    Title = "The Shadow is Rising",
                    Price = 21.15m
                };
                var jb2 = new Book()
                {
                    Title = "The Eye of the World",
                    Price = 25.80m
                };

                repository.Create(jordan);
                repository.Create(feist);
                repository.Create(fb1);
                repository.Create(fb2);
                repository.Create(jb1);
                repository.Create(jb2);
                repository.Attach(feist, new Relation("author", fb1));
                repository.Attach(feist, new Relation("author", fb2));
                repository.Attach(jordan, new Relation("author", jb1));
                repository.Attach(jordan, new Relation("author", jb2));
                #endregion

                var query = new EntityQuery2("author");
                query.AddProperties("firstname", "lastname", "born");
                var res = repository.Search(query);

                Assert.AreEqual(2, res.Count());
                Assert.AreEqual(2, repository.Count(query));

                //greater then
                EntityQuery2 q = new EntityQuery2("book");
                q.WhereGreaterThen("price", 19.0m);
                Assert.AreEqual(3, repository.Search(q).Count());
                Assert.AreEqual(3, repository.Count(q));

                //less then
                q = new EntityQuery2("book");
                q.WhereLessThen("price", 20.0m);
                Assert.AreEqual(2, repository.Search(q).Count());
                Assert.AreEqual(2, repository.Count(q));

                //is boolean
                q = new EntityQuery2("author");
                q.WhereIs("isalive", false);
                var r = repository.Search(q);
                Assert.AreEqual(1, r.Count());
                Assert.AreEqual(1, repository.Count(q));


                //is string (ignore case)
                q = new EntityQuery2("author");
                q.WhereIs("lastname", "jordan");
                r = repository.Search(q);
                Assert.AreEqual(1, r.Count());
                Assert.AreEqual(1, repository.Count(q));

                //starts with
                q = new EntityQuery2("author");
                q.WhereStartsWith("firstname", "ra");
                r = repository.Search(q);
                Assert.AreEqual(1, r.Count());
                Assert.AreEqual(1, repository.Count(q));

                //ends with
                q = new EntityQuery2("book");
                q.WhereEndsWith("title", "world");
                r = repository.Search(q);
                Assert.AreEqual(1, r.Count());
                Assert.AreEqual(1, repository.Count(q));

                //less then
                q = new EntityQuery2("book");
                q.WhereAnyOf("id", new object[] { fb1.Id, jb1.Id, jb2.Id });
                Assert.AreEqual(3, repository.Search(q).Count());
                Assert.AreEqual(3, repository.Count(q));

                //between decimal
                q = new EntityQuery2("book");
                q.WhereBetween("price", 19.0m, 22.0m);
                Assert.AreEqual(2, repository.Search(q).Count());
                Assert.AreEqual(2, repository.Count(q));

                //between datetime
                q = new EntityQuery2("author");
                q.WhereBetween("born", new DateTime(1948, 1, 1), new DateTime(1949, 1, 1));
                r = repository.Search(q);
                Assert.AreEqual(1, r.Count());
                Assert.AreEqual(1, repository.Count(q));

                q = new EntityQuery2("author");
                q.WhereBetween("born", new DateTime(1948, 1, 1), new DateTime(1949, 1, 1));
                q.WhereIs("isalive", true);
                Assert.AreEqual(0, repository.Search(q).Count());
                Assert.AreEqual(0, repository.Count(q));

                q = new EntityQuery2("author");
                q.WhereBetween("born", new DateTime(1960, 1, 1), new DateTime(1970, 1, 1));
                q.WhereIs("isalive", true);
                q.WhereStartsWith("firstname", "ra");
                Assert.AreEqual(1, repository.Search(q).Count());
                Assert.AreEqual(1, repository.Count(q));
            }
        }

        
        [TestMethod]
        public void Test_EntityRepo_Sorting()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                #region prepare data
                var jordan = new Author()
                {
                    FirstName = "Robert",
                    LastName = "Jordan",
                    IsAlive = false,
                    Born = new DateTime(1948, 10, 17),
                    Rating = 10.0m
                };

                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    IsAlive = true,
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                var fb1 = new Book()
                {
                    Title = "The Apprentice",
                    Price = 19.90m
                };

                var fb2 = new Book()
                {
                    Title = "The Magician",
                    Price = 17.10m
                };


                var jb1 = new Book()
                {
                    Title = "The Shadow is Rising",
                    Price = 21.15m
                };
                var jb2 = new Book()
                {
                    Title = "The Eye of the World",
                    Price = 25.80m
                };

                repository.Create(jordan);
                repository.Create(feist);
                repository.Create(fb1);
                repository.Create(fb2);
                repository.Create(jb1);
                repository.Create(jb2);
                repository.Attach(feist, new Relation("author", fb1));
                repository.Attach(feist, new Relation("author", fb2));
                repository.Attach(jordan, new Relation("author", jb1));
                repository.Attach(jordan, new Relation("author", jb2));
                #endregion

                var query = new EntityQuery2("author");
                query.AddProperties("firstname", "lastname", "born");
                query.SortBy = new Sorting("firstname");
                var res = repository.Search(query);

                Assert.AreEqual(feist.Id, res.First().Id);
                query.SortBy = new Sorting("firstname", true);
                res = repository.Search(query);
                Assert.AreEqual(jordan.Id, res.First().Id);


                var q = new EntityQuery2("book");
                q.AddProperty("title");
                q.WhereAnyOf("id", new object[] { fb1.Id, jb1.Id, jb2.Id });
                q.SortBy = new Sorting("title");
                res = repository.Search(q);
                Assert.AreEqual(fb1.Id, res.ElementAt(0).Id);
                Assert.AreEqual(jb2.Id, res.ElementAt(1).Id);
                Assert.AreEqual(jb1.Id, res.ElementAt(2).Id);

                ////greater then
                //EntityQuery2 q = new EntityQuery2("book");
                //q.WhereGreaterThen("price", 19.0m);
                //Assert.AreEqual(3, repository.Search(q).Count());

                ////less then
                //q = new EntityQuery2("book");
                //q.WhereLessThen("price", 20.0m);
                //Assert.AreEqual(2, repository.Search(q).Count());

                ////is boolean
                //q = new EntityQuery2("author");
                //q.WhereIs("isalive", true);
                //var r = repository.Search(q);
                //Assert.AreEqual(1, r.Count());
                //Assert.AreEqual(feist.Id, r.Single().Id);

                ////is string (ignore case)
                //q = new EntityQuery2("author");
                //q.WhereIs("lastname", "jordan");
                //r = repository.Search(q);
                //Assert.AreEqual(1, r.Count());
                //Assert.AreEqual(jordan.Id, r.Single().Id);

                ////starts with
                //q = new EntityQuery2("author");
                //q.WhereStartsWith("firstname", "ra");
                //r = repository.Search(q);
                //Assert.AreEqual(1, r.Count());
                //Assert.AreEqual(feist.Id, r.Single().Id);

                ////ends with
                //q = new EntityQuery2("book");
                //q.WhereEndsWith("title", "world");
                //r = repository.Search(q);
                //Assert.AreEqual(1, r.Count());
                //Assert.AreEqual(jb2.Id, r.Single().Id);

                ////less then
                //q = new EntityQuery2("book");
                //q.WhereAnyOf("id", new object[] { fb1.Id, jb1.Id, jb2.Id });
                //Assert.AreEqual(3, repository.Search(q).Count());

                ////between decimal
                //q = new EntityQuery2("book");
                //q.WhereBetween("price", 19.0m, 22.0m);
                //Assert.AreEqual(2, repository.Search(q).Count());

                ////between datetime
                //q = new EntityQuery2("author");
                //q.WhereBetween("born", new DateTime(1948, 1, 1), new DateTime(1949, 1, 1));
                //r = repository.Search(q);
                //Assert.AreEqual(1, r.Count());
                //Assert.AreEqual(jordan.Id, r.Single().Id);

                //q = new EntityQuery2("author");
                //q.WhereBetween("born", new DateTime(1948, 1, 1), new DateTime(1949, 1, 1));
                //q.WhereIs("isalive", true);
                //Assert.AreEqual(0, repository.Search(q).Count());

                //q = new EntityQuery2("author");
                //q.WhereBetween("born", new DateTime(1960, 1, 1), new DateTime(1970, 1, 1));
                //q.WhereIs("isalive", true);
                //q.WhereStartsWith("firstname", "ra");
                //Assert.AreEqual(1, repository.Search(q).Count());


                //repository.Complete();
            }
        }

        [TestMethod]
        public void Test_EntityRepo_Paging()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                #region prepare data

                int aCnt = 20;
                int bCnt = 3;
                for (int i = 0; i < aCnt; i++)
                {
                    var a = new Author()
                    {
                        FirstName = "Fname" + i,
                        LastName = "Lname" + i,
                        Born = DateTime.Now.AddYears(-20).AddDays(i),
                        NumberOfAwards = i / 3
                    };
                    repository.Create(a);
                    for (int j = 0; j < bCnt; j++)
                    {
                        var b = new Book()
                        {
                            Title = string.Format("Book_{0}_{1}", i, j),
                            Genre = Genre.SciFi,
                            Price = 10.0m + j,
                            ISBN = string.Format("{0}_{1}", a.LastName, j)
                        };
                        repository.Create(b);
                        repository.Attach(b, new Relation("author", a));
                    }
                }

                Assert.AreEqual(aCnt, repository.Search(new EntityQuery2("author")).Count());
                Assert.AreEqual(aCnt * bCnt, repository.Search(new EntityQuery2("book")).Count());

                #endregion

                var query = new EntityQuery2("Author");
                query.AddProperties("FirstName", "LastName");
                query.Include("book", "author");
                query.Paging = new Paging(1, 10);
                var res = repository.Search(query);
                Assert.AreEqual(10, res.Count());
                int idx = 0;
                foreach (var r in res)
                {
                    Assert.AreEqual("Fname" + idx, r.GetData<string>("firstname"));
                    var books = r.GetManyRelations("book", "author");
                    int bidx = 0;
                    foreach (var b in books)
                    {
                        Assert.AreEqual(string.Format("Book_{0}_{1}", idx, bidx++), b.Entity.GetData<string>("title"));
                    }
                    idx++;
                }

                //assert second page
                query.Paging.Page++;
                res = repository.Search(query);
                Assert.AreEqual(10, res.Count());
                foreach (var r in res)
                {
                    Assert.AreEqual("Fname" + idx, r.GetData<string>("firstname"));
                    var books = r.GetManyRelations("book", "author");
                    int bidx = 0;
                    foreach (var b in books)
                    {
                        Assert.AreEqual(string.Format("Book_{0}_{1}", idx, bidx++), b.Entity.GetData<string>("title"));
                    }
                    idx++;
                }
            }
        }

        [TestMethod]
        public void Test_EntityRepo_SearchWithRels()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                var jordan = new Author()
                {
                    FirstName = "Robert",
                    LastName = "Jordan",
                    IsAlive = false,
                    Born = new DateTime(1948, 10, 17),
                    Rating = 10.0m
                };

                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    IsAlive = true,
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                var fb1 = new Book()
                {
                    Title = "The Apprentice",
                    Price = 19.90m
                };

                var fb2 = new Book()
                {
                    Title = "The Magician",
                    Price = 17.10m
                };

                repository.Create(jordan);
                repository.Create(feist);
                repository.Create(fb1);
                repository.Create(fb2);
                repository.Attach(feist, new Relation("author", fb1));
                repository.Attach(feist, new Relation("author", fb2));

                var query = new EntityQuery2("author");
                query.AddProperties("firstname", "lastname", "born");
                query.Include("book", "author");
                var res = repository.Search(query);

                Assert.AreEqual(2, res.Count());
                var rf = res.Single(e => e.Id == feist.Id);
                var rj = res.Single(e => e.Id == jordan.Id);

                Assert.AreEqual(1, rf.RelationsData.Count);
                var books = rf.GetManyRelations("book", "author");
                Assert.AreEqual(2, books.Count());
                foreach (var r in books)
                {
                    var orig = r.Entity.Id == fb1.Id ? fb1 : fb2;
                    foreach (var pm in dms.Domain.Entities["book"].Properties)
                    {
                        if (orig.Data.ContainsKey(pm.Name))
                            Assert.AreEqual(orig.Data[pm.Name], r.Entity.Data[pm.Name]);
                    }
                }

                Assert.AreEqual(0, rj.RelationsData.Count);
                Assert.AreEqual(0, rj.GetManyRelations("book", "author").Count());

                //TODO: rules!


                //repository.Complete();
            }
        }


        [TestMethod]
        public void Test_EntityRepo_SearchRelated()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                #region prepare data
                var jordan = new Author()
                {
                    FirstName = "Robert",
                    LastName = "Jordan",
                    IsAlive = false,
                    Born = new DateTime(1948, 10, 17),
                    Rating = 10.0m
                };

                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    IsAlive = true,
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                var fb1 = new Book()
                {
                    Title = "The Apprentice",
                    Price = 19.90m
                };

                var fb2 = new Book()
                {
                    Title = "The Magician",
                    Price = 17.10m
                };


                var jb1 = new Book()
                {
                    Title = "The Shadow is Rising",
                    Price = 21.15m
                };
                var jb2 = new Book()
                {
                    Title = "The Eye of the World",
                    Price = 25.80m
                };

                repository.Create(jordan);
                repository.Create(feist);
                repository.Create(fb1);
                repository.Create(fb2);
                repository.Create(jb1);
                repository.Create(jb2);
                repository.Attach(feist, new Relation("author", fb1));
                repository.Attach(feist, new Relation("author", fb2));
                repository.Attach(jordan, new Relation("author", jb1));
                repository.Attach(jordan, new Relation("author", jb2));
                #endregion

                EntityQuery2 query = new EntityQuery2("book");
                query.AllProperties = true;
                var fq = new RelationQuery("author", "author", feist.Id);
                query.RelatedTo.Add(fq);
                var res = repository.Search(query);
                Assert.AreEqual(2, res.Count());
                Assert.IsNotNull(res.First(b => b.GetData<string>("title") == fb1.Title));
                Assert.IsNotNull(res.First(b => b.GetData<string>("title") == fb2.Title));

                query.Include("author", "author");
                res = repository.Search(query);
                Assert.AreEqual(2, res.Count());
                Assert.IsNotNull(res.First(b => b.GetData<string>("title") == fb1.Title));
                Assert.IsNotNull(res.First(b => b.GetData<string>("title") == fb2.Title));
            }
        }

        [TestMethod]
        public void Test_EntityRepo_Update()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                #region prepare data
                var jordan = new Author()
                {
                    FirstName = "Robert",
                    LastName = "Jordan",
                    IsAlive = false,
                    Born = new DateTime(1948, 10, 17),
                    Rating = 10.0m
                };

                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    IsAlive = true,
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                var fb1 = new Book()
                {
                    Title = "The Apprentice",
                    Price = 19.90m
                };

                var fb2 = new Book()
                {
                    Title = "The Magician",
                    Price = 17.10m
                };

                repository.Create(jordan);
                repository.Create(feist);
                repository.Create(fb1);
                repository.Create(fb2);
                repository.Attach(feist, new Relation("author", fb1));
                repository.Attach(feist, new Relation("author", fb2));
                #endregion

                jordan.LastName += "EDIT";
                jordan.IsAlive = true;
                jordan.Born = jordan.Born.AddDays(2.0);
                jordan.Rating -= 0.5m;

                repository.Update(jordan);
                var q = new EntityQuery2("author", jordan.Id);
                q.AddProperties("lastname", "isalive", "born", "rating");
                var e = repository.Read(q);
                var updated = new Author(e);
                Assert.AreEqual(jordan.LastName, updated.LastName);
                Assert.AreEqual(jordan.Born, updated.Born);
                Assert.AreEqual(jordan.IsAlive, updated.IsAlive);
                Assert.AreEqual(jordan.Rating, updated.Rating);
            }
        }

        //TODO: Test other property types' default values
        [TestMethod]
        public void Test_EntityRepo_Create()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                #region prepare data
                var jordan = new Author()
                {
                    FirstName = "Robert",
                    LastName = "Jordan",
                    IsAlive = false,
                    Born = new DateTime(1948, 10, 17),
                    Rating = 10.0m
                };

                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                var fb1 = new Book()
                {
                    Title = "The Apprentice",
                    Price = 19.90m
                };

                var fb2 = new Book()
                {
                    Title = "The Magician",
                    Price = 17.10m,
                    Genre = Genre.Fantasy
                };
                #endregion

                repository.Create(jordan);
                repository.Create(feist);
                repository.Create(fb1);
                repository.Create(fb2);
                repository.Attach(feist, new Relation("author", fb1));
                repository.Attach(feist, new Relation("author", fb2));
                fb1.Genre = Genre.Mistery; //default value
                feist.IsAlive = true;//default value

                var q = new EntityQuery2("author", feist.Id);
                q.AddProperties("FirstName", "lastname", "isalive", "born", "rating");
                q.Include("book", "author");
                var e = repository.Read(q);
                var created = new Author(e);
                Assert.AreEqual(feist.FirstName, created.FirstName);
                Assert.AreEqual(feist.LastName, created.LastName);
                Assert.AreEqual(feist.Born, created.Born);
                Assert.AreEqual(feist.IsAlive, created.IsAlive);
                Assert.AreEqual(feist.Rating, created.Rating);
                Assert.AreEqual(2, e.GetManyRelations("book", "author").Count());//repository.Detach(feist, new Relation("author", fb1));
                var eb1 = e.GetManyRelations("book", "author").First();
                Book b = new Book(eb1.Entity);
                Assert.AreEqual(fb1.Genre, b.Genre);
            }
        }

        [TestMethod]
        public void Test_EntityRepo_CreateSequences()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                var fb1 = new Book()
                {
                    Title = "The Apprentice",
                    Price = 19.90m
                };

                var fb2 = new Book()
                {
                    Title = "The Magician",
                    Price = 17.10m,
                    Genre = Genre.Fantasy
                };

                repository.Create(feist);
                repository.Create(fb1);
                repository.Create(fb2);
                repository.Attach(feist, new Relation("author", fb1));
                repository.Attach(feist, new Relation("author", fb2));

                var order = new Entity("Order");
                order.SetData<decimal>("total", fb1.Price + fb2.Price);
                order.SetData<DateTime>("createdon", DateTime.Now);
                repository.Create(order);
                repository.Attach(order, new Relation("ordered", fb1));
                repository.Attach(order, new Relation("ordered", fb2));

                var q = new EntityQuery2("order", order.Id);
                q.AllProperties = true;
                var saved = repository.Read(q);
                Assert.AreEqual(string.Format("BO-1-{0}", DateTime.Now.Year), saved.GetData<string>("number"));

                var order2 = new Entity("Order");
                order2.SetData<decimal>("total", fb1.Price);
                order2.SetData<DateTime>("createdon", DateTime.Now);
                repository.Create(order2);
                repository.Attach(order2, new Relation("ordered", fb1));

                q = new EntityQuery2("order", order2.Id);
                q.AllProperties = true;
                saved = repository.Read(q);
                Assert.AreEqual(string.Format("BO-2-{0}", DateTime.Now.Year), saved.GetData<string>("number"));
            }
        }

        [TestMethod]
        public void Test_EntityRepo_AttachDetach()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                #region prepare data
                var jordan = new Author()
                {
                    FirstName = "Robert",
                    LastName = "Jordan",
                    IsAlive = false,
                    Born = new DateTime(1948, 10, 17),
                    Rating = 10.0m
                };

                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    IsAlive = true,
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                var fb1 = new Book()
                {
                    Title = "The Apprentice",
                    Price = 19.90m
                };

                var fb2 = new Book()
                {
                    Title = "The Magician",
                    Price = 17.10m
                };

                repository.Create(jordan);
                repository.Create(feist);
                repository.Create(fb1);
                repository.Create(fb2);

                #endregion

                repository.Attach(feist, new Relation("author", fb1));
                var rel2 = new Relation("author", fb2);
                var writtenOn = new DateTime(1996, 4, 25);
                rel2.SetData<DateTime>("WrittenOn", writtenOn);
                repository.Attach(feist, rel2);

                var q = new EntityQuery2("author", feist.Id);
                q.AddProperties("FirstName", "lastname", "isalive", "born", "rating");
                q.Include("book", "author");
                var e = repository.Read(q);
                Assert.AreEqual(2, e.GetManyRelations("book", "author").Count());

                var bq = new EntityQuery2("book");
                bq.Include("author", "author");
                var bes = repository.Search(bq);
                foreach (var be in bes)
                {
                    Assert.AreEqual(1, be.RelationsData.Count);
                    Assert.AreEqual(feist.Id, be.GetSingleRelation("author", "author").Entity.Id);
                    if (be.Id == fb2.Id)
                        Assert.AreEqual(writtenOn, be.GetSingleRelation("author", "author").GetData<DateTime>("writtenon"));
                }

                repository.Detach(feist, new Relation("author", fb1));
                e = repository.Read(q);
                Assert.AreEqual(1, e.GetManyRelations("book", "author").Count());

                repository.Attach(fb1, new Relation("author", feist));
                e = repository.Read(q);
                Assert.AreEqual(2, e.GetManyRelations("book", "author").Count());
                repository.Detach(fb1, new Relation("author", feist));
                e = repository.Read(q);
                Assert.AreEqual(1, e.GetManyRelations("book", "author").Count());

                bool ex = false;
                try { repository.Attach(fb2, new Relation("author", jordan)); }
                catch (Exception) { ex = true; }
                Assert.IsTrue(ex, "Exception not thrown when attaching two authors to single book");
            }
        }

        [TestMethod]
        public void Test_EntityRepo_Delete()
        {
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            bool ex = false;
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                #region prepare data
                var jordan = new Author()
                {
                    FirstName = "Robert",
                    LastName = "Jordan",
                    IsAlive = false,
                    Born = new DateTime(1948, 10, 17),
                    Rating = 10.0m
                };

                var feist = new Author()
                {
                    FirstName = "Raymond",
                    LastName = "Feist",
                    IsAlive = true,
                    Born = new DateTime(1963, 2, 14),
                    Rating = 6.7m
                };

                var fb1 = new Book()
                {
                    Title = "The Apprentice",
                    Price = 19.90m
                };

                var fb2 = new Book()
                {
                    Title = "The Magician",
                    Price = 17.10m
                };

                //var jb1 = new Book()
                //{
                //    Title = "The Wheel of Time",
                //    Price = 21.0m
                //};

                repository.Create(jordan);
                repository.Create(feist);
                repository.Create(fb1);
                repository.Create(fb2);
                //repository.Create(jb1);
                repository.Attach(feist, new Relation("author", fb1));
                repository.Attach(feist, new Relation("author", fb2));
                //repository.Attach(jordan, new Relation("author", jb1));

                #endregion

                var r1 = new EntityQuery2("author", jordan.Id);
                Assert.IsNotNull(repository.Read(r1));
                repository.Delete(jordan);
                Assert.IsNull(repository.Read(r1));

                var r2 = new EntityQuery2("author", feist.Id);
                Assert.IsNotNull(repository.Read(r2));

                try { repository.Delete(feist); }
                catch (Exception) { ex = true; }
            }
            Assert.IsTrue(ex, "Exception not thrown when deleting author with attached relations");
        }

        //[TestMethod]
        public void Test_EntityRepo_Perf()
        {
            var r = new Random();
            var dbService = new TestDatabaseService();
            var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            using (var ctx = dbService.GetDatabaseContext(true))
            {
                for (int i = 0; i < 1000; i++)
                {
                    EntityUpdate update = new EntityUpdate("author");
                    update.Set("firstname", "Robert" + i);
                    update.Set("lastname", "Jordan");
                    update.Set("isalive", false);
                    update.Set("Born", new DateTime(1948, 10, 17));
                    update.Set("Rating", 5.5m + r.Next(4));
                    var id = repository.Create(update.ToEntity());
                    Assert.IsTrue(id > 0);

                    EntityQuery2 q = new EntityQuery2("author", id);
                    q.AddProperties("FirstName", "lastname", "isalive", "born", "rating");
                    var e = repository.Read(q);
                    foreach (var pu in update.PropertyUpdates)
                    {
                        Assert.AreEqual(pu.Value, e.Data[pu.Key]);
                    }

                    EntityUpdate update2 = new EntityUpdate(e.Name, e.Id);
                    update2.Set("rating", 5.5m + r.Next(4));
                    update2.Set("lastname", e.Data["lastname"] + "_EDIT");

                    repository.Update(update2.ToEntity());
                    e = repository.Read(q);
                    foreach (var pu in update2.PropertyUpdates)
                    {
                        Assert.AreEqual(pu.Value, e.Data[pu.Key]);
                    }
                    foreach (var pu in update.PropertyUpdates)
                    {
                        if (!pu.Key.Equals("rating", StringComparison.InvariantCultureIgnoreCase) && !pu.Key.Equals("lastname", StringComparison.InvariantCultureIgnoreCase))
                            Assert.AreEqual(pu.Value, e.Data[pu.Key]);
                    }

                }

                ctx.Complete();
            }

            using (var ctx = dbService.GetDatabaseContext(true))
            {
                var qAll = new EntityQuery2("Author");
                var all = repository.Search(qAll);
                Assert.AreEqual(1000, all.Count());
                foreach (var a in all)
                    repository.Delete(a);
                Assert.AreEqual(0, repository.Search(qAll).Count());
            }
        }

        private static void CreateDomain()
        {
            var dm = new DomainModel();
            ModelBuilder ba = new ModelBuilder("Author");
            ba.AddIdentity("Id");
            ba.AddString("FirstName", 128);
            ba.AddString("LastName", 128);
            ba.AddBoolean("IsAlive", true);
            ba.AddInteger("NumberOfAwards");
            ba.AddDateTime("Born");
            ba.AddDecimal("Rating");

            ModelBuilder bb = new ModelBuilder("Book");
            bb.AddIdentity("Id");
            bb.AddString("Title", 256);
            bb.AddString("ISBN", 20);
            bb.AddDecimal("Price");
            bb.AddEnum<Genre>("Genre", Genre.Mistery);

            bb.Rules.AddRequired("title");
            bb.Rules.AddUnique("ISBN");

            var rel = ba.AddRelationTo(bb.EntityModel, RelationType.OneToMany, "Author");
            var br = new ModelBuilder(rel);
            br.AddDateTime("WrittenOn");

            dm.Entities.Add(ba.EntityModel);
            dm.Entities.Add(bb.EntityModel);

            ModelBuilder bo = new ModelBuilder("Order");
            bo.AddDecimal("Total");
            bo.AddBoolean("Paid");
            bo.AddDateTime("CreatedOn");
            bo.AddDateTime("ShipmentDate");
            bo.AddRelationTo(dm.Entities["book"], RelationType.ManyToMany, "Ordered");
            bo.AddUri("Number", "BO");
            dm.Entities.Add(bo.EntityModel);


            dms.Save(dm);

            using (var conn = new TestDatabaseService().GetSqlConnection())
            {
                conn.Open();
                var dbManager = new DatabaseManager(conn);
                SequenceProvider.Initialize(dbManager);
            }
        }

        private static void ClearDatabase()
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder();
            b.DataSource = "localhost";
            b.InitialCatalog = "TestingNbuLib";
            b.IntegratedSecurity = true;

            using (SqlConnection conn = new SqlConnection(b.ConnectionString))
            {
                conn.Open();
                DatabaseManager mgr = new DatabaseManager(conn);
                mgr.LoadSchema();
                while (mgr.Tables.Count > 0)
                    mgr.DropTable(mgr.Tables[0], true);

                mgr.LoadSchema();
                Assert.AreEqual(0, mgr.Tables.Count);
            }
        }
    }

    [TestClass]
    public class EntityOperationServiceTests
    {
        #region private classes

        private class Inspector : IEntityOperationInspector, IEntityQueryInspector
        {
            public InspectionResult Inspect(Core.Services.tmp.EntityOperation operation)
            {
                return InspectionResult.Allow;
            }

            public InspectionResult InspectQuery(EntityQuery2 query)
            {
                return InspectionResult.Allow;
            }

            public InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entity)
            {
                return InspectionResult.Allow;
            }
        }

        private class Logic : IEntityOperationLogic
        {
            public void Before(Core.Services.tmp.EntityOperation operation, EntityOperationContext context)
            {
                if (operation is EntityUpdate)
                {
                    var update = operation as EntityUpdate;
                    if (!update.Id.HasValue)
                        update.Set("CreatedOn", DateTime.Now);
                }
            }

            public void After(Core.Services.tmp.EntityOperation operation, EntityOperationContext context, EntityOperationResult result)
            {
            }
        }

        private class TestDomainListener : IDomainChangeListener
        {
            public void BeforeSave(EntityModel entityModel)
            {
                if (entityModel.IsNomenclature)
                    return;

                if (!entityModel.Properties.Contains("CreatedOn"))
                {
                    var mb = new ModelBuilder(entityModel);
                    mb.AddDateTime("CreatedOn");
                }
            }

            public void AfterSave(EntityModel entityModel)
            {
            }
        }


        #endregion

        private static IDomainModelService dms;


        [ClassInitialize]
        public static void Prepare(TestContext ctx)
        {
            dms = new DomainModelService(new TestDatabaseService(), new IDomainChangeListener[] { new EntityRepositoryDomainListener(), new TestDomainListener() });
            ClearDatabase();
            CreateDomain();
        }

        [TestMethod]
        public void Test_EntityOperation_Update()
        {
            var dbService = new TestDatabaseService();
            var repo = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
            IEntityOperationService svc = new EntityOperationService(repo, dbService, new IEntityOperationInspector[] { new Inspector() }, new IEntityQueryInspector[] { new Inspector() }, new IEntityOperationLogic[] { new Logic() });
            EntityUpdate update = new EntityUpdate("Author");
            update.Set("FirstName", "John");
            update.Set("LastName", "Tolkin");
            update.Set("Numberofawards", 2);
            update.Set("IsAlive", false);

            EntityUpdate book = new EntityUpdate("book");
            book.Set("Title", "The Eye of the World");
            book.Set("genre", Genre.Fantasy);

            svc.Update(book);
            update.Attach("Book", "author", book.Id.Value);

            var result = svc.Update(update);
            Assert.AreEqual(true, result.Success);

            EntityQuery2 query = new EntityQuery2("Author");
            query.AddProperties("FirstName", "LastName", "IsAlive", "CreatedOn");
            query.Include("book", "author");
            var res = repo.Search(query);
            Assert.AreEqual(1, res.Count());
            var a = res.Single();
            Assert.AreEqual("John", a.GetData<string>("Firstname"));
            Assert.AreEqual("Tolkin", a.GetData<string>("LastName"));
            var created = a.GetData<DateTime>("createdon");
            Assert.AreEqual(DateTime.Now.Date, created.Date);
            var books = a.GetManyRelations("book", "author");
            Assert.AreEqual(1, books.Count());
            var b = books.Single().Entity;
            Assert.AreEqual("The Eye of the World", b.GetData<string>("title"));
            Assert.AreEqual(Genre.Fantasy, b.GetData<Genre>("genre"));
            created = b.GetData<DateTime>("createdon");
            Assert.AreEqual(DateTime.Now.Date, created.Date);

            repo.Delete(a, true);
            repo.Delete(b);
        }

        private static void CreateDomain()
        {
            var dm = new DomainModel();
            ModelBuilder ba = new ModelBuilder("Author");
            ba.AddIdentity("Id");
            ba.AddString("FirstName", 128);
            ba.AddString("LastName", 128);
            ba.AddBoolean("IsAlive", true);
            ba.AddInteger("NumberOfAwards");
            ba.AddDateTime("Born");
            ba.AddDecimal("Rating");

            ModelBuilder bb = new ModelBuilder("Book");
            bb.AddIdentity("Id");
            bb.AddString("Title", 256);
            bb.AddString("ISBN", 20);
            bb.AddDecimal("Price");
            bb.AddEnum<Genre>("Genre");

            bb.Rules.AddRequired("title");
            bb.Rules.AddUnique("ISBN");

            ba.AddRelationTo(bb.EntityModel, RelationType.OneToMany, "Author");

            dm.Entities.Add(ba.EntityModel);
            dm.Entities.Add(bb.EntityModel);

            ModelBuilder bo = new ModelBuilder("Order");
            bo.AddDecimal("Total");
            bo.AddBoolean("Paid");
            bo.AddDateTime("CreatedOn");
            bo.AddDateTime("ShipmentDate");
            bo.AddRelationTo(dm.Entities["book"], RelationType.ManyToMany, "Ordered");
            dm.Entities.Add(bo.EntityModel);


            dms.Save(dm);
        }

        private static void ClearDatabase()
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder();
            b.DataSource = "localhost";
            b.InitialCatalog = "TestingNbuLib";
            b.IntegratedSecurity = true;

            using (SqlConnection conn = new SqlConnection(b.ConnectionString))
            {
                conn.Open();
                DatabaseManager mgr = new DatabaseManager(conn);
                mgr.LoadSchema();
                while (mgr.Tables.Count > 0)
                    mgr.DropTable(mgr.Tables[0], true);

                mgr.LoadSchema();
                Assert.AreEqual(0, mgr.Tables.Count);
            }
        }
    }

    public enum Genre
    {
        SciFi,
        Fantasy,
        Horror,
        Mistery
    }

    public class Author : Entity
    {
        public Author()
            : base("Author")
        {

        }

        public Author(int id)
            : base("Author", id)
        {

        }


        public Author(Entity entity)
            : base(entity)
        {
            if (!entity.Name.Equals("Author", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Cannot create Author object from other entity type.");
        }

        public string FirstName
        {
            get
            {
                return GetData<string>("FirstName");
            }
            set
            {
                SetData<string>("FirstName", value);
            }
        }

        public string LastName
        {
            get
            {
                return GetData<string>("LastName");
            }
            set
            {
                SetData<string>("LastName", value);
            }
        }

        public decimal Rating
        {
            get
            {
                return GetData<decimal>("Rating");
            }
            set
            {
                SetData<decimal>("Rating", value);
            }
        }

        public DateTime Born
        {
            get
            {
                return GetData<DateTime>("Born");
            }
            set
            {
                SetData<DateTime>("Born", value);
            }
        }

        public int NumberOfAwards
        {
            get
            {
                return GetData<int>("NumberOfAwards");
            }
            set
            {
                SetData<int>("NumberOfAwards", value);
            }
        }

        public bool IsAlive
        {
            get
            {
                return GetData<bool>("IsAlive");
            }
            set
            {
                SetData<bool>("IsAlive", value);
            }
        }
    }

    public class Book : Entity
    {
        public Book()
            : base("Book")
        {

        }

        public Book(int id)
            : base("Book", id)
        {

        }

        public Book(Entity entity)
            : base(entity)
        {
            if (!entity.Name.Equals("Book", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Cannot create book object from other entity type.");
        }


        public string Title
        {
            get
            {
                return GetData<string>("Title");
            }
            set
            {
                SetData<string>("Title", value);
            }
        }

        public Genre Genre
        {
            get
            {
                return GetData<Genre>("Genre");
            }
            set
            {
                SetData<Genre>("Genre", value);
            }
        }

        public string ISBN
        {
            get
            {
                return GetData<string>("ISBN");
            }
            set
            {
                SetData<string>("ISBN", value);
            }
        }

        public decimal Price
        {
            get
            {
                return GetData<decimal>("Price");
            }
            set
            {
                SetData<decimal>("Price", value);
            }
        }
    }
}
