using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NbuLibrary.Core.Services;
using System.Data.SqlClient;
using System.Transactions;
using NbuLibrary.Core.Infrastructure;
using System.Collections.Generic;
using NbuLibrary.Core.Domain;
using System.Linq;

namespace NbuLibrary.Tests.Performance
{
    //[TestClass]
    //public class PrepareData
    //{
    //    [TestMethod]
    //    public void GenerateLotsOfData()
    //    {
    //        //load config
    //        int countOfInqueries = 0;
    //        int countOfCompletedBibl = 0;
    //        int countOfBiblQueries = 0;
    //        int countOfCustomers = 0;


    //        var r = new Random();

    //        var dbService = new TestDatabaseService();
    //        var dms = new DomainModelService(new TestDatabaseService(), new IDomainChangeListener[] { new EntityRepositoryDomainListener() });
    //        var repository = new EntityRepository(dms, dbService, new SequenceProvider(dbService));
    //        using (var ctx = dbService.GetDatabaseContext(false))
    //        {
    //            var groups = GetAllCustomerGroups(repository);
    //            for (int i = 0; i < countOfCustomers; i++)
    //            {
    //                var cust = new User()
    //                {
    //                    Email = string.Format("loadtest_user_{0}@test.test", Guid.NewGuid().ToString().Replace("-", "")),
    //                    FirstName = "LOADTEST FirstName " + r.Next(),
    //                    LastName = "LOADTEST LastName " + r.Next(),
    //                    UserType = UserTypes.Customer,
    //                    Password="nopass"
    //                };
    //                repository.Create(cust);
    //                repository.Attach(cust, new Relation("UserGroup", groups[r.Next(0, groups.Count)]));

    //            }

    //            var customers = GetCustomers(repository);
    //            var librarians = GetLibrarians(repository);
    //            var languages = repository.Search(new EntityQuery2("Language")).Select(e => e.Id).ToList();
    //            var biblDocs = repository.Search(new EntityQuery2("BibliographicDocument")).Select(e => e.Id).ToList();

    //            for (int it = 0; it < countOfInqueries; it++)
    //            {
    //                CreateInquery(repository, new DateTime(2000 + r.Next(0, 13), r.Next(1, 12), r.Next(1, 20)), customers[r.Next(0, customers.Count)], librarians[r.Next(0, librarians.Count)]);
    //            }

    //            for (int i = 0; i < countOfCompletedBibl; i++)
    //            {
    //                int lang1 = languages[r.Next(0, languages.Count)];
    //                int lang2 = languages[r.Next(0, languages.Count)];
    //                while (lang2 == lang1)
    //                    lang2 = languages[r.Next(0, languages.Count)];

    //                int doc1 = biblDocs[r.Next(0, biblDocs.Count)];
    //                int doc2 = biblDocs[r.Next(0, biblDocs.Count)];
    //                while (doc1 == doc2)
    //                    doc2 = biblDocs[r.Next(0, biblDocs.Count)];

    //                CreateBibliography(repository, string.Format("LOADTEST Bibliography No{0}_{1}", r.Next(), r.Next()), r.Next(1950, 2013), r.Next(1950, 2013), new[] { lang1, lang2 }, new[] { doc1, doc2 });
    //            }

    //            var bibliographies = GetCompletedBibliographies(repository);

    //            for (int i = 0; i < countOfBiblQueries; i++)
    //            {
    //                CreateBibliographicQuery(
    //                    repository,
    //                    new DateTime(2000 + r.Next(0, 13), r.Next(1, 12), r.Next(1, 20)),
    //                    PaymentMethod.CardCredit,
    //                    (decimal)(r.NextDouble() * 20 + 10.0),
    //                    r.NextDouble() > 0.5,
    //                    customers[r.Next(0, customers.Count)],
    //                    librarians[r.Next(0, librarians.Count)],
    //                    bibliographies[r.Next(0, bibliographies.Count)]);

    //            }

    //            ctx.Complete();
    //        }
    //    }

