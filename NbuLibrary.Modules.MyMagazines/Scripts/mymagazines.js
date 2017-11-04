/// <reference path="../app/usercontext.js" />
define('mods/mymagazines',
    ['jquery', 'navigation', 'router', 'textprovider', 'presenter', 'usercontext', 'dataservice', 'grid', 'filters', 'mods/vm.magazines', 'mods/vm.subscriptions', 'mods/vm.mysubscriptions', 'mods/vm.magazinemgr', 'mods/vm.mymagcustomers', 'reports'],
    function ($, navigation, router, tp, presenter, usercontext, dataservice, entitygrid, gf, vmMagazines, vmSubscriptions, vmMySubscriptions, vmMagazineMgr, vmCustomers, reports) {
        var MODULE_ID = 102;
        var Permissions = {
            Use: 'Use'
        };

        if (!usercontext.hasPermission(MODULE_ID, Permissions.Use))
            return;

        if (usercontext.isLibrarian()) {

            var item = navigation.add(tp.get('nav_mymagazines'), '#/mymagazines/');

            var url = '#/mymagazines/magazines';
            item.add(tp.get('nav_mymagazines_magazines'), url);
            item.add(tp.get('nav_mymagazines_customers'), '#/mymagazines/customers');
            router.add(url, function () {
                vmMagazines.bind();
                presenter.show(vmMagazines);
            });
            router.add('#/mymagazines/subscriptions/:id', function () {
                vmSubscriptions.bind(this.id);
                presenter.show(vmSubscriptions);
            });

            router.add('#/mymagazines/usersubscriptions/:id', function () {
                vmMySubscriptions.bind(this.id, true);
                presenter.show(vmMySubscriptions);
            });

            router.add('#/mymagazines/issues/:id', function () {
                vmMagazineMgr.bind(this.id);
                presenter.show(vmMagazineMgr);
            });

            router.add('#/mymagazines/customers', function () {
                vmCustomers.bind();
                presenter.show(vmCustomers);
            });

            $.when(reports.getByModule(MODULE_ID)).done(function (reports) {
                $(reports).each(function () {
                    item.add(this.label, this.url, 9999, 'icon-list-alt');
                });
            });

        }
        else if (usercontext.isCustomer()) {
            var url = '#/mymagazines/magazines';
            var item = navigation.add(tp.get('nav_mymagazines'), '#/mymagazines/');
            item.add(tp.get('nav_mymagazines_magazines'), url);
            router.add(url, function () {
                vmMySubscriptions.bind(usercontext.currentUser().Id, true);
                presenter.show(vmMySubscriptions);
            });
            var url2 = '#/mymagazines/subscriptions/';
            item.add(tp.get('nav_mymagazines_mysubscriptions'), url2);
            router.add(url2, function () {
                vmMySubscriptions.bind(usercontext.currentUser().Id);
                presenter.show(vmMySubscriptions);
            });

            var issuesGrid = new entitygrid.Grid();
            var filters = new gf.GridFilters();
            router.add('#/mymagazines/issues/:id', function () {
                var query = dataservice.search('Issue').allProperties(true);
                query.include('File', 'Content');
                query.include('Magazine', 'Issue');
                query.rules.is('Sent', true);
                query.rules.relatedTo('Magazine', 'Issue', this.id);
                query.sort('CreatedOn', true);
                filters.bind([{ Type: 6, Label: tp.get('filterby_issue_year'), Property: 'Year' }],
                       query,
                       function () { issuesGrid.reload(true); }, //filters apply callback
                       function () { issuesGrid.reload(true); });//filters clear callback

                //filters.Filters()[0].value(new Date().getUTCFullYear());
                issuesGrid.bind(query, 'MyMagazines_Customer_IssuesGrid');//TODO: UIDef for mymagazines-customer-issues
                presenter.show({
                    template: 'MyMagazines_Customer_Issues',
                    Items: issuesGrid.Items,
                    Definition: issuesGrid.Definition,
                    LoadedDef: issuesGrid.LoadedDef,
                    Loaded: issuesGrid.Loaded,
                    Sorting: issuesGrid.Sorting,
                    Paging: issuesGrid.Paging,
                    Filters: filters,
                    sort: issuesGrid.sort
                });
            });
        }
    });

