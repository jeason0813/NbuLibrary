define('mods/biblref',
    ['navigation', 'router', 'textprovider', 'usercontext', 'presenter', 'entitypage', 'mods/vm.biblquery', 'mods/vm.biblqueries', 'mods/vm.bibliographies', 'reports'],
    function (navigation, router, tp, usercontext, presenter, ep, vmQuery, vmQueries, vmBibliographies, reports) {

        var MODULE_ID = 103;

        var Permissions = {
            Use: 'Use'
        }

        if (!usercontext.hasPermission(MODULE_ID, Permissions.Use))
            return;


        if (usercontext.isCustomer()) {
            var item = navigation.add(tp.get('nav_biblref_root'), '#/biblref/');
            item.add(tp.get('nav_biblref_newquery'), '#/biblref/query');

            item.add(tp.get('nav_biblref_queries'), '#/biblref/queries');
            router.add('#/biblref/queries', function () {
                vmQueries.bind();
                presenter.show(vmQueries);
            });
        }
        else if (usercontext.isLibrarian()) {
            var item = navigation.add(tp.get('nav_biblref_root'), '#/biblref/');
            item.add(tp.get('nav_biblref_queries'), '#/biblref/queries');
            item.add(tp.get('nav_biblref_bibliographies'), '#/biblref/bibliographies');
            router.add('#/biblref/bibliographies', function () {
                vmBibliographies.bind();
                presenter.show(vmBibliographies);
            });
            router.add('#/biblref/queries', function () {
                vmQueries.bind();
                presenter.show(vmQueries);
            });

            $.when(reports.getByModule(MODULE_ID)).done(function (reports) {
                $(reports).each(function () {
                    item.add(this.label, this.url, 9999, 'icon-list-alt');
                });
            });
        }

        router.add('#/biblref/query', function () {
            vmQuery.bind();
            presenter.show(vmQuery);
        });

        var queryPage = new ep.Page();
        var biblPage = new ep.Page();
        router.registerDetailsPage('BibliographicQuery', '#/biblref/queries/:id');
        router.add('#/biblref/queries/:id', function () {
            var uiQuery = usercontext.isCustomer() ? 'BiblRef_Customer_BibliographicQuery_Details' : 'BiblRef_Librarian_BibliographicQuery_Details';
            var uiBibl = usercontext.isCustomer() ? 'BiblRef_Customer_Bibliography_Details' : 'BiblRef_Librarian_Bibliography_Details';
            queryPage.bind('BibliographicQuery', uiQuery, this.id)
            queryPage.getQuery().include('Payment', 'Payment');
            queryPage.getQuery().include('Bibliography', 'Query');

            var vm = {
                Query: queryPage,
                Bibliography: biblPage,
                IsCustomer: usercontext.isCustomer(),
                IsPaid: ko.observable(false),
                template: 'BiblRef_Bibliography_Details'
            };

            $.when(queryPage.load()).done(function () {
                biblPage.bind('Bibliography', uiBibl, queryPage.Entity.RelationsData.Bibliography_Query().Entity.Id);
                biblPage.load();

                var payment = null;
                if (queryPage.Entity.RelationsData && queryPage.Entity.RelationsData.Payment_Payment)
                    payment = queryPage.Entity.RelationsData.Payment_Payment();
                if (payment && payment.Entity.Data.Status == 2)//Paid
                    vm.IsPaid(true);

            });
            presenter.show(vm);
        });
    });
