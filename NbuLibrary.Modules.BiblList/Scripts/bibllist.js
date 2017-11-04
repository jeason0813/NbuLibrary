define('mods/bibllist',
    ['navigation', 'router', 'textprovider', 'usercontext', 'presenter', 'entitypage', 'mods/vm.bibllistquery', 'mods/vm.bibllistqueries', 'reports'],
    function (navigation, router, tp, usercontext, presenter, ep, vmQuery, vmQueries, reports) {
        var MODULE_ID = 104;

        var Permissions = {
            Use: 'Use'
        }

        if (!usercontext.hasPermission(MODULE_ID, Permissions.Use))
            return;

        if (usercontext.isCustomer()) {
            var item = navigation.add(tp.get('nav_bibllist_root'), '#/bibllist/');
            item.add(tp.get('nav_bibllist_newquery'), '#/bibllist/query');

            item.add(tp.get('nav_bibllist_queries'), '#/bibllist/queries');
            router.add('#/bibllist/queries', function () {
                vmQueries.bind();
                presenter.show(vmQueries);
            });
        }
        else if (usercontext.isLibrarian()) {
            var item = navigation.add(tp.get('nav_bibllist_root'), '#/bibllist/queries');
            router.add('#/bibllist/queries', function () {
                vmQueries.bind();
                presenter.show(vmQueries);
            });

            $.when(reports.getByModule(MODULE_ID)).done(function (reports) {
                if (reports && reports.length)
                    item.add(tp.get('nav_bibllist_queries'), '#/bibllist/queries');
                $(reports).each(function () {
                    item.add(this.label, this.url, 9999, 'icon-list-alt');
                });
            });
        }

        router.add('#/bibllist/query', function () {
            vmQuery.bind();
            presenter.show(vmQuery);
        });
        router.add('#/bibllist/editquery/:id', function () {
            vmQuery.bind(this.id);
            presenter.show(vmQuery);
        });

        var queryPage = new ep.Page();
        router.registerDetailsPage('BibliographicListQuery', '#/bibllist/queries/:id');
        router.add('#/bibllist/queries/:id', function () {
            var uiQuery = usercontext.isCustomer() ? 'BiblList_Customer_BibliographicListQuery_Details' : 'BiblList_Librarian_BibliographicListQuery_Details';
            queryPage.bind('BibliographicListQuery', uiQuery, this.id)
            queryPage.getQuery().include('Payment', 'Payment');
            queryPage.getQuery().addProperty('Status');
            var vm = {
                Query: queryPage,
                IsCustomer: usercontext.isCustomer(),
                IsPaid: ko.observable(false),
                template: 'BiblList_Query_Details'
            };

            $.when(queryPage.load()).done(function () {
                if (queryPage.Entity.Data.Status == 0) {
                    vm.IsPaid(true); //no pending payment, query is still in new status
                    return;
                }

                var payment = null;
                if (queryPage.Entity.RelationsData && queryPage.Entity.RelationsData.Payment_Payment)
                    payment = queryPage.Entity.RelationsData.Payment_Payment();
                if (payment && payment.Entity.Data.Status == 2)//Paid
                    vm.IsPaid(true);
            });
            presenter.show(vm);
        });
    });