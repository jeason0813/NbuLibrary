define('mods/refanalysis',
    ['navigation', 'router', 'textprovider', 'usercontext', 'presenter', 'entitypage', 'mods/vm.analysisquery', 'mods/vm.analysisqueries', 'reports'],
    function (navigation, router, tp, usercontext, presenter, ep, vmQuery, vmQueries, reports) {
        var MODULE_ID = 105;

        var Permissions = {
            Use: 'Use'
        }

        if (!usercontext.hasPermission(MODULE_ID, Permissions.Use))
            return;

        if (usercontext.isCustomer()) {
            var item = navigation.add(tp.get('nav_refanalysis_root'), '#/refanalysis/');
            item.add(tp.get('nav_refanalysis_newquery'), '#/refanalysis/query');

            item.add(tp.get('nav_refanalysis_queries'), '#/refanalysis/queries');
            router.add('#/refanalysis/queries', function () {
                vmQueries.bind();
                presenter.show(vmQueries);
            });
        }
        else if (usercontext.isLibrarian()) {
            var item = navigation.add(tp.get('nav_refanalysis_root'), '#/refanalysis/queries');
            router.add('#/refanalysis/queries', function () {
                vmQueries.bind();
                presenter.show(vmQueries);
            });

            $.when(reports.getByModule(MODULE_ID)).done(function (reports) {
                if (reports && reports.length)
                    item.add(tp.get('nav_refanalysis_queries'), '#/refanalysis/queries');
                $(reports).each(function () {
                    item.add(this.label, this.url, 9999, 'icon-list-alt');
                });
            });
        }

        router.add('#/refanalysis/query', function () {
            vmQuery.bind();
            presenter.show(vmQuery);
        });
        router.add('#/refanalysis/editquery/:id', function () {
            vmQuery.bind(this.id);
            presenter.show(vmQuery);
        });

        var queryPage = new ep.Page();
        router.registerDetailsPage('AnalysisQuery', '#/refanalysis/queries/:id');
        router.add('#/refanalysis/queries/:id', function () {
            var uiQuery = usercontext.isCustomer() ? 'RefAnalysis_Customer_AnalysisQuery_Details' : 'RefAnalysis_Librarian_AnalysisQuery_Details';
            queryPage.bind('AnalysisQuery', uiQuery, this.id)
            queryPage.getQuery().include('Payment', 'Payment');
            queryPage.getQuery().addProperty('Status');
            var vm = {
                Query: queryPage,
                IsCustomer: usercontext.isCustomer(),
                IsPaid: ko.observable(false),
                template: 'RefAnalysis_Query_Details'
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