    //    private List<Entity> GetCustomers(EntityRepository repo)
    //    {
    //        var q = new EntityQuery2("User");
    //        q.WhereIs("UserType", UserTypes.Customer);
    //        return repo.Search(q).ToList();
    //    }

    //    private List<Entity> GetLibrarians(EntityRepository repo)
    //    {
    //        var q = new EntityQuery2("User");
    //        q.WhereIs("UserType", UserTypes.Librarian);
    //        return repo.Search(q).ToList();
    //    }

    //    private List<Entity> GetCompletedBibliographies(EntityRepository repo)
    //    {
    //        var q = new EntityQuery2("Bibliography");
    //        q.WhereIs("Complete", true);
    //        return repo.Search(q).ToList();
    //    }

    //    private List<Entity> GetAllCustomerGroups(EntityRepository repo)
    //    {
    //        var q = new EntityQuery2("UserGroup");
    //        q.WhereIs("UserType", UserTypes.Customer);
    //        return repo.Search(q).ToList();
    //    }

    //    private void CreateInquery(EntityRepository repo, DateTime replyBefore, Entity customer, Entity librarian)
    //    {
    //        var inquery = new Entity("Inquery");
    //        inquery.SetData<string>("Question", "LOADTESTING. This is a load testing generated text. This is a load testing generated text. This is a load testing generated text. This is a load testing generated text. This is a load testing generated text. This is a load testing generated text. This is a load testing generated text. This is a load testing generated text. This is a load testing generated text. This is a load testing generated text. This is a load testing generated text.");
    //        inquery.SetData<ReplyMethods>("ReplyMethod", ReplyMethods.ByNotification);
    //        inquery.SetData<DateTime>("ReplyBefore", replyBefore);

    //        repo.Create(inquery);
    //        repo.Attach(inquery, new Relation("Customer", customer));
    //        repo.Attach(inquery, new Relation("ProcessedBy", librarian));
    //    }
    //    private void CreateBibliography(EntityRepository repo, string subject, int from, int to, int[] langs, int[] docs)
    //    {
    //        var bibl = new Entity("Bibliography");
    //        bibl.SetData<string>("subject", subject);
    //        bibl.SetData<int>("FromYear", from);
    //        bibl.SetData<int>("ToYear", to);
    //        bibl.SetData<bool>("Complete", true);
    //        repo.Create(bibl);

    //        foreach (var id in langs)
    //        {
    //            var rel = new Relation("Keywords", new Entity("Language", id));
    //            rel.SetData<string>("Keywords", "Keywords, keywords, keywords, keywords, keywords, keywords, keywords, keywords, keywords, keywords, keywords, keywords, keywords.");
    //            repo.Attach(bibl, rel);
    //        }
    //        foreach (var id in docs)
    //        {
    //            var rel = new Relation("Included", new Entity("BibliographicDocument", id));
    //            repo.Attach(bibl, rel);
    //        }
    //    }
    //    private void CreateBibliographicQuery(EntityRepository repo, DateTime replyBefore, PaymentMethod paymentMethod, decimal price, bool paid, Entity customer, Entity librarian, Entity bibliography)
    //    {
    //        var q = new Entity("BibliographicQuery");
    //        q.SetData<bool>("forNew", false);
    //        q.SetData<ReplyMethods>("ReplyMethod", ReplyMethods.ByNotification);
    //        q.SetData<DateTime>("ReplyBefore", replyBefore);
    //        q.SetData<PaymentMethod>("PaymentMethod", paymentMethod);

    //        repo.Create(q);

    //        var payment = new Entity("Payment");
    //        payment.SetData<decimal>("Amount", price);
    //        payment.SetData<PaymentMethod>("Method", paymentMethod);
    //        payment.SetData<PaymentStatus>("Status", paid ? PaymentStatus.Paid : PaymentStatus.Pending);

    //        repo.Create(payment);

    //        repo.Attach(q, new Relation("Customer", customer));
    //        repo.Attach(q, new Relation("ProcessedBy", librarian));
    //        repo.Attach(q, new Relation("Payment", payment));
    //        repo.Attach(q, new Relation("Query", bibliography));
    //    }
    //}
}
