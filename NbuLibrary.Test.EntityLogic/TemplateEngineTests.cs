using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Infrastructure;
using NbuLibrary.Core.Services.tmp;
using NbuLibrary.Core.Domain;
using System.Collections.Generic;
using System.Data.SqlClient;
using NbuLibrary.Core.Sql;

namespace NbuLibrary.Test.EntityLogic
{
    [TestClass]
    public class TemplateEngineTests
    {
        [ClassInitialize]
        public static void Prepare(TestContext ctx)
        {
            ClearDatabase();
            using (SqlConnection conn = getConnection())
            {
                conn.Open();
                DatabaseManager mgr = new DatabaseManager(conn);
                TemplateServiceImpl.Install(mgr);
            }
        }

        [TestMethod]
        public void Test_Template_Save()
        {
            var svc = getSvc();
            var template = new HtmlTemplate() { Id = Guid.NewGuid(), Name = "Test 1", SubjectTemplate = "About...", BodyTemplate = "Plain text template." };
            svc.Save(template);
            var ret = svc.Get(template.Id);
            Assert.IsNotNull(ret);
            Assert.AreEqual(template.Id, ret.Id);
            Assert.AreEqual(template.Name, ret.Name);
            Assert.AreEqual(template.BodyTemplate, ret.BodyTemplate);
            Assert.AreEqual(template.SubjectTemplate, ret.SubjectTemplate);

            template.SubjectTemplate = template.SubjectTemplate + "_edit";
            template.BodyTemplate = template.BodyTemplate + "_edit";
            svc.Save(template);

            ret = svc.Get(template.Id);
            Assert.IsNotNull(ret);
            Assert.AreEqual(template.Id, ret.Id);
            Assert.AreEqual(template.Name, ret.Name);
            Assert.AreEqual(template.BodyTemplate, ret.BodyTemplate);
            Assert.AreEqual(template.SubjectTemplate, ret.SubjectTemplate);
        }

        [TestMethod]
        public void Test_Template_Render()
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

            var svc = getSvc();
            var tmpl = "<h2>{Favorite.Firstname} is my favorite author</h2><p>You can  check <a href=\"google.com/search?q={Favorite.lastname}\">{Favorite.FirstNAME}'s books</a>, though I also like {SecondFavorite.FirstName} {SecondFavorite.Lastname}, born on {SecondFavorite.born}.</p>";
            var subj = "About this {Favorite.lastname} author";
            var exp = "<h2>Raymond is my favorite author</h2><p>You can  check <a href=\"google.com/search?q=Feist\">Raymond's books</a>, though I also like Robert Jordan, born on 17/10/1948.</p>";
            var expSubj = "About this Feist author";
            var template = new HtmlTemplate() { Id = Guid.NewGuid(), Name = "Test 2", BodyTemplate = tmpl, SubjectTemplate = subj };
            svc.Save(template);

            var tmplContext = new Dictionary<string, Entity>();
            tmplContext.Add("Favorite", feist);
            tmplContext.Add("SecondFavorite", jordan);

            string body = null, subject = null;
            svc.Render(template, tmplContext, out subject, out body);
            Assert.AreEqual(exp, body);
            Assert.AreEqual(expSubj, subject);

        }

        [TestMethod]
        public void Test_Template_Render_WrongCtxValues()
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

            var svc = getSvc();
            var tmpl = "<h2>{Favorite.Firstname} is my{favorite.asdf} favorite author</h2><p>You can  check <a href=\"google.com/search?q={Favorite.lastname}\">{Favorite.FirstNAME}'s books</a>, though I also like {SecondFavorite.FirstName} {SecondFavorite.Lastname}, born on {SecondFavorite.born}.</p>";
            var exp = "<h2>Raymond is my{favorite.asdf} favorite author</h2><p>You can  check <a href=\"google.com/search?q=Feist\">Raymond's books</a>, though I also like Robert Jordan, born on 17/10/1948.</p>";
            var template = new HtmlTemplate() { Id = Guid.NewGuid(), Name = "Test 2", BodyTemplate = tmpl, SubjectTemplate = "irrelevent" };
            svc.Save(template);

            var tmplContext = new Dictionary<string, Entity>();
            tmplContext.Add("Favorite", feist);
            tmplContext.Add("SecondFavorite", jordan);

            string body = null, subject = null;
            svc.Render(template, tmplContext, out subject, out body);
            Assert.AreEqual(exp, body);

        }

        [TestMethod]
        public void Test_HtmlProcessor()
        {
            string html = @"<h3>Добре дошли!</h3><p>Добре дошли в системата за електронни услуги на библиотеката на Нов български университет!</p><p>Информация за видовете услуги може да намерите в <a target=""_blank"" href=""http://nbu.bg/index.php?l=116"">сайта на библиотеката</a>.</p><p>За допълнителна информация:</p><ul><li>имейл: library@nbu.bg</li><li>тел.: 02/8110296</li><li>скайп: NBU_Biblioteka</li></ul>Намерете ни в <a href=""https://google.com"">Google</a> сега!";
            string htmlWithoutAnchors = @"<h3>Добре дошли!</h3><p>Добре дошли в системата за електронни услуги на библиотеката на Нов български университет!</p><p>Информация за видовете услуги може да намерите</p>";
            string htmlWithEveryAnchor = "<p>Go to <a target=\"_self\" href=\"http://google.com\">google</a></p>are<a href=\"https://google.com\">Google</a>are<a target=\"_blank\" href=\"https://google.com\">Google</a>are<a target=\"_self\" href=\"https://google.com\">Google</a>are<a target=\"_parent\" href=\"https://google.com\">Google</a>are<a target=\"_top\" href=\"https://google.com\">Google</a>";
            string result = HtmlProcessor.ProcessEncodedHtml(System.Web.HttpUtility.HtmlEncode(html)).Replace("  ", " ");
            Assert.AreEqual( html, result);
            Assert.AreEqual(htmlWithoutAnchors, HtmlProcessor.ProcessEncodedHtml(System.Web.HttpUtility.HtmlEncode(htmlWithoutAnchors)).Replace("  ", " "));
            Assert.AreEqual(htmlWithEveryAnchor, HtmlProcessor.ProcessEncodedHtml(System.Web.HttpUtility.HtmlEncode(htmlWithEveryAnchor)).Replace("  ", " "));
        }

        private ITemplateService getSvc()
        {
            return new TemplateServiceImpl(new TestDatabaseService());
        }

        private static SqlConnection getConnection()
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder();
            b.DataSource = "localhost";
            b.InitialCatalog = "TestingNbuLib";
            b.IntegratedSecurity = true;
            return new SqlConnection(b.ConnectionString);
        }

        private static void ClearDatabase()
        {
            using (SqlConnection conn = getConnection())
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
}